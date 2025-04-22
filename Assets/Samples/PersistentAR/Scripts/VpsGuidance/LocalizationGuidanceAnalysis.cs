// Copyright 2022-2025 Niantic.

using System.Collections.Generic;
using System.Linq;
using Niantic.Lightship.AR.XRSubsystems;
using UnityEngine;

namespace Niantic.Lightship.AR.Samples
{
    public class LocalizationGuidanceAnalysis
    {
        // Diagnostic threshold consts
        public const float BlurryProbability = 0.65f;
        public const float BlurryLinearVelocity = 0.2f;
        public const float BlurryAngularVelocity = 0.2f;
        public const float GroundProbability = 0.5f;
        public const float ObstructedProbability = 0.65f;
        public const float BrightSaturatedRatio = 0.1f;
        public const float BrightnessMean = 120.0f;
        public const float BrightnessVariance = 4000.0f;
        public const float TooDarkProbability = 0.7f;
        public const float InCarProbability = 0.8f;

        //
        private float _vpsRequestTimeTooLongThresholdInSeconds;
        private Dictionary<ulong, LocalizationGuidance.DebugInfoPerFrame> _debugInfos;
        private List<LocalizationGuidance.DebugInfoPerFrame> _debugInfoHistory;

        public LocalizationGuidanceAnalysis(
            Dictionary<ulong, LocalizationGuidance.DebugInfoPerFrame> debugInfos,
            List<LocalizationGuidance.DebugInfoPerFrame> debugInfoHistory,
            float vpsRequestTimeTooLongThresholdInSeconds
        )
        {
            _vpsRequestTimeTooLongThresholdInSeconds = vpsRequestTimeTooLongThresholdInSeconds;
            _debugInfos = debugInfos;
            _debugInfoHistory = debugInfoHistory;
        }

        // Check network connection available
        public  ILocalizationGuidanceService.StopReason CheckNetworkAvailability()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                return ILocalizationGuidanceService.StopReason.NetworkError;
            }

            return ILocalizationGuidanceService.StopReason.None;
        }

        // Check if VPS query running "too long"
        public bool IsVpsRequestRunningLong(LocalizationGuidance.DebugInfoPerFrame debugInfo)
        {
            if (!debugInfo.SlowNetworkReported)
            {
                float reqestResponseTime = 0.0f;
                if (debugInfo.NetworkStatus.Status == RequestStatus.Pending || debugInfo.NetworkStatus.Status == RequestStatus.Unknown)
                {
                    if (debugInfo.RequestStartTimeInSec == 0.0f)
                    {
                        Debug.Log("missing start time. strange.... this started something else than network info.");
                        return false;
                    }
                    reqestResponseTime = Time.time - debugInfo.RequestStartTimeInSec;
                }
                else
                {
                    reqestResponseTime = debugInfo.RequestEndTimeInSec - debugInfo.RequestStartTimeInSec;
                }

                if (reqestResponseTime >= _vpsRequestTimeTooLongThresholdInSeconds)
                {
                    return true;
                }
            }
            return false;
        }

        // Analyze VPS localiztaion requests' network response time
        public ILocalizationGuidanceService.RecoveryGuidance AnalyzeNetworkResponseTime()
        {
            var recoveryGuidance = ILocalizationGuidanceService.RecoveryGuidance.None;

            // Check if response time is too long in the _debugInfos map
            List<ulong> keysSlowNetworkReported = new List<ulong>();
            foreach (var debugInfoPair in _debugInfos)
            {
                if (IsVpsRequestRunningLong(debugInfoPair.Value))
                {
                    recoveryGuidance = ILocalizationGuidanceService.RecoveryGuidance.SlowNetwork;
                    keysSlowNetworkReported.Add(debugInfoPair.Key);
                }
            }
            // update "reported" flags to prevent reporting slow network for the same request repeatedly
            foreach (var key in keysSlowNetworkReported)
            {
                var debugInfo = _debugInfos[key];
                debugInfo.SlowNetworkReported = true;
                _debugInfos[key] = debugInfo;
            }


            // Check if response time is too long in the _debugInfoHistory list
            for (var i = 0; i < _debugInfoHistory.Count(); i++)
            {
                var debugInfo = _debugInfoHistory[i];
                if (IsVpsRequestRunningLong(debugInfo))
                {
                    recoveryGuidance = ILocalizationGuidanceService.RecoveryGuidance.SlowNetwork;
                    debugInfo.SlowNetworkReported = true;
                    _debugInfoHistory[i] = debugInfo;
                }
            }

            return recoveryGuidance;
        }

        // Determine if user is getting network related error consistently, or slow network
        public void AnalyzeNetworkStatusIssues(
            out ILocalizationGuidanceService.StopReason stopReason
        )
        {
            stopReason = ILocalizationGuidanceService.StopReason.None;

            // Check if same error status continues past a few VPS requests
            RequestStatus firstStatus = _debugInfoHistory[0].NetworkStatus.Status;
            int sameStatusCount = 0;
            ErrorCode errorCode = 0;

            foreach (var debugInfo in _debugInfoHistory)
            {
                if (debugInfo.NetworkStatus.Status == firstStatus)
                {
                    sameStatusCount++;
                    if (firstStatus == RequestStatus.Failed)
                    {
                        errorCode = debugInfo.NetworkStatus.Error;
                    }
                }
            }

            if (sameStatusCount == _debugInfoHistory.Count)
            {
                if (firstStatus == RequestStatus.Failed)
                {
                    // TODO: What error code to get when no network connection?
                    stopReason = ILocalizationGuidanceService.StopReason.NetworkError;
                    if (errorCode == ErrorCode.BadNetworkConnection)
                    {
                        stopReason = ILocalizationGuidanceService.StopReason.NetworkError;
                    }
                    else if (errorCode == ErrorCode.BadApiKey)
                    {
                        stopReason = ILocalizationGuidanceService.StopReason.ClientError;
                    }
                    else if (errorCode == ErrorCode.PermissionDenied)
                    {
                        stopReason = ILocalizationGuidanceService.StopReason.ClientError;
                    }
                    else if (errorCode == ErrorCode.RequestsLimitExceeded)
                    {
                        stopReason = ILocalizationGuidanceService.StopReason.ServerError;
                    }
                    else if (errorCode == ErrorCode.InternalServer)
                    {
                        stopReason = ILocalizationGuidanceService.StopReason.ServerError;
                    }
                    else if (errorCode == ErrorCode.InternalClient)
                    {
                        stopReason = ILocalizationGuidanceService.StopReason.ClientError;
                    }
                    return;
                }
                if (firstStatus == RequestStatus.Pending)
                {
                    // TODO: in what circumstances we get into this situation? extremely slow network?
                    stopReason = ILocalizationGuidanceService.StopReason.NetworkError;
                }
            }
        }

        // Determine if user is moving too fast
        public ILocalizationGuidanceService.RecoveryGuidance AnalyzeTooFast()
        {
            float blurryProbability = 0.0f;
            float linearVelocity = 0.0f;
            float angularVelocity = 0.0f;
            // check values if we have enough data points
            if (AverageDiagnostics(
                DiagnosticLabel.BlurryProbability,
                DiagnosticLabel.LinearVelocityMetersPerSecond,
                DiagnosticLabel.AngularVelocityRadiansPerSecond,
                out blurryProbability,
                out linearVelocity,
                out angularVelocity)
            )
            {
                // TODO: adjust veleocity thresholds
                if
                (
                    blurryProbability >= BlurryProbability &&
                    (linearVelocity >= BlurryLinearVelocity || angularVelocity >= BlurryAngularVelocity)
                )
                {
                    return ILocalizationGuidanceService.RecoveryGuidance.SlowDown;
                }
            }
            return ILocalizationGuidanceService.RecoveryGuidance.None;
        }

        // looking ground/feet
        public ILocalizationGuidanceService.RecoveryGuidance AnalyzeLookingGround()
        {
            if (AverageDiagnostics(
                    DiagnosticLabel.GroundOrFeetProbability,
                    DiagnosticLabel.Unknown,
                    DiagnosticLabel.Unknown,
                    out var groundProbability,
                    out var unused1,
                    out var unused2)
               )
            {
                // TODO: use camera roatation too _lastCameraTransform.transform.eulerAngles
                if (groundProbability >= GroundProbability)
                {
                    return ILocalizationGuidanceService.RecoveryGuidance.LookUp;
                }
            }
            return ILocalizationGuidanceService.RecoveryGuidance.None;
        }

        // obstructed
        public ILocalizationGuidanceService.RecoveryGuidance AnalyzeImageObstructed()
        {
            if (AverageDiagnostics(
                    DiagnosticLabel.ObstructedProbability,
                    DiagnosticLabel.Unknown,
                    DiagnosticLabel.Unknown,
                    out var obstructedProbability,
                    out var unused1,
                    out var unused2)
               )
            {
                if (obstructedProbability >= ObstructedProbability)
                {
                    return ILocalizationGuidanceService.RecoveryGuidance.Obstructed;
                }
            }
            return ILocalizationGuidanceService.RecoveryGuidance.None;
        }

        // glare
        public ILocalizationGuidanceService.RecoveryGuidance AnalyzeImageGlare()
        {
            if (AverageDiagnostics(
                    DiagnosticLabel.OversaturatedPixelRatio,
                    DiagnosticLabel.BrightnessMean,
                    DiagnosticLabel.BrightnessVariance,
                    out var saturatedRatio,
                    out var brightnessMean,
                    out var brightnessVariance)
               )
            {
                if
                (
                    saturatedRatio >= BrightSaturatedRatio &&
                    brightnessMean <= BrightnessMean &&
                    brightnessVariance > BrightnessVariance
                )
                {
                    return ILocalizationGuidanceService.RecoveryGuidance.AvoidGlare;
                }
            }
            return ILocalizationGuidanceService.RecoveryGuidance.None;
        }

        // blurry
        public ILocalizationGuidanceService.RecoveryGuidance AnalyzeImageBlurry()
        {
            if (AverageDiagnostics(
                DiagnosticLabel.BlurryProbability,
                DiagnosticLabel.Unknown,
                DiagnosticLabel.Unknown,
                out var blurryProbability,
                out var unused1,
                out var unused2)
            )
            {
                if (blurryProbability >= BlurryProbability)
                {
                    return ILocalizationGuidanceService.RecoveryGuidance.BlurryImage;
                }
            }
            return ILocalizationGuidanceService.RecoveryGuidance.None;
        }

        // too dark
        public ILocalizationGuidanceService.RecoveryGuidance AnalyzeImageTooDark()
        {
            if (AverageDiagnostics(
                DiagnosticLabel.TooDarkProbability,
                DiagnosticLabel.Unknown,
                DiagnosticLabel.Unknown,
                out var tooDarkProbability,
                out var unused1,
                out var unused2)
            )
            {
                if (tooDarkProbability >= TooDarkProbability)
                {
                    return ILocalizationGuidanceService.RecoveryGuidance.FindBetterLighting;
                }
            }
            return ILocalizationGuidanceService.RecoveryGuidance.None;
        }

        // in car
        public ILocalizationGuidanceService.RecoveryGuidance AnalyzeImageInCar()
        {
            if (AverageDiagnostics(
                    DiagnosticLabel.InCarProbability,
                    DiagnosticLabel.Unknown,
                    DiagnosticLabel.Unknown,
                    out var inCarProbability,
                    out var unused1,
                    out var unused2)
               )
            {
                if (inCarProbability >= InCarProbability)
                {
                    return ILocalizationGuidanceService.RecoveryGuidance.InCar;
                }
            }
            return ILocalizationGuidanceService.RecoveryGuidance.None;
        }

        // averaging up to 3 specific diagnostic results
        private bool AverageDiagnostics(DiagnosticLabel label1, DiagnosticLabel label2, DiagnosticLabel label3,
            out float averageValue1, out float averageValue2, out float averageValue3)
        {
            averageValue1 = 0.0f;
            averageValue2 = 0.0f;
            averageValue3 = 0.0f;

            if (_debugInfoHistory.Count == 0)
            {
                // no data yet. do nothing
                return false;
            }

            int count = 0;
            int expectedCount = 0;
            //
            if (label1 != DiagnosticLabel.Unknown)
            {
                expectedCount += _debugInfoHistory.Count;
            }
            if (label2 != DiagnosticLabel.Unknown)
            {
                expectedCount += _debugInfoHistory.Count;
            }
            if (label3 != DiagnosticLabel.Unknown)
            {
                expectedCount += _debugInfoHistory.Count;
            }
            //
            foreach (var debugInfo in _debugInfoHistory)
            {
                var diagnosticsDict = debugInfo.Diagnostics.ScoresPerDiagnosticLabel;
                if (diagnosticsDict == null)
                {
                    continue;
                }
                if (label1 != DiagnosticLabel.Unknown && diagnosticsDict.ContainsKey(label1))
                {
                    averageValue1 += diagnosticsDict[label1];
                    count++;
                }
                if (label2 != DiagnosticLabel.Unknown && diagnosticsDict.ContainsKey(label2))
                {
                    averageValue2 += diagnosticsDict[label2];
                    count++;
                }
                if (label3 != DiagnosticLabel.Unknown && diagnosticsDict.ContainsKey(label3))
                {
                    averageValue3 += diagnosticsDict[label3];
                    count++;
                }
            }

            if (count > 0 && count == expectedCount)
            {
                averageValue1 /= (float)_debugInfoHistory.Count;
                averageValue2 /= (float)_debugInfoHistory.Count;
                averageValue3 /= (float)_debugInfoHistory.Count;
                return true;
            }

            return false;
        }
    }
}

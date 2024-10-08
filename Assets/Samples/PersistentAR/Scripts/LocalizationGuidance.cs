// Copyright 2022-2024 Niantic.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Niantic.Lightship.AR.LocationAR;
using Niantic.Lightship.AR.XRSubsystems;
using UnityEngine;

namespace Niantic.Lightship.AR.Samples
{
    public class LocalizationGuidance : ILocalizationGuidanceService
    {
        // Time to invoke clientTimeout event, when no successful localization
        public float ClientTimeoutInSeconds { get; set; } = 60.0f;

        // Additional time until invoke timeout when guidance event occured right before timeout
        public float ClientTimeoutAdditionalSeconds { get; set; } = 5.0f;

        // Time interval between checking aggregated over frames guidance info
        public float IntervalCheckingGuidanceInSeconds { get; set; } = 3.0f;

        // Number of past frames of diagnostics to analyze for guidance event
        public int FrameAnalysisWindowsize { get; set; } = 5;

        // The latest debug text from VPS diagnostics info
        public string LatestDebugSummaryText { get; private set; }

        // Maximum guidance event. Events will be suppressed after the max count
        public int MaxGuidanceEventCount { get; set; } = 3;

        // If an error fires with < N seconds left before timeout, add N seconds to the overall timeout
        public float TimeFromLastEventConsiderAddTimeout { get; set; } = 5.0f;

        // Threshold for "cloud vps request taking too long" in seconds
        public float VpsRequestTimeTooLongThresholdInSeconds { get; set; } = 3.0f;

        // An event with critical issue detected so that app can stop localization attempt
        public event Action<ILocalizationGuidanceService.StopCondition> ShouldStop;

        // A guidance event so that app may inform user to react for better chance of localization
        public event Action<ILocalizationGuidanceService.RecoverableCondition> CanRecover;

        // All three debug info combined for a frame
        private struct DebugInfoPerFrame
        {
            public XRPersistentAnchorNetworkRequestStatus NetworkStatus;
            public XRPersistentAnchorLocalizationStatus LocalizationStatus;
            public XRPersistentAnchorFrameDiagnostics Diagnostics;
            public Transform CameraTransform;
            public ulong FrameId; // maybe not needed
            public bool Processed;
            public float RequestStartTimeInSec;
            public float RequestEndTimeInSec;
            public bool SlowNetworkReported;
        }

        //
        private ARLocationManager _arLocationManager;

        private Camera _camera;

        private bool _isRunning;

        private bool _localized;

        private Dictionary<ulong, DebugInfoPerFrame> _debugInfos;

        private List<DebugInfoPerFrame> _debugInfoHistory;

        private int _guidanceEventCount;

        private float _timeLastEventOccured;

        // ctor
        public LocalizationGuidance(ARLocationManager arLocationManager, Camera camera)
        {
            _debugInfos = new Dictionary<ulong, DebugInfoPerFrame>();
            _debugInfoHistory = new List<DebugInfoPerFrame>();
            _arLocationManager = arLocationManager;
            _camera = camera;
        }

        public void StartGuidance()
        {
            _arLocationManager.subsystem.debugInfoProvided += OnDebugInfoProvided;
            _isRunning = true;
            _guidanceEventCount = 0;
            _timeLastEventOccured = Time.time;
            // TODO: should set the current localization status, instead of set to false here
            _localized = false;

            // start client timeout as coroutine
            _arLocationManager.StartCoroutine(LocalizationTimeoutCounter());
            _arLocationManager.StartCoroutine(PeriodicDiagnosticsCheck());
        }

        public void StopGuidance()
        {
            _isRunning = false;
            if (_arLocationManager.subsystem != null)
            {
                _arLocationManager.subsystem.debugInfoProvided -= OnDebugInfoProvided;
            }
            _debugInfos.Clear();
            _debugInfoHistory.Clear();
        }

        private void OnDebugInfoProvided(XRPersistentAnchorDebugInfo anchorDebugInfo)
        {
            // debug info for specific frame may come as different events. Combine them and process when
            // localization status is known

            // Process network status
            if (anchorDebugInfo.networkStatusArray != null && anchorDebugInfo.networkStatusArray.Any())
            {
                foreach (var netStatus in anchorDebugInfo.networkStatusArray)
                {
                    if (!_debugInfos.TryGetValue(netStatus.FrameId, out var debugInfo))
                    {
                        debugInfo.FrameId = netStatus.FrameId;
                        if (_camera)
                        {
                            debugInfo.CameraTransform = _camera.transform;
                        }
                        debugInfo.RequestStartTimeInSec = Time.time;
                        debugInfo.SlowNetworkReported = false;
                    }
                    if (netStatus.Status == RequestStatus.Successful || netStatus.Status == RequestStatus.Failed)
                    {
                        debugInfo.RequestEndTimeInSec = Time.time;
                    }
                    debugInfo.NetworkStatus = netStatus;
                    // TODO: how to avoid re-adding after the frame has been processed?
                    _debugInfos[netStatus.FrameId] = debugInfo;
                }
            }

            // Process diagnostics
            if (anchorDebugInfo.frameDiagnosticsArray != null && anchorDebugInfo.frameDiagnosticsArray.Any())
            {
                foreach (var diagnostics in anchorDebugInfo.frameDiagnosticsArray)
                {
                    if (_debugInfos.TryGetValue(diagnostics.FrameId, out var debugInfo))
                    {
                        // update diagnostics info
                        debugInfo.Diagnostics = diagnostics;
                        _debugInfos[diagnostics.FrameId] = debugInfo;
                    }
                    // Ignore diagnostic event if no entry in _debugInfos. Any valid frame should have network event first.
                }
            }

            // Process localization status
            if (anchorDebugInfo.localizationStatusArray != null && anchorDebugInfo.localizationStatusArray.Any())
            {
                foreach (var localizationStatus in anchorDebugInfo.localizationStatusArray)
                {
                    if (localizationStatus.Status == LocalizationStatus.Success)
                    {
                        // if localized, reset everything
                        _localized = true;
                        _debugInfoHistory.Clear();
                        _debugInfos.Clear();
                        return;
                    }

                    if (!_debugInfos.TryGetValue(localizationStatus.FrameId, out var debugInfo))
                    {
                        // Ignore if no entry in the _debugInfos map. this should not happen, but just in case
                        continue;
                    }

                    // update localization info, and move it into the _debugInfoHistory list
                    debugInfo = _debugInfos[localizationStatus.FrameId];
                    debugInfo.LocalizationStatus = localizationStatus;

                    _debugInfos.Remove(localizationStatus.FrameId);

                    if (_debugInfoHistory.Count < FrameAnalysisWindowsize)
                    {
                        _debugInfoHistory.Add(debugInfo);
                    }
                    else
                    {
                        _debugInfoHistory.RemoveAt(0);
                        _debugInfoHistory.Add(debugInfo);
                    }
                    UpdateDiagnosticsDebugText(debugInfo);
                }
            }
        }

        // Process debug info history. averaging past frame values and threshold.
        private void ProcessDebugInfoHistory()
        {
            // Check network availability and fire StopCondition event immediately, if no network connectivity
            var networkAvailable = CheckNetworkAvailability();
            if (networkAvailable != ILocalizationGuidanceService.StopReason.None)
            {
                InvokeShouldStopGuidanceEvent(networkAvailable);
                return;
            }

            // check if network is too slow
            var networkSlow = AnalyzeNetworkResponseTime();
            if (networkSlow != ILocalizationGuidanceService.RecoveryGuidance.None)
            {
                InvokeRecoverableGuidanceEvent(networkSlow);
                // slow network reporting won't prevent other condition reporting
            }

            // Do nothing if the list size is less than window size
            if (_debugInfoHistory.Count < FrameAnalysisWindowsize)
            {
                return;
            }

            // check network issues
            AnalyzeNetworkStatusIssues(out var stopReason);
            if (stopReason != ILocalizationGuidanceService.StopReason.None)
            {
                InvokeShouldStopGuidanceEvent(stopReason);
                return;
            }

            // in car?
            var inCar = AnalyzeImageInCar();
            if (inCar != ILocalizationGuidanceService.RecoveryGuidance.None)
            {
                InvokeRecoverableGuidanceEvent(inCar);
                return;
            }

            // moving too fast?
            var tooFast = AnalyzeTooFast();
            if (tooFast != ILocalizationGuidanceService.RecoveryGuidance.None)
            {
                InvokeRecoverableGuidanceEvent(tooFast);
                return;
            }

            // looking ground?
            var lookingGround = AnalyzeLookingGround();
            if (lookingGround != ILocalizationGuidanceService.RecoveryGuidance.None)
            {
                InvokeRecoverableGuidanceEvent(lookingGround);
                return;
            }

            // obstructed?
            var imageObstructed = AnalyzeImageObstructed();
            if (imageObstructed != ILocalizationGuidanceService.RecoveryGuidance.None)
            {
                InvokeRecoverableGuidanceEvent(imageObstructed);
                return;
            }

            // glare in image?
            var imageGlare = AnalyzeImageGlare();
            if (imageGlare != ILocalizationGuidanceService.RecoveryGuidance.None)
            {
                InvokeRecoverableGuidanceEvent(imageGlare);
                return;
            }

            // blurry?
            var imageBlurry = AnalyzeImageBlurry();
            if (imageBlurry != ILocalizationGuidanceService.RecoveryGuidance.None)
            {
                InvokeRecoverableGuidanceEvent(imageBlurry);
                return;
            }

            // too dark?
            var imageTooDark = AnalyzeImageTooDark();
            if (imageTooDark != ILocalizationGuidanceService.RecoveryGuidance.None)
            {
                InvokeRecoverableGuidanceEvent(imageTooDark);
                return;
            }
        }

        // Check network connection available
        private ILocalizationGuidanceService.StopReason CheckNetworkAvailability()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                return ILocalizationGuidanceService.StopReason.NetworkError;
            }

            return ILocalizationGuidanceService.StopReason.None;
        }

        // Check if VPS query running "too long"
        private bool IsVpsRequestRunningLong(DebugInfoPerFrame debugInfo)
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

                if (reqestResponseTime >= VpsRequestTimeTooLongThresholdInSeconds)
                {
                    return true;
                }
            }
            return false;
        }

        // Analyze VPS localiztaion requests' network response time
        private ILocalizationGuidanceService.RecoveryGuidance AnalyzeNetworkResponseTime()
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
        private void AnalyzeNetworkStatusIssues(
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
        private ILocalizationGuidanceService.RecoveryGuidance AnalyzeTooFast()
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
                if (blurryProbability >= 0.65f && (linearVelocity >= 0.2f || angularVelocity >= 0.2f))
                {
                    return ILocalizationGuidanceService.RecoveryGuidance.SlowDown;
                }
            }
            return ILocalizationGuidanceService.RecoveryGuidance.None;
        }

        // looking ground/feet
        private ILocalizationGuidanceService.RecoveryGuidance AnalyzeLookingGround()
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
                if (groundProbability >= 0.5f)
                {
                    return ILocalizationGuidanceService.RecoveryGuidance.LookUp;
                }
            }
            return ILocalizationGuidanceService.RecoveryGuidance.None;
        }

        // obstructed
        private ILocalizationGuidanceService.RecoveryGuidance AnalyzeImageObstructed()
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
                if (obstructedProbability >= 0.65f)
                {
                    return ILocalizationGuidanceService.RecoveryGuidance.Obstructed;
                }
            }
            return ILocalizationGuidanceService.RecoveryGuidance.None;
        }

        // glare
        private ILocalizationGuidanceService.RecoveryGuidance AnalyzeImageGlare()
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
                if (saturatedRatio >= 0.1f && brightnessMean <= 120.0f && brightnessVariance > 4000.0f)
                {
                    return ILocalizationGuidanceService.RecoveryGuidance.AvoidGlare;
                }
            }
            return ILocalizationGuidanceService.RecoveryGuidance.None;
        }

        // blurry
        private ILocalizationGuidanceService.RecoveryGuidance AnalyzeImageBlurry()
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
                if (blurryProbability >= 0.65f)
                {
                    return ILocalizationGuidanceService.RecoveryGuidance.BlurryImage;
                }
            }
            return ILocalizationGuidanceService.RecoveryGuidance.None;
        }

        // too dark
        private ILocalizationGuidanceService.RecoveryGuidance AnalyzeImageTooDark()
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
                if (tooDarkProbability >= 0.7f)
                {
                    return ILocalizationGuidanceService.RecoveryGuidance.FindBetterLighting;
                }
            }
            return ILocalizationGuidanceService.RecoveryGuidance.None;
        }

        // in car
        private ILocalizationGuidanceService.RecoveryGuidance AnalyzeImageInCar()
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
                if (inCarProbability >= 0.8f)
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

        // Invoke recoverable guidance event
        private void InvokeRecoverableGuidanceEvent(ILocalizationGuidanceService.RecoveryGuidance guidance)
        {
            var recoverableCondition = new ILocalizationGuidanceService.RecoverableCondition(guidance);
            CanRecover?.Invoke(recoverableCondition);
            if (guidance != ILocalizationGuidanceService.RecoveryGuidance.SlowNetwork)
            {
                // increment guidance count if not slow network. slow network will be reported always
                _guidanceEventCount++;
            }
            _timeLastEventOccured = Time.time;
        }

        // Invoke ShouldStop guidance event
        private void InvokeShouldStopGuidanceEvent(ILocalizationGuidanceService.StopReason reason)
        {
            var stopCondition = new ILocalizationGuidanceService.StopCondition(reason);
            ShouldStop?.Invoke(stopCondition);
            _guidanceEventCount++;
            _timeLastEventOccured = Time.time;
        }

        // Handle client timeout, which flags after N seconds without successful localization
        private IEnumerator LocalizationTimeoutCounter()
        {
            yield return new WaitForSeconds(ClientTimeoutInSeconds);
            if (!_localized && _isRunning)
            {
                if (Time.time - _timeLastEventOccured < TimeFromLastEventConsiderAddTimeout)
                {
                    // Wait extra seconds to give a chance to user to react to the last guidance
                    yield return new WaitForSeconds(ClientTimeoutAdditionalSeconds);
                }
                InvokeShouldStopGuidanceEvent(ILocalizationGuidanceService.StopReason.ClientTimeout);
            }
        }

        // Handle repeating diagnostics check
        private IEnumerator PeriodicDiagnosticsCheck()
        {
            while (_isRunning && !_localized)
            {
                yield return new WaitForSeconds(IntervalCheckingGuidanceInSeconds);
                Debug.Log($"Run diagnostic analysis: running={_isRunning}, localized={_localized}, guidance#={_guidanceEventCount}, max_guidance#={MaxGuidanceEventCount}");
                if (_isRunning && !_localized && _guidanceEventCount < MaxGuidanceEventCount)
                {
                    ProcessDebugInfoHistory();
                }
            }
        }

        // Update diagnostics debug text
        private void UpdateDiagnosticsDebugText(DebugInfoPerFrame debugInfo)
        {
            var diagnosticScoreDict = debugInfo.Diagnostics.ScoresPerDiagnosticLabel;
            if (diagnosticScoreDict == null)
            {
                return;
            }

            float linVelocity = 0.0f;
            float angVelocity = 0.0f;

            // construct debug text
            string debugText = "[[Diagnostics info]]\n";
            string debugTextColor;
            // dark <0.7
            if (diagnosticScoreDict.TryGetValue(DiagnosticLabel.TooDarkProbability, out var dark))
            {
                debugTextColor = "green";
                if (dark >= 0.7f)
                {
                    debugTextColor = "red";
                }

                debugText += $"<color={debugTextColor}>DARK: {dark}</color>\n";
            }

            // inCar < 0.8
            if (diagnosticScoreDict.TryGetValue(DiagnosticLabel.InCarProbability, out var inCar))
            {
                debugTextColor = "green";
                if (inCar >= 0.8f)
                {
                    debugTextColor = "red";
                }

                debugText += $"<color={debugTextColor}>CAR: {inCar}</color>\n";
            }


            // obstructed <0.65
            if (diagnosticScoreDict.TryGetValue(DiagnosticLabel.ObstructedProbability, out var obstructed))
            {
                debugTextColor = "green";
                if (obstructed >= 0.65f)
                {
                    debugTextColor = "red";
                }

                debugText += $"<color={debugTextColor}>OBST: {obstructed}</color>\n";
            }

            // blurry <0.65
            if (diagnosticScoreDict.TryGetValue(DiagnosticLabel.BlurryProbability, out var blurry))
            {
                debugTextColor = "green";
                if (blurry >= 0.65f)
                {
                    debugTextColor = "red";
                }

                debugText += $"<color={debugTextColor}>BLUR: {blurry}</color>\n";
            }

            // ground/feet <0.5
            if (diagnosticScoreDict.TryGetValue(DiagnosticLabel.GroundOrFeetProbability, out var ground))
            {
                debugTextColor = "green";
                if (ground >= 0.5f)
                {
                    debugTextColor = "red";
                }

                debugText += $"<color={debugTextColor}>GRND: {ground}</color>\n";
            }

            // bright ratio>=0.1 && mean<=120 && var>4000
            if (
                diagnosticScoreDict.TryGetValue(DiagnosticLabel.OversaturatedPixelRatio, out var overSatu) &&
                diagnosticScoreDict.TryGetValue(DiagnosticLabel.BrightnessMean, out var brightMean) &&
                diagnosticScoreDict.TryGetValue(DiagnosticLabel.BrightnessVariance, out var brightVar)
            )
            {
                debugTextColor = "green";
                if (overSatu >= 0.1f && brightMean <= 120.0f && brightVar > 4000.0f)
                {
                    debugTextColor = "red";
                }

                debugText += $"<color={debugTextColor}>BRGT: {overSatu}, {brightMean}, {brightVar}</color>\n";
            }

            // fast moving (no threashold yet)
            debugTextColor = "green";

            if (diagnosticScoreDict.TryGetValue(DiagnosticLabel.LinearVelocityMetersPerSecond, out linVelocity) &&
                diagnosticScoreDict.TryGetValue(DiagnosticLabel.AngularVelocityRadiansPerSecond, out angVelocity))
            {
                if (linVelocity >= 0.3f || angVelocity >= 0.3f)
                {
                    debugTextColor = "red";
                }
                debugText += $"<color={debugTextColor}>VELO: {linVelocity}, {angVelocity}</color>\n";
            }
            else
            {
                debugText += $"<color={debugTextColor}>VELO: -, -</color>\n";
            }

            // We don't have threshold for the rest of diagnostics
            debugTextColor = "black";
            // bad quality (no threashold yet)
            if (diagnosticScoreDict.TryGetValue(DiagnosticLabel.BadQualityProbability, out var badQuality))
            {
                debugText += $"<color={debugTextColor}>BADQ: {badQuality}</color>\n";
            }

            // targe not visible (no threashold yet)

            if (diagnosticScoreDict.TryGetValue(DiagnosticLabel.TargetNotVisibleProbability, out var notVisible))
            {
                debugText += $"<color={debugTextColor}>NVIS: {notVisible}</color>\n";
            }

            // camera rotation
            if (debugInfo.CameraTransform)
            {
                debugText += $"<color={debugTextColor}>ROT: {debugInfo.CameraTransform.eulerAngles}</color>\n";
            }

            // response time
            var dur = debugInfo.NetworkStatus.EndTimeMs - debugInfo.NetworkStatus.StartTimeMs;
            debugText += $"<color={debugTextColor}>NET: {dur}ms {debugInfo.NetworkStatus.Status} {debugInfo.NetworkStatus.Error} </color>\n";

            // update debug text
            LatestDebugSummaryText = debugText;
        }
    }
}

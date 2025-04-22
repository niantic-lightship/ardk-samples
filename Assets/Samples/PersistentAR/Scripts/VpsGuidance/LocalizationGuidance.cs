// Copyright 2022-2025 Niantic.

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
        public struct DebugInfoPerFrame
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

        private readonly LocalizationGuidanceAnalysis _guidanceAnalysis;

        // ctor
        public LocalizationGuidance(ARLocationManager arLocationManager, Camera camera)
        {
            _debugInfos = new Dictionary<ulong, DebugInfoPerFrame>();
            _debugInfoHistory = new List<DebugInfoPerFrame>();
            _guidanceAnalysis = new LocalizationGuidanceAnalysis(
                _debugInfos,
                _debugInfoHistory,
                VpsRequestTimeTooLongThresholdInSeconds
            );
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
                    var debugText = LocalizationGuidanceUtils.UpdateDiagnosticsDebugText(debugInfo);
                    if (!String.IsNullOrEmpty(debugText))
                    {
                        LatestDebugSummaryText = debugText;
                    }
                }
            }
        }

        // Process debug info history. averaging past frame values and threshold.
        private void ProcessDebugInfoHistory()
        {
            // Check network availability and fire StopCondition event immediately, if no network connectivity
            var networkAvailable = _guidanceAnalysis.CheckNetworkAvailability();
            if (networkAvailable != ILocalizationGuidanceService.StopReason.None)
            {
                InvokeShouldStopGuidanceEvent(networkAvailable);
                return;
            }

            // check if network is too slow
            var networkSlow = _guidanceAnalysis.AnalyzeNetworkResponseTime();
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
            _guidanceAnalysis.AnalyzeNetworkStatusIssues(out var stopReason);
            if (stopReason != ILocalizationGuidanceService.StopReason.None)
            {
                InvokeShouldStopGuidanceEvent(stopReason);
                return;
            }

            // in car?
            var inCar = _guidanceAnalysis.AnalyzeImageInCar();
            if (inCar != ILocalizationGuidanceService.RecoveryGuidance.None)
            {
                InvokeRecoverableGuidanceEvent(inCar);
                return;
            }

            // moving too fast?
            var tooFast = _guidanceAnalysis.AnalyzeTooFast();
            if (tooFast != ILocalizationGuidanceService.RecoveryGuidance.None)
            {
                InvokeRecoverableGuidanceEvent(tooFast);
                return;
            }

            // looking ground?
            var lookingGround = _guidanceAnalysis.AnalyzeLookingGround();
            if (lookingGround != ILocalizationGuidanceService.RecoveryGuidance.None)
            {
                InvokeRecoverableGuidanceEvent(lookingGround);
                return;
            }

            // obstructed?
            var imageObstructed = _guidanceAnalysis.AnalyzeImageObstructed();
            if (imageObstructed != ILocalizationGuidanceService.RecoveryGuidance.None)
            {
                InvokeRecoverableGuidanceEvent(imageObstructed);
                return;
            }

            // glare in image?
            var imageGlare = _guidanceAnalysis.AnalyzeImageGlare();
            if (imageGlare != ILocalizationGuidanceService.RecoveryGuidance.None)
            {
                InvokeRecoverableGuidanceEvent(imageGlare);
                return;
            }

            // blurry?
            var imageBlurry = _guidanceAnalysis.AnalyzeImageBlurry();
            if (imageBlurry != ILocalizationGuidanceService.RecoveryGuidance.None)
            {
                InvokeRecoverableGuidanceEvent(imageBlurry);
                return;
            }

            // too dark?
            var imageTooDark = _guidanceAnalysis.AnalyzeImageTooDark();
            if (imageTooDark != ILocalizationGuidanceService.RecoveryGuidance.None)
            {
                InvokeRecoverableGuidanceEvent(imageTooDark);
                return;
            }
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
    }
}

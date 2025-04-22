// Copyright 2022-2025 Niantic.

using System.Collections;
using Niantic.Lightship.AR.LocationAR;
using UnityEngine;
using UnityEngine.UI;

namespace Niantic.Lightship.AR.Samples
{
    public class LocalizationGuidanceManager : MonoBehaviour
    {
        [Tooltip("The location manager")] [SerializeField]
        private ARLocationManager _arLocationManager;

        [SerializeField]
        private GameObject _guidanceToast;

        [SerializeField]
        private Text _guidanceToastText;

        private ILocalizationGuidanceService _localizationGuidanceService;

        private float _timeStarted;

        private void Start()
        {
            _guidanceToast.SetActive(false);
            _localizationGuidanceService = new LocalizationGuidance(_arLocationManager, Camera.main);
            _localizationGuidanceService.ClientTimeoutInSeconds = 90;
            _localizationGuidanceService.IntervalCheckingGuidanceInSeconds = 3.5f;
            _localizationGuidanceService.VpsRequestTimeTooLongThresholdInSeconds = 3.0f;
            _localizationGuidanceService.ShouldStop += LocalizationGuidanceOnShouldStop;
            _localizationGuidanceService.CanRecover += LocalizationGuidanceOnCanRecover;
        }

        private void OnDestroy()
        {
            if (_localizationGuidanceService != null)
            {
                _localizationGuidanceService.ShouldStop -= LocalizationGuidanceOnShouldStop;
                _localizationGuidanceService.CanRecover -= LocalizationGuidanceOnCanRecover;
                _localizationGuidanceService.StopGuidance();
                _localizationGuidanceService = null;
            }
        }

        public void StartGuidance()
        {
            _localizationGuidanceService.StartGuidance();
            _timeStarted = Time.time;
        }

        public void StopGuidance()
        {
            if (_localizationGuidanceService != null)
            {
                _localizationGuidanceService.StopGuidance();
            }
        }

        private void LocalizationGuidanceOnCanRecover(ILocalizationGuidanceService.RecoverableCondition condition)
        {
            if (_guidanceToast.activeSelf)
            {
                // already showing toast. don't show this one.
                return;
            }
            bool showToast = false;
            string message = "";
            switch (condition.Guidance)
            {
                case ILocalizationGuidanceService.RecoveryGuidance.AvoidGlare:
                    message = "Please avoid glare";
                    showToast = true;
                    break;
                case ILocalizationGuidanceService.RecoveryGuidance.SlowDown:
                    message = "Please slow down";
                    showToast = true;
                    break;
                case ILocalizationGuidanceService.RecoveryGuidance.FindBetterLighting:
                    message = "Move to better lighting place";
                    showToast = true;
                    break;
                case ILocalizationGuidanceService.RecoveryGuidance.LookUp:
                    message = "Look up";
                    showToast = true;
                    break;
                case ILocalizationGuidanceService.RecoveryGuidance.Obstructed:
                    message = "Camera view is obstructed";
                    showToast = true;
                    break;
                case ILocalizationGuidanceService.RecoveryGuidance.BlurryImage:
                    message = "Camera is getting blurry image";
                    showToast = true;
                    break;
                case ILocalizationGuidanceService.RecoveryGuidance.LookAtPoi:
                    message = "Point the device towards POI";
                    showToast = true;
                    break;
                case ILocalizationGuidanceService.RecoveryGuidance.SlowNetwork:
                    message = "Your network is slow.";
                    showToast = true;
                    break;
                case ILocalizationGuidanceService.RecoveryGuidance.InCar:
                    message = "You are inside a vehicle";
                    showToast = true;
                    break;
                default:
                    // do nothing
                    break;
            }
            if (showToast)
            {
                _guidanceToastText.text = $"<color=black>{message}</color>";
                _guidanceToast.SetActive(true);
                StartCoroutine(CloseToastLater(2.0f));
            }
        }

        private void LocalizationGuidanceOnShouldStop(ILocalizationGuidanceService.StopCondition condition)
        {
            bool showToast = false;
            string message = "";
            switch (condition.Reason)
            {
                case ILocalizationGuidanceService.StopReason.ClientError:
                    message = "Client Error";
                    showToast = true;
                    break;
                case ILocalizationGuidanceService.StopReason.ClientTimeout:
                    message = "Client Timeout";
                    showToast = true;
                    break;
                case ILocalizationGuidanceService.StopReason.NetworkError:
                    message = "Network Error";
                    showToast = true;
                    break;
                case ILocalizationGuidanceService.StopReason.NetworkUnavailable:
                    message = "Network Unavailable";
                    showToast = true;
                    break;
                case ILocalizationGuidanceService.StopReason.ServerError:
                    message = "Server Error";
                    showToast = true;
                    break;
            }

            if (showToast)
            {
                _guidanceToastText.text = $"<color=red>{message}</color>";
                _guidanceToast.SetActive(true);
                StartCoroutine(CloseToastLater(3.0f));
                _localizationGuidanceService.StopGuidance();
            }
        }

        private IEnumerator CloseToastLater(float time)
        {
            yield return new WaitForSeconds(time);
            _guidanceToast.SetActive(false);
        }

    }
}

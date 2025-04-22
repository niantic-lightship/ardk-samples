// Copyright 2022-2025 Niantic.
// Copyright 2022-${CurrentDate.Year} Niantic.

using System;

namespace Niantic.Lightship.AR.Samples
{
    /// <summary>
    /// Interface for the localization guidance service.
    /// </summary>
    public interface ILocalizationGuidanceService
    {
        // Enums and structs

        /// <summary>
        /// The reason user should stop the localization process.
        /// </summary>
        public enum StopReason : UInt32
        {
            None = 0,
            NetworkUnavailable,
            NetworkError,
            ServerError,
            ClientError,
            ClientTimeout
        }

        /// <summary>
        ///  The reason user should stop the localization process.
        /// </summary>
        public readonly struct StopCondition
        {
            public StopCondition(StopReason reason)
            {
                Reason = reason;
            }
            public readonly StopReason Reason;
        }

        /// <summary>
        /// The reason user should take action for better chance to VPS localize
        /// </summary>
        public enum RecoveryGuidance : UInt32
        {
            None = 0,
            SlowDown,
            LookUp,
            Obstructed,
            AvoidGlare,
            BlurryImage,
            FindBetterLighting,
            LookAtPoi,
            SlowNetwork,
            InCar
        }

        /// <summary>
        /// The reason user should take action for better chance to VPS localize
        /// </summary>
        public readonly struct RecoverableCondition
        {
            public RecoverableCondition(RecoveryGuidance guidance)
            {
                Guidance = guidance;
            }
            public readonly RecoveryGuidance Guidance;
        }

        // Properties

        /// <summary>
        /// The time in seconds after which the client should stop trying to localize.
        /// </summary>
        public float ClientTimeoutInSeconds { get; set; }

        /// <summary>
        /// The time in seconds how often the client should check for guidance.
        /// </summary>
        public float IntervalCheckingGuidanceInSeconds { get; set; }

        /// <summary>
        /// The latest diagnostics summary for debugging purposes.
        /// </summary>
        public string LatestDebugSummaryText { get; }

        /// <summary>
        /// VPS localization request time to consider "slow"
        /// </summary>
        public float VpsRequestTimeTooLongThresholdInSeconds { get; set; }

        // Events

        /// <summary>
        /// Event that is triggered when the client should stop trying to localize.
        /// </summary>
        public event Action<StopCondition> ShouldStop;

        /// <summary>
        /// Event that is triggered when the user should take action for better chance to VPS localize.
        /// </summary>
        public event Action<RecoverableCondition> CanRecover;

        // Public methods

        /// <summary>
        /// Start the guidance service.
        /// </summary>
        public void StartGuidance();

        /// <summary>
        /// Stop the guidance service.
        /// </summary>
        public void StopGuidance();

    }
}

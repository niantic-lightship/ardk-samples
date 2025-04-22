// Copyright 2022-2025 Niantic.

using System.Text;
using Niantic.Lightship.AR.XRSubsystems;

namespace Niantic.Lightship.AR.Samples
{
    public class LocalizationGuidanceUtils
    {
                // Update diagnostics debug text
        public static string UpdateDiagnosticsDebugText(LocalizationGuidance.DebugInfoPerFrame debugInfo)
        {
            var diagnosticScoreDict = debugInfo.Diagnostics.ScoresPerDiagnosticLabel;
            if (diagnosticScoreDict == null)
            {
                return "";
            }

            float linVelocity = 0.0f;
            float angVelocity = 0.0f;

            // construct debug text
            StringBuilder debugText = new StringBuilder(100);
            debugText.AppendLine("[[Diagnostics info]]");
            string debugTextColor;
            // dark <0.7
            if (diagnosticScoreDict.TryGetValue(DiagnosticLabel.TooDarkProbability, out var dark))
            {
                debugTextColor = "green";
                if (dark >= LocalizationGuidanceAnalysis.TooDarkProbability)
                {
                    debugTextColor = "red";
                }

                debugText.AppendLine($"<color={debugTextColor}>DARK: {dark}</color>");
            }

            // inCar < 0.8
            if (diagnosticScoreDict.TryGetValue(DiagnosticLabel.InCarProbability, out var inCar))
            {
                debugTextColor = "green";
                if (inCar >= LocalizationGuidanceAnalysis.InCarProbability)
                {
                    debugTextColor = "red";
                }

                debugText.AppendLine($"<color={debugTextColor}>CAR: {inCar}</color>");
            }


            // obstructed <0.65
            if (diagnosticScoreDict.TryGetValue(DiagnosticLabel.ObstructedProbability, out var obstructed))
            {
                debugTextColor = "green";
                if (obstructed >= LocalizationGuidanceAnalysis.ObstructedProbability)
                {
                    debugTextColor = "red";
                }

                debugText.AppendLine($"<color={debugTextColor}>OBST: {obstructed}</color>");
            }

            // blurry <0.65
            if (diagnosticScoreDict.TryGetValue(DiagnosticLabel.BlurryProbability, out var blurry))
            {
                debugTextColor = "green";
                if (blurry >= LocalizationGuidanceAnalysis.BlurryProbability)
                {
                    debugTextColor = "red";
                }

                debugText.AppendLine($"<color={debugTextColor}>BLUR: {blurry}</color>");
            }

            // ground/feet <0.5
            if (diagnosticScoreDict.TryGetValue(DiagnosticLabel.GroundOrFeetProbability, out var ground))
            {
                debugTextColor = "green";
                if (ground >= LocalizationGuidanceAnalysis.GroundProbability)
                {
                    debugTextColor = "red";
                }

                debugText.AppendLine($"<color={debugTextColor}>GRND: {ground}</color>");
            }

            // bright ratio>=0.1 && mean<=120 && var>4000
            if (
                diagnosticScoreDict.TryGetValue(DiagnosticLabel.OversaturatedPixelRatio, out var overSatu) &&
                diagnosticScoreDict.TryGetValue(DiagnosticLabel.BrightnessMean, out var brightMean) &&
                diagnosticScoreDict.TryGetValue(DiagnosticLabel.BrightnessVariance, out var brightVar)
            )
            {
                debugTextColor = "green";
                if
                (
                    overSatu >= LocalizationGuidanceAnalysis.BrightSaturatedRatio &&
                    brightMean <= LocalizationGuidanceAnalysis.BrightnessMean&&
                    brightVar > LocalizationGuidanceAnalysis.BrightnessVariance
                )
                {
                    debugTextColor = "red";
                }

                debugText.AppendLine($"<color={debugTextColor}>BRGT: {overSatu}, {brightMean}, {brightVar}</color>");
            }

            // fast moving (no threashold yet)
            debugTextColor = "green";

            if (diagnosticScoreDict.TryGetValue(DiagnosticLabel.LinearVelocityMetersPerSecond, out linVelocity) &&
                diagnosticScoreDict.TryGetValue(DiagnosticLabel.AngularVelocityRadiansPerSecond, out angVelocity))
            {
                if
                (
                    linVelocity >= LocalizationGuidanceAnalysis.BlurryLinearVelocity ||
                    angVelocity >= LocalizationGuidanceAnalysis.BlurryAngularVelocity
                )
                {
                    debugTextColor = "red";
                }
                debugText.AppendLine($"<color={debugTextColor}>VELO: {linVelocity}, {angVelocity}</color>");
            }
            else
            {
                debugText.AppendLine($"<color={debugTextColor}>VELO: -, -</color>");
            }

            // We don't have threshold for the rest of diagnostics
            debugTextColor = "black";
            // bad quality (no threashold yet)
            if (diagnosticScoreDict.TryGetValue(DiagnosticLabel.BadQualityProbability, out var badQuality))
            {
                debugText.AppendLine($"<color={debugTextColor}>BADQ: {badQuality}</color>");
            }

            // targe not visible (no threashold yet)

            if (diagnosticScoreDict.TryGetValue(DiagnosticLabel.TargetNotVisibleProbability, out var notVisible))
            {
                debugText.AppendLine($"<color={debugTextColor}>NVIS: {notVisible}</color>");
            }

            // camera rotation
            if (debugInfo.CameraTransform)
            {
                debugText.AppendLine($"<color={debugTextColor}>ROT: {debugInfo.CameraTransform.eulerAngles}</color>");
            }

            // response time
            var dur = debugInfo.NetworkStatus.EndTimeMs - debugInfo.NetworkStatus.StartTimeMs;
            debugText.AppendLine($"<color={debugTextColor}>NET: {dur}ms {debugInfo.NetworkStatus.Status} {debugInfo.NetworkStatus.Error} </color>");

            return debugText.ToString();
        }
    }
}

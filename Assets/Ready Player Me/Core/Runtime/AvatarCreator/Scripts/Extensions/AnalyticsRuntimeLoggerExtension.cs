using ReadyPlayerMe.Core;
using ReadyPlayerMe.Core.Analytics;
using System.Collections.Generic;

namespace ReadyPlayerMe.AvatarCreator {
    public static class AnalyticsRuntimeLoggerExtension {
        private const string RUN_AVATAR_CREATOR_SAMPLE = "run avatar creator sample";
        private const string APP_ID = "app id";

        public static void LogAvatarCreatorSample(this IAnalyticsRuntimeLogger _, string appId) {
            if (!CoreSettingsHandler.CoreSettings.EnableAnalytics) return;

#if UNITY_EDITOR
            AmplitudeEventLogger.LogEvent(RUN_AVATAR_CREATOR_SAMPLE, new Dictionary<string, object>()
            {
                { APP_ID, appId }
            });
#endif

        }
    }
}

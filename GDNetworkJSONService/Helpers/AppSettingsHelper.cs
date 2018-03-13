using System;
using System.Configuration;
using NLog.Targets.NetworkJSON.ExtensionMethods;

namespace GDNetworkJSONService.Helpers
{
    public class AppSettingsHelper
    {
        public static string DiagnosticsScheduleMode
        {
            get
            {
                var scheduleMode = ConfigurationManager.AppSettings["DiagnosticsScheduleMode"].ToUpper();
                return !scheduleMode.IsNullOrEmpty() ? scheduleMode : "INTERVAL";
            }
        }
        public static DateTime DiagnosticsScheduledTime
        {
            get
            {
                DateTime schDateTime;
                if (DateTime.TryParse(ConfigurationManager.AppSettings["DiagnosticsScheduleTime"], out schDateTime))
                {
                    return schDateTime;
                }
                return DateTime.Now;
            }
        }
        public static int DiagnosticsIntervalSeconds
        {
            get
            {
                var secondsStr = ConfigurationManager.AppSettings["DiagnosticsIntervalSeconds"];
                var seconds = 0;
                //Default of 10 minutes if not present.
                return int.TryParse(secondsStr, out seconds) ? seconds : (600);
            }
        }

        public static bool SkipZeroDiagnostics
        {
            get
            {
                var skipZeroDiagsStr = ConfigurationManager.AppSettings["SkipZeroDiagnostics"];
                var skipZeroDiags = false;
                // Default of true if not present.
                return bool.TryParse(skipZeroDiagsStr, out skipZeroDiags) ? skipZeroDiags : true;
            }
        }

        public static int DbSelectCount
        {
            get
            {
                var dbSelectCountStr = ConfigurationManager.AppSettings["DBSelectCount"];
                var dbSelectCountInt = 0;
                return int.TryParse(dbSelectCountStr, out dbSelectCountInt) ? dbSelectCountInt : 10;
            }
        }

        public static int MultiWritePause
        {
            get
            {
                var multiWritePauseStr = ConfigurationManager.AppSettings["MultiWritePause"];
                var multiWritePauseInt = 0;
                return int.TryParse(multiWritePauseStr, out multiWritePauseInt) ? multiWritePauseInt : 0;
            }
        }

        public static int MinutesToDeadLetter
        {
            get
            {
                var tempMtdlStr = ConfigurationManager.AppSettings["MinutesToDeadLetter"];
                var tempMtdlInt = 0;
                // 4 days is default.
                return int.TryParse(tempMtdlStr, out tempMtdlInt) ? tempMtdlInt : 5760;
            }
        }
    }
}

using System;

namespace Teamownik.Web.Helpers
{
    public static class TimeZoneHelper
    {
        private static TimeZoneInfo? _polandTimeZone;
        public static TimeZoneInfo PolandTimeZone
        {
            get
            {
                if (_polandTimeZone == null)
                {
                    try
                    {
                        _polandTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw");
                    }
                    catch
                    {
                        try
                        {
                            _polandTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
                        }
                        catch
                        {
                            _polandTimeZone = TimeZoneInfo.Local;
                        }
                    }
                }
                return _polandTimeZone;
            }
        }
        
        public static DateTime ToLocalTime(DateTime utcDateTime)
        {
            if (utcDateTime.Kind != DateTimeKind.Utc)
            {
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            }
            
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, PolandTimeZone);
        }

        
        public static DateTime ToUtc(DateTime localDateTime)
        {
            return TimeZoneInfo.ConvertTimeToUtc(localDateTime, PolandTimeZone);
        }
        
        public static string FormatToLocal(DateTime utcDateTime, string format = "dd.MM.yyyy HH:mm")
        {
            var localTime = ToLocalTime(utcDateTime);
            return localTime.ToString(format);
        }
    }


    public static class DateTimeExtensions
    {
     
        public static DateTime ToPolandTime(this DateTime utcDateTime)
        {
            return TimeZoneHelper.ToLocalTime(utcDateTime);
        }
        
        public static string ToLocalFormat(this DateTime utcDateTime, string format = "dd.MM.yyyy HH:mm")
        {
            return TimeZoneHelper.FormatToLocal(utcDateTime, format);
        }
    }
}

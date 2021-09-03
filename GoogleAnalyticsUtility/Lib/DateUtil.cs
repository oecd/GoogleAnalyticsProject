using System;
using System.Text.RegularExpressions;

namespace Oecd.GoogleAnalyticsUtility.Lib
{
    public static class DateUtil
    {

        private static readonly string _xDaysAgoPattern = @"^(?<val>[1-9][0-9]*)(?:daysago)$";
        private static readonly string _xWeeksAgoPattern = @"^(?<val>[1-9][0-9]*)(?:weeksago)$";
        private static readonly string _xMonthsAgoPattern = @"^(?<val>[1-9][0-9]*)(?:monthsago)$";
        private static readonly string _xYearsAgoPattern = @"^(?<val>[1-9][0-9]*)(?:yearsago)$";
        private static readonly string _lastXDaysPattern = @"^(?:last)(?<val>[1-9][0-9]*)(?:days)$";
        private static readonly string _lastXWeeksPattern = @"^(?:last)(?<val>[1-9][0-9]*)(?:weeks)$";
        private static readonly string _lastXMonthsPattern = @"^(?:last)(?<val>[1-9][0-9]*)(?:months)$";
        private static readonly string _lastXYearsPattern = @"^(?:last)(?<val>[1-9][0-9]*)(?:years)$";

        /// <summary>
        /// Evaluate fluent date regarding several patterns (ignoring case):
        /// today, yesterday, [1-9][0-9]*(daysAgo|weeksAgo|monthsAgo|yearsAgo)
        /// </summary>
        /// <param name="value">string to evaluate</param>
        /// <returns>datetime matching the pattern (fallback: today date)</returns>
        public static DateTime EvaluateDate(string value)
        {
            value = value.ToLower();
            return value switch
            {
                "today" => Today,
                "yesterday" => Yesterday,
                var _ when new Regex(_xDaysAgoPattern, RegexOptions.IgnoreCase).IsMatch(value) => XDaysAgo(GetValueFromPattern(_xDaysAgoPattern, value)),
                var _ when new Regex(_xWeeksAgoPattern, RegexOptions.IgnoreCase).IsMatch(value) => XWeeksAgo(GetValueFromPattern(_xWeeksAgoPattern, value)),
                var _ when new Regex(_xMonthsAgoPattern, RegexOptions.IgnoreCase).IsMatch(value) => XMonthsAgo(GetValueFromPattern(_xMonthsAgoPattern, value)),
                var _ when new Regex(_xYearsAgoPattern, RegexOptions.IgnoreCase).IsMatch(value) => XYearsAgo(GetValueFromPattern(_xYearsAgoPattern, value)),
                _ => Today,
            };
        }

        /// <summary>
        /// Evaluate fluent date span regarding several patterns (ignoring case)
        /// <remarks>
        /// <list type="bullet">
        /// <item>
        /// <term>thisWeek, thisMonth, thisYear</term>
        /// <description>returns a date span starting from the first day of the current week, month or year and ending yesterday</description>
        /// </item>
        /// <item>
        /// <term>lastWeek, lastMonth, lastYear</term>
        /// <description>returns a date span starting from the first day of the previous complete week, month or year and ending the last day of this previous complete week, month or year</description>
        /// </item>
        /// <item>
        /// <term>(last)[1-9][0-9]*(days|weeks|months|years)</term>
        /// <description>returns a date span starting from x days, weeks, months or years and ending to last complete day, week, month or year</description>
        /// </item>
        /// </list>
        /// </remarks>
        /// </summary>
        /// <param name="value">fluent date span to evaluate</param>
        /// <returns>a Tuple of dates defining the span</returns>
        public static (DateTime startDate, DateTime endDate) EvaluateDateSpan(string value)
        {
            value = value.ToLower();
            return value switch
            {
                var _ when new Regex(_lastXDaysPattern, RegexOptions.IgnoreCase).IsMatch(value) => LastXDays(GetValueFromPattern(_lastXDaysPattern, value)),
                "thisweek" => ThisWeek,
                "lastweek" => LastWeek,
                var _ when new Regex(_lastXWeeksPattern, RegexOptions.IgnoreCase).IsMatch(value) => LastXWeeks(GetValueFromPattern(_lastXWeeksPattern, value)),
                "thismonth" => ThisMonth,
                "lastmonth" => LastMonth,
                var _ when new Regex(_lastXMonthsPattern, RegexOptions.IgnoreCase).IsMatch(value) => LastXMonths(GetValueFromPattern(_lastXMonthsPattern, value)),
                "thisyear" => ThisYear,
                "lastyear" => LastYear,
                var _ when new Regex(_lastXYearsPattern, RegexOptions.IgnoreCase).IsMatch(value) => LastXYears(GetValueFromPattern(_lastXYearsPattern, value)),
                _ => (Today, Today),
            };
        }

        private static int GetValueFromPattern(string pattern, string value)
        {
            var capt = new Regex(pattern, RegexOptions.IgnoreCase).Match(value).Groups["val"].Value;
            _ = int.TryParse(capt, out var val);
            return val;
        }

        /// DAY ///
        public static DateTime Today => DateTime.Now;
        public static DateTime Yesterday => Today.AddDays(-1);
        public static DateTime XDaysAgo(int value) => Today.AddDays(-value);
        public static (DateTime startDate, DateTime endDate) LastXDays(int value) => ForDay(Today.AddDays(-value), Yesterday);
        public static (DateTime startDate, DateTime endDate) ForDay(DateTime day1, DateTime day2) => (day1, day2);

        /// WEEK ///
        public static DateTime XWeeksAgo(int value) => Today.AddDays(-value * 7);
        public static (DateTime startDate, DateTime endDate) ThisWeek => ForWeek(Today);
        public static (DateTime startDate, DateTime endDate) LastWeek => ForWeek(Today.AddDays(-7));
        public static (DateTime startDate, DateTime endDate) LastXWeeks(int value) => ForWeek(Today.AddDays(-value * 7), Today.AddDays(-7));
        public static (DateTime startDate, DateTime endDate) ForWeek(DateTime value) => (value.StartDateOfWeek(), value.EndDateOfWeek());
        public static (DateTime startDate, DateTime endDate) ForWeek(DateTime week1, DateTime week2) => (week1.StartDateOfWeek(), week2.EndDateOfWeek());

        /// MONTH ///

        public static DateTime XMonthsAgo(int value) => Today.AddMonths(-value);
        public static (DateTime startDate, DateTime endDate) ThisMonth => ForMonth(Today);
        public static (DateTime startDate, DateTime endDate) LastMonth => ForMonth(Today.AddMonths(-1));
        public static (DateTime startDate, DateTime endDate) LastXMonths(int value) => ForMonth(Today.AddMonths(-value), Today.AddMonths(-1));
        public static (DateTime startDate, DateTime endDate) ForMonth(DateTime value) => (value.StartDateOfMonth(), value.EndDateOfMonth());
        public static (DateTime startDate, DateTime endDate) ForMonth(DateTime month1, DateTime month2) => (month1.StartDateOfMonth(), month2.EndDateOfMonth());

        /// YEAR ///
        public static DateTime XYearsAgo(int value) => Today.AddYears(-value);
        public static (DateTime startDate, DateTime endDate) ThisYear => ForYear(Today);
        public static (DateTime startDate, DateTime endDate) LastYear => ForYear(Today.AddYears(-1));
        public static (DateTime startDate, DateTime endDate) LastXYears(int value) => ForYear(Today.AddYears(-value), Today.AddYears(-1));
        public static (DateTime startDate, DateTime endDate) ForYear(DateTime value) => (value.StartDateOfYear(), value.EndDateOfYear());
        public static (DateTime startDate, DateTime endDate) ForYear(DateTime year1, DateTime year2) => (year1.StartDateOfYear(), year2.EndDateOfYear());

    }

    public static class DateTimeExtensions
    {
        public static DateTime StartDateOfWeek(this DateTime value)
        {
            // Monday as the first day of week
            var culture = new System.Globalization.CultureInfo("fr-FR");
            var diff = value.DayOfWeek - culture.DateTimeFormat.FirstDayOfWeek;

            if (diff < 0)
            {
                diff += 7;
            }

            return value.AddDays(-diff).Date;
        }

        public static DateTime EndDateOfWeek(this DateTime value) => value.StartDateOfWeek().AddDays(6);

        public static DateTime StartDateOfMonth(this DateTime value) => new(value.Year, value.Month, 1);

        public static DateTime EndDateOfMonth(this DateTime value) => value.StartDateOfMonth().AddMonths(1).AddDays(-1);

        public static DateTime StartDateOfYear(this DateTime value) => new(value.Year, 1, 1);

        public static DateTime EndDateOfYear(this DateTime value) => value.StartDateOfYear().AddYears(1).AddDays(-1);
    }
}

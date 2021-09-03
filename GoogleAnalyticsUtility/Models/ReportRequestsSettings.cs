using Google.Apis.AnalyticsReporting.v4.Data;
using System.Collections.Generic;

namespace Oecd.GoogleAnalyticsUtility.Models
{
    public class ReportRequestsSettings : ReportRequest
    {
        public string Name { get; set; }
        public IList<string> DateSpans { get; set; }
    }
}

using System.Collections.Generic;

namespace Oecd.GoogleAnalyticsUtility.Models
{
    public class GASettings
    {
        public IList<ReportRequestsSettings> ReportRequestsSettings { get; set; } = new List<ReportRequestsSettings>();
    }
}

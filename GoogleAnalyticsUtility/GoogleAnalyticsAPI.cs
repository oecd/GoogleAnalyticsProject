using Google.Apis.AnalyticsReporting.v4;
using Google.Apis.AnalyticsReporting.v4.Data;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oecd.GoogleAnalyticsUtility.Lib;
using Oecd.GoogleAnalyticsUtility.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Oecd.GoogleAnalyticsUtility.Lib.DateUtil;

namespace Oecd.GoogleAnalyticsUtility
{
    public class GoogleAnalyticsAPI
    {
        public AnalyticsReportingService Service { get; set; }

        private GASettings GaSettings { get; set; }

        public event EventHandler EventLogs;

        /// <summary>
        /// Creates a new GoogleAnalyticsAPI Service to query GoogleAnalytics
        /// </summary>
        /// <param name="applicationName">Name of the application (used for log purpose)</param>
        /// <param name="jsonConfig">the GA json config file containing query definition</param>
        /// <param name="jsonKeyFile">the GA Json key file</param>
        public GoogleAnalyticsAPI(string applicationName, string jsonConfig, string jsonKeyFile)
        {
            try
            {
                GaSettings = JsonConvert.DeserializeObject<GASettings>(jsonConfig);
                CreateService(applicationName, jsonKeyFile);
            }
            catch (Exception)
            {
                throw new Exception("Can't start Google service.");
            }
        }

        /// <summary>
        /// Creates the GoogleAnalyticsAPI Service
        /// </summary>
        /// <param name="applicationName">Name of the application (used for log purpose)</param>
        /// <param name="jsonKeyFile">the GA Json key file</param>
        private void CreateService(string applicationName, string jsonKeyFile)
        {
            GoogleCredential credential;
            credential = GoogleCredential.FromJson(jsonKeyFile)
                .CreateScoped(new[] { AnalyticsReportingService.Scope.AnalyticsReadonly });

            // Create the  Analytics service.
            Service = new AnalyticsReportingService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = applicationName,
            });
        }

        /// <summary>
        /// Returns list of AnalyticReport containing GA raw data
        /// </summary>
        /// <returns>list of AnalyticReport containing GA raw data</returns>
        public List<AnalyticReport> GetAnalyticReports()
        {
            var reportRequestsSettings = GaSettings.ReportRequestsSettings;
            var analyticReports = new List<AnalyticReport>();
            foreach (ReportRequestsSettings reportRequestsSetting in reportRequestsSettings)
            {
                Log($"Fetching Usage Raw Data from GA (report '{reportRequestsSetting.Name}')");
                var analyticReport = GetAnalyticReport(reportRequestsSetting);
                Log($"Raw Data from GA fetched ({analyticReport.Rows.Count} rows)");
                analyticReports.Add(analyticReport);
            }
            return analyticReports;
        }


        /// <summary>
        /// Take a reportRequestsSetting and build the GA query to get raw data
        /// </summary>
        /// <param name="reportRequestsSetting">reportRequestsSetting contains all properties for the GA querys</param>
        /// <returns>AnalyticReport containing GA raw data</returns>
        private AnalyticReport GetAnalyticReport(ReportRequestsSettings reportRequestsSetting)
        {
            AnalyticReport data = new();

            GetReportsResponse response = null;
            do
            {
                string pageToken = response?.Reports[0].NextPageToken;
                IList<DateRange> toDateRanges = TranslateDate(reportRequestsSetting.DateRanges, reportRequestsSetting.DateSpans);
                var request = BuildReportRequest(
                   viewId: reportRequestsSetting.ViewId,
                   dim: reportRequestsSetting.Dimensions,
                   metr: reportRequestsSetting.Metrics,
                   dates: toDateRanges,
                   dimensionFilterClauses: reportRequestsSetting.DimensionFilterClauses,
                   pageToken: pageToken);
                response = request.Execute();

                if (response.Reports.First().Data.RowCount == null)
                {
                    return data;
                }

                data.Rows.AddRange(response.Reports.First().Data.Rows);
                data.ColumnHeader = response.Reports.First().ColumnHeader;
                data.ReportName = reportRequestsSetting.Name;
                data.ReportDateSpan = GetReportDateSpan(toDateRanges);

            } while (!string.IsNullOrEmpty(response.Reports.First().NextPageToken));

            return data;
        }

        /// <summary>
        /// Build a GA query
        /// </summary>
        /// <param name="viewId"> Analytics View ID containing the data associated with the user</param>
        /// <param name="dim">List of wanted dimensions</param>
        /// <param name="metr">List of wanted metrics</param>
        /// <param name="dates">List of date ranges</param>
        /// <param name="dimensionFilterClauses">List of filter on dimension</param>
        /// <param name="pageToken">For pagination purpose</param>
        /// <returns>the builted GA query</returns>
        private ReportsResource.BatchGetRequest BuildReportRequest(
            string viewId, IList<Dimension> dim, IList<Metric> metr, IList<DateRange> dates, IList<DimensionFilterClause> dimensionFilterClauses, string pageToken = null)
        {
            var reportRequest = new ReportRequest
            {
                DateRanges = dates,
                Dimensions = dim,
                Metrics = metr,
                ViewId = viewId,
                PageToken = pageToken,
                DimensionFilterClauses = dimensionFilterClauses
            };

            var getReportsRequest = new GetReportsRequest
            {
                ReportRequests = new List<ReportRequest> { reportRequest }
            };
            ReportsResource.BatchGetRequest request = Service.Reports.BatchGet(getReportsRequest);
            return request;
        }

        /// <summary>
        /// Get a daterange or a datespan and translate it into a valid GA daterange
        /// </summary>
        /// <param name="dateRanges">List of dateranges to be tranbslated into valid GA dateranges</param>
        /// <param name="dateSpans">List of dateSpans to be tranbslated into GA dateranges</param>
        /// <returns>List of valid dateranges</returns>
        private static IList<DateRange> TranslateDate(IList<DateRange> dateRanges, IList<string> dateSpans)
        {
            IList<DateRange> dateRangesList = new List<DateRange>();
            if (dateSpans?.Count > 0)
            {
                foreach (string dateSpan in dateSpans)
                {
                    var (startDate, endDate) = GetDateValueFromSpan(dateSpan);
                    dateRangesList.Add(new DateRange()
                    {
                        StartDate = startDate,
                        EndDate = endDate
                    });
                }
            }
            else
            {
                foreach (DateRange dateRange in dateRanges)
                {
                    dateRangesList.Add(new DateRange()
                    {
                        StartDate = GetDateValue(dateRange.StartDate),
                        EndDate = GetDateValue(dateRange.EndDate)
                    });
                }
            }
            return dateRangesList;
        }

        /// <summary>
        /// AnalyticReport model
        /// </summary>
        public class AnalyticReport
        {
            public AnalyticReport()
            {
                Rows = new List<ReportRow>();
            }
            public string ReportName { get; set; }
            public string ReportDateSpan { get; set; }
            public ColumnHeader ColumnHeader { get; set; }
            public List<ReportRow> Rows { get; set; }
        }

        /// <summary>
        /// Very simple log based on event creation and subsrciption from the caller
        /// </summary>
        /// <param name="message">message to log</param>
        private void Log(string message)
        {
            EventLogs?.Invoke(this, new EventLogs() { Message = message });
        }

        /// <summary>
        /// Take a date or fluent date (ie: yesterday) and transform it to a valid GA date
        /// </summary>
        /// <param name="value"> a date or fluent date</param>
        /// <returns> a valid GA date</returns>
        private static string GetDateValue(string value)
        {
            if (DateTime.TryParse(value, out var dateValue))
                return ToGADateFormat(dateValue);
            else
                return ToGADateFormat(EvaluateDate(value));
        }

        /// <summary>
        /// take a date span (ie: last3weeks) and transform it to a valid GA daterange
        /// </summary>
        /// <param name="value"> a date span</param>
        /// <returns> a valid GA daterange</returns>
        private static (string startDate, string endDate) GetDateValueFromSpan(string value)
        {
            var (startDate, endDate) = EvaluateDateSpan(value);
            return ToGADateFormat(startDate, endDate);
        }

        /// <summary>
        /// get DateTime to Google Analytics Date Format
        /// </summary>
        /// <param name="value">input DateTime</param>
        /// <returns>string datetime formated to "yyyy-MM-dd"</returns>
        public static string ToGADateFormat(DateTime value) => value.ToString("yyyy-MM-dd");
        /// <summary>
        /// get Tuple DateTime to Tuple Google Analytics Date Format
        /// </summary>
        /// <param name="startDate">input DateTime</param>
        /// <param name="endDate">input DateTime</param>
        /// <returns>Tuple of strings datetime formated to "yyyy-MM-dd"</returns>
        public static (string startDate, string endDate) ToGADateFormat(DateTime startDate, DateTime endDate) => (ToGADateFormat(startDate), ToGADateFormat(endDate));

        /// <summary>
        /// Use to generate a string containing the start dates and the end dates of a list of datespans
        /// </summary>
        /// <param name="dateRanges"> a list of datespans</param>
        /// <returns> a string containing the start and end dates</returns>
        private static string GetReportDateSpan(IList<DateRange> dateRanges) => string.Join("_and_", dateRanges.Select(x => $"{x.StartDate}_to_{x.EndDate}"));

        /// <summary>
        /// Merge a system json config file with a user config file containing date ranges and report name
        /// </summary>
        /// <param name="sysConfigPath">Path to the GA system config file containing the reportRequestsSettings template with Dimension, Metrics and dimensionFilterClauses</param>
        /// <param name="userConfigPath">Path to the GA user config file containing reportRequestsSettings with DateRanges or DateSpans</param>
        /// <param name="filterValue">if any a value used as a dimension filter (ex: country value)</param>
        /// <returns>A merged a GA system config file</returns>
        public static string MergeSystemAndUserConfigFiles(string sysConfigPath, string userConfigPath, string filterValue = "")
        {
            StreamReader srSysConfig = new(sysConfigPath);
            string jsonSysConfig = srSysConfig.ReadToEnd();
            if (!string.IsNullOrEmpty(filterValue))
            {
                jsonSysConfig = jsonSysConfig.Replace("#filterValueToBeReplaced#", filterValue);
            }
            JObject joSysConfig = JObject.Parse(jsonSysConfig);
            JArray jAReportRequestsSettingsTemplate = (JArray)joSysConfig["reportRequestsSettings"];
            // copy all props from the template
            JObject joReportRequestsSettingsTemplate = (JObject)jAReportRequestsSettingsTemplate[0].DeepClone();

            StreamReader srUserConfig = new(userConfigPath);
            string jsonUserConfig = srUserConfig.ReadToEnd();
            JObject joUserConfig = JObject.Parse(jsonUserConfig);

            JEnumerable<JObject> jeReportRequestsSettings = joUserConfig.SelectToken("reportRequestsSettings").Children<JObject>();
            foreach (JObject joReportRequestsSettings in jeReportRequestsSettings)
            {
                // for each user request definition add props from the template
                joReportRequestsSettings.Merge(joReportRequestsSettingsTemplate);
                jAReportRequestsSettingsTemplate.Add(joReportRequestsSettings);
            }
            // remove the template
            jAReportRequestsSettingsTemplate.RemoveAt(0);
            return JsonConvert.SerializeObject(joSysConfig, Formatting.Indented);
        }

        /// <summary>
        /// Generate a GA config file with a config file and start and end dates
        /// </summary>
        /// <param name="configPath">json content of the GA config file</param>
        /// <param name="startDate">start and end dates to be injected into the GA config</param>
        /// <param name="endDate">end dates to be injected into the GA config</param>
        /// <returns> GA config file with a config file and start and end dates</returns>
        public static string SetDateRange(string configPath, string startDate, string endDate)
        {
            StreamReader srSysConfig = new(configPath);
            string jsonSysConfig = srSysConfig.ReadToEnd();
            JObject joSysConfig = JObject.Parse(jsonSysConfig);
            JArray jAReportRequestsSettings = (JArray)joSysConfig["reportRequestsSettings"];
            // copy all props from the template
            JObject joReportRequestsSettingsTemplate = (JObject)jAReportRequestsSettings[0];

            JProperty jpDateRange = new("DateRanges", new JArray(
                new JObject()
                {
                    new JProperty("startDate", startDate),
                    new JProperty("endDate", endDate)
                }
             ));
            joReportRequestsSettingsTemplate.Add(jpDateRange);
            return JsonConvert.SerializeObject(joSysConfig, Formatting.Indented);
        }
    }
}

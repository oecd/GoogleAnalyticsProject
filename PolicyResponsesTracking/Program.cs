using ClosedXML.Excel;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Oecd.GoogleAnalyticsUtility;
using Oecd.GoogleAnalyticsUtility.Lib;
using Oecd.Utilities;
using System;
using System.IO;
using static Oecd.GoogleAnalyticsUtility.GoogleAnalyticsAPI;
using static Oecd.PolicyResponsesTrackingUtil.Lib.PolicyResponsesTrackingUtil;

namespace Oecd.PolicyReponsesTracking
{
    class Program
    {
        enum ExitCode : int
        {
            Success = 0,
            InvalidArguments = 1,
            UnknownError = 10
        }
        static int Main(string[] args)
        {
            // return if no userConfig file was dragged onto exe
            if (args == null || args.Length == 0 || !File.Exists(args[0]))
            {
                Console.WriteLine("You must provide the userConfig file");
                Console.WriteLine("Press any key to close the application");
                Console.ReadKey();
                return (int)ExitCode.InvalidArguments; ; ;
            }

            var userGASettingsPath = args[0]; //@"E:\Temp\userGAsettings.json";

            var currentDirectory = Path.GetDirectoryName(AppContext.BaseDirectory);
            var ressourcesDirectory = Path.Combine(currentDirectory, "ressources");
            var outDirectory = Path.Combine(currentDirectory, "OUT");
            var pathToGAsettings = Path.Combine(ressourcesDirectory, @"GAsettings.json");

            var builder = new ConfigurationBuilder()
                .SetBasePath(currentDirectory)
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.specific.json", optional: false, reloadOnChange: true);
            var configuration = builder.Build();

            var configurationServiceAPI = new ConfigurationServiceAPI(configuration.GetValue<string>("ConfigurationServiceAPIAddress"));
            var kappaApiKeyProperty = configurationServiceAPI.GetConfigurationFile(configuration.GetValue<string>("ConfigurationServiceAPIEndpoint"), configuration.GetValue<string>("KappaApiKeyfile"));
            var kappaApiKey = JObject.Parse(kappaApiKeyProperty)["kappaApiKey"].ToString();

            /// /////////////////////////////////////////////////////
            /// Fetching Policy Responses metadata from Kappa
            /// /////////////////////////////////////////////////////
            Console.WriteLine("Fetching Policy Responses metadata from Kappa");
            var kappaApi = new KappaMiddlewareAPI(kappaApiKey,
                                                  configuration.GetValue<string>("kappaMiddlewareAddress"));
            var xDocKappaData = kappaApi.GetReport(configuration.GetValue<string>("kappaMiddlewareEndpoint"));
            var dtKappaData = GenerateKappaDataTable(xDocKappaData);
            Console.WriteLine($"Metadata from Kappa fetched ({dtKappaData.Rows.Count} rows)");

            /// /////////////////////////////////////////////////////
            /// Fetching Google Analytics Data
            /// /////////////////////////////////////////////////////
            var applicationName = configuration.GetValue<string>("applicationName");
            var googleAnalyticsKey = configurationServiceAPI.GetConfigurationFile(configuration.GetValue<string>("ConfigurationServiceAPIEndpoint"), configuration.GetValue<string>("GoogleAnalyticsKey"));

            var mergedJsonConfigFile = MergeSystemAndUserConfigFiles(pathToGAsettings, userGASettingsPath);
            var ga = new GoogleAnalyticsAPI(applicationName, mergedJsonConfigFile, googleAnalyticsKey);
            ga.EventLogs += SubprocessEventLogs;
            var analyticReports = ga.GetAnalyticReports();
            foreach (AnalyticReport analyticReport in analyticReports)
            {
                Console.WriteLine("Generate DataTable from GA Raw Data (Get id from url) ");
                var dtCleaned = GenerateCleanedDataTable(analyticReport);
                Console.WriteLine($"DataTable generated");

                Console.WriteLine("Refining GA Raw Data (Grouped by Policy responses Id) ");
                var dtGroupedByRef = GroupDataTableByRef(dtCleaned);
                Console.WriteLine($"GA Raw Data Refined ({dtGroupedByRef.Rows.Count} rows)");


                Console.WriteLine("Merging Policy Responses metadata and Refined GA Raw Data (based on Policy responses Id)");
                var dtMerged = MergeKappaAndGADataTables(dtGroupedByRef, dtKappaData);
                Console.WriteLine($"Policy Responses metadata and Refined GA Raw Data merged ({dtMerged.Rows.Count} rows)");

                Console.WriteLine("Building Excel worksheet");
                var wb = new XLWorkbook();
                wb.Worksheets.Add(dtMerged, GetExcelValidSheetName($"PR Track. {analyticReport.ReportName}"));
                wb.Worksheets.Add(dtKappaData, GetExcelValidSheetName("Kappa PR Medata"));
                wb.Worksheets.Add(dtCleaned, GetExcelValidSheetName("_DEBUG PR Track. RAW data"));
                wb.Worksheets.Add(dtGroupedByRef, GetExcelValidSheetName("_DEBUG PR Track. Grouped by id"));

                var filename = ReplaceInvalidChars($"PolicyResponsesTracking_{analyticReport.ReportDateSpan}_generatedOn_{DateTime.Now:yyyyMMddhhmm}.xlsx");
                var pathToSave = Path.Combine(outDirectory, filename);
                wb.SaveAs(pathToSave);
                Console.WriteLine($"Report saved here: {pathToSave}");
            }
            Console.WriteLine("Press any key to close the application");
            Console.ReadKey();
            return (int)ExitCode.Success;
        }

        private static string GetExcelValidSheetName(string sheetName)
        {
            return sheetName.Truncate(31);
        }

        private static string ReplaceInvalidChars(string filename)
        {
            return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
        }

        /// <summary>
        /// Get log of the subprocess
        /// </summary>
        public static void SubprocessEventLogs(object sender, EventArgs e)
        {
            Console.WriteLine((e as EventLogs).Message);
        }


    }
}

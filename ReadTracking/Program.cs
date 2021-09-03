using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Oecd.GoogleAnalyticsUtility;
using Oecd.GoogleAnalyticsUtility.Lib;
using Oecd.Utilities;
using System;
using System.IO;
using System.Text;
using static Oecd.GoogleAnalyticsUtility.GoogleAnalyticsAPI;
using static Oecd.ReadTracking.Lib.ReadTrackingUtil;

namespace Oecd.ReadTracking
{
    class Program
    {
        enum ExitCode : int
        {
            Success = 0,
            InvalidLengthArguments = 1,
            UnknownError = 10
        }


        static int Main(string[] args)
        {
            //return if no argument
            if (args == null || args.Length < 2)
            {
                Console.WriteLine("You must provide a date and a full path to generate the output csv file");
                return (int)ExitCode.InvalidLengthArguments; ;
            }

            var date = args[0]; //"2021-08-14"; //
            var csvPath = args[1];// @$"E:\Temp\Us_Test\Script\OUT\FreePreview_{date}.csv"; //

            var currentDirectory = Path.GetDirectoryName(AppContext.BaseDirectory);
            var ressourcesDirectory = Path.Combine(currentDirectory, "ressources");
            var pathToGAsettings = Path.Combine(ressourcesDirectory, @"GAsettings.json");

            var builder = new ConfigurationBuilder()
               .SetBasePath(currentDirectory)
               .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
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

            var finalJsonConfigFile = SetDateRange(pathToGAsettings, date, date);
            var ga = new GoogleAnalyticsAPI(applicationName, finalJsonConfigFile, googleAnalyticsKey);
            ga.EventLogs += SubprocessEventLogs;
            //only 1 report for Read Service
            var analyticReport = ga.GetAnalyticReports()[0];

            Console.WriteLine("Generate DataTable from GA Raw Data (Get id and ilibrary url from url) ");
            var dtWithId = ToDataTableWithIdField(analyticReport, dtKappaData);
            Console.WriteLine($"DataTable generated ({ dtWithId.Rows.Count} rows)");

            Console.WriteLine("Refining GA Raw Data (Grouped by ilibrary url) ");
            var dtGroupedByRef = GroupDataTableByRef(dtWithId);
            Console.WriteLine($"GA Raw Data Refined ({dtGroupedByRef.Rows.Count} rows)");

            Console.WriteLine("Modeling datable to FreePreview Raw Format");
            var dtFreePreviewRawFormat = ToFreePreviewRawFormat(dtGroupedByRef);
            Console.WriteLine($"Modeling datable over ({dtFreePreviewRawFormat.Rows.Count} rows)");

            dtFreePreviewRawFormat.ToCsvFile(csvPath, Encoding.GetEncoding(1200));
            Console.WriteLine($"Csv file saved here: {csvPath}");
            return (int)ExitCode.Success;
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

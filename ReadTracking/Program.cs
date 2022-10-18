using CommandLine;
using CommandLine.Text;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Oecd.GoogleAnalyticsUtility;
using Oecd.GoogleAnalyticsUtility.Lib;
using Oecd.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using static Oecd.GoogleAnalyticsUtility.GoogleAnalyticsAPI;
using static Oecd.ReadTracking.Lib.ReadTrackingUtil;

namespace Oecd.ReadTracking
{
    class Program
    {
        private enum ExitCode : int
        {
            Success = 0,
            InvalidLengthArguments = 1,
            UnknownError = 10
        }

        public enum ReportType
        {
            dateRange,
            daily
        }

        private static IEnumerable<DateTime> EachDay(DateTime from, DateTime thru)
        {
            for (var day = from.Date; day.Date <= thru.Date; day = day.AddDays(1))
                yield return day;
        }

        public sealed class Options
        {
            [Option('s', "startdate", Required = true, HelpText = "Set the start date of report or the start date of the date range for reports (date must follow the ISO8601 date format YYYY-MM-DD).")]
            public string StartDate { get; set; }

            [Option('e', "enddate", Required = true, HelpText = "Set the end date of report or the end date of the date range for reports (date must follow the ISO8601 date format YYYY-MM-DD).")]
            public string EndDate { get; set; }

            [Option('r', "reportby", Required = true, HelpText = "Available values are: 'daily' or 'daterange'. Using 'daterange' will generate a single cumulative report for the entire date range defined by startdate and endate. Using 'daily' will generate as many daily reports as many days between startdate and endate.")]
            public ReportType ReportBy { get; set; }

            [Option('o', "outputdirectory", Required = true, HelpText = "Path where the report(s) will be generated.")]
            public string OutputDirectory { get; set; }

            [Usage(ApplicationAlias = "ReadTracking")]
            public static IEnumerable<Example> Examples
            {
                get
                {
                    return new List<Example>() {
                        new Example("Generate 5 reports for every day beetween the 2022-01-01 and the 2022-01-05", new Options { StartDate = "2022-01-01", EndDate = "2022-01-05", ReportBy = ReportType.daily, OutputDirectory = @"E:\ReportData" }),
                        new Example("Generate on single report for the daterange 2022-01-01 to 2022-01-05", new Options { StartDate = "2022-01-01", EndDate = "2022-01-05", ReportBy = ReportType.dateRange, OutputDirectory = @"E:\ReportData" })
                  };
                }
            }

        }

        private static DataTable DtKappaData = new DataTable();
        private static string PathToGAsettings;
        private static string ApplicationName;
        private static string GoogleAnalyticsKey;
        private static string OutPath;
        private static DateTime StartDate;
        private static DateTime EndDate;

        public static int Main(string[] args)
        {
            var parserResult = new Parser(config =>
                {
                    config.HelpWriter = null;
                    config.CaseInsensitiveEnumValues = true;
                }
                ).ParseArguments<Options>(args);
            return parserResult.MapResult(
                (Options opts) => Run(opts),
                errs => DisplayHelp(parserResult, errs)
                );
        }

        private static int DisplayHelp(ParserResult<Options> parserResult, IEnumerable<Error> errs)
        {
            HelpText helpText = null;
            if (errs.IsVersion())  //check if error is version request
                helpText = HelpText.AutoBuild(parserResult);
            else
            {
                helpText = HelpText.AutoBuild(parserResult, h =>
                {
                    //configure help
                    h.AdditionalNewLineAfterOption = false;
                    return HelpText.DefaultParsingErrorsHandler(parserResult, h);
                }, e => e);
            }
            Console.WriteLine(helpText);
            return (int)ExitCode.InvalidLengthArguments;
        }

        private static int Run(Options options)
        {
            StartDate = DateTime.ParseExact(options.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture); //"2021-08-14"; //
            EndDate = DateTime.ParseExact(options.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture); //"2021-08-14"; //

            OutPath = options.OutputDirectory;// @$"E:\Temp\Us_Test\Script\OUT"; //
            if (!Directory.Exists(OutPath))
            {
                Directory.CreateDirectory(OutPath);
            }

            var currentDirectory = Path.GetDirectoryName(AppContext.BaseDirectory);
            var ressourcesDirectory = Path.Combine(currentDirectory, "ressources");
            PathToGAsettings = Path.Combine(ressourcesDirectory, @"GAsettings.json");

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
            DtKappaData = GenerateKappaDataTable(xDocKappaData);
            Console.WriteLine($"Metadata from Kappa fetched ({DtKappaData.Rows.Count} rows)");

            /// /////////////////////////////////////////////////////
            /// Fetching Google Analytics Data
            /// /////////////////////////////////////////////////////
            ApplicationName = configuration.GetValue<string>("applicationName");
            GoogleAnalyticsKey = configurationServiceAPI.GetConfigurationFile(configuration.GetValue<string>("ConfigurationServiceAPIEndpoint"), configuration.GetValue<string>("GoogleAnalyticsKey"));
            if (options.ReportBy == ReportType.dateRange)
            {
                GenerateReport(StartDate.ToString("yyyy-MM-dd"), EndDate.ToString("yyyy-MM-dd"));
            }
            else
            {
                foreach (DateTime day in EachDay(StartDate, EndDate))
                {
                    var date = day.ToString("yyyy-MM-dd");
                    GenerateReport(date, date);
                }
            }
            return (int)ExitCode.Success;
        }

        private static void GenerateReport(string startdate, string enddate)
        {
            var finalJsonConfigFile = SetDateRange(PathToGAsettings, startdate, enddate);
            var ga = new GoogleAnalyticsAPI(ApplicationName, finalJsonConfigFile, GoogleAnalyticsKey);
            ga.EventLogs += SubprocessEventLogs;
            //only 1 report for Read Service
            var analyticReport = ga.GetAnalyticReports()[0];

            Console.WriteLine("Generate DataTable from GA Raw Data (Get id and ilibrary url from url) ");
            var dtWithId = ToDataTableWithIdField(analyticReport, DtKappaData);
            Console.WriteLine($"DataTable generated ({dtWithId.Rows.Count} rows)");

            Console.WriteLine("Refining GA Raw Data (Grouped by ilibrary url) ");
            var dtGroupedByRef = GroupDataTableByRef(dtWithId);
            Console.WriteLine($"GA Raw Data Refined ({dtGroupedByRef.Rows.Count} rows)");

            Console.WriteLine("Modeling datable to FreePreview Raw Format");
            var dtFreePreviewRawFormat = ToFreePreviewRawFormat(dtGroupedByRef);
            Console.WriteLine($"Modeling datable over ({dtFreePreviewRawFormat.Rows.Count} rows)");
            var date = startdate == enddate ? startdate : $"{startdate}_to_{enddate}";
            var filename = $"FreePreview_{date}.csv";
            var pathToSave = Path.Combine(OutPath, filename);
            dtFreePreviewRawFormat.ToCsvFile(pathToSave, Encoding.GetEncoding(1200));
            Console.WriteLine($"Csv file saved here: {pathToSave}");

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

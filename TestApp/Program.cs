using Microsoft.Extensions.Configuration;
using Oecd.GoogleAnalyticsUtility;
using Oecd.GoogleAnalyticsUtility.Lib;
using Oecd.Utilities;
using System;
using System.IO;

namespace TestApp
{
    class Program
    {
        enum ExitCode : int
        {
            Success = 0,
            InvalidLengthArguments = 1,
            UnknownError = 10
        }
        static int Main()
        {
            var currentDirectory = Path.GetDirectoryName(AppContext.BaseDirectory);
            var ressourcesDirectory = Path.Combine(currentDirectory, "ressources");
            var pathToGAsettings = Path.Combine(ressourcesDirectory, @"GAsettings.json");

            var builder = new ConfigurationBuilder()
                .SetBasePath(ressourcesDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            var configuration = builder.Build();

            var configurationServiceAPI = new ConfigurationServiceAPI(configuration.GetValue<string>("ConfigurationServiceAPIAddress"));
            /// /////////////////////////////////////////////////////
            /// Fetching Google Analytics Data
            /// /////////////////////////////////////////////////////
            var applicationName = configuration.GetValue<string>("applicationName");
            var googleAnalyticsKey = configurationServiceAPI.GetConfigurationFile(configuration.GetValue<string>("ConfigurationServiceAPIEndpoint"), configuration.GetValue<string>("GoogleAnalyticsKey"));

            StreamReader srSysConfig = new(pathToGAsettings);
            string jsonSysConfig = srSysConfig.ReadToEnd();
            var ga = new GoogleAnalyticsAPI(applicationName, jsonSysConfig, googleAnalyticsKey);
            ga.EventLogs += SubprocessEventLogs;
            //only 1 report for Read Service
            var analyticReport = ga.GetAnalyticReports()[0];

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

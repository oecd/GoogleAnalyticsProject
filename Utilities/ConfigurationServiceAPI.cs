using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Oecd.Utilities
{
    public class ConfigurationServiceAPI
    {
        private static string ConfigurationAPIAddress { get; set; }
        private HttpClient Client { get; }
        private UriBuilder Builder { get; set; }

        /// <summary>
        /// Instanties a new API connection to Configuration Service API
        /// </summary>
        /// <param name="Address">API URL</param>
        public ConfigurationServiceAPI(string Address)
        {
            HttpClientHandler handler = new() { UseProxy = false, UseDefaultCredentials = true };


            Client = new HttpClient(handler);
            ConfigurationAPIAddress = Address;
        }

        /// <summary>
        /// <summary>
        /// Get a configuration file from Configuration Service API
        /// </summary>
        /// <param name="endpoint">Configuration Service API endpoint</param>
        /// <returns>the content of the configuration file</returns>
        public string GetConfigurationFile(string endpoint, string file)
        {
            Builder = new UriBuilder(ConfigurationAPIAddress);
            SetEndpoint(endpoint + file);
            return CallConfigurationFile().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Call Configuration Service API to get a configuration file
        /// </summary>
        /// <returns>A string containig the configuration file</returns>
        private async Task<string> CallConfigurationFile()
        {
            // Call the endpoint
            HttpResponseMessage response = await Client.GetAsync(Builder.Uri, HttpCompletionOption.ResponseHeadersRead);

            // Throws an exception if the request fails
            response.EnsureSuccessStatusCode();

            string configurationFile = await response.Content.ReadAsStringAsync();

            return configurationFile;
        }
        /// <summary>
        /// Set the endpoint
        /// </summary>
        /// <param name="endpoint">the computed endpoint</param>
        private void SetEndpoint(string endpoint)
        {
            Builder.Path += endpoint;
        }
    }
}

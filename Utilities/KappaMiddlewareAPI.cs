using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Oecd.Utilities
{
    public class KappaMiddlewareAPI
    {
        private static string KappaMiddlewareAddress { get; set; }
        private HttpClient Client { get; }
        private UriBuilder Builder { get; set; }

        /// <summary>
        /// Instanties a new API connectiopn to KappaMiddleware API
        /// </summary>
        /// <param name="Key">token</param>
        /// <param name="Address">API URL</param>
        public KappaMiddlewareAPI(string Key, string Address)
        {
            HttpClientHandler handler = new() { UseProxy = false };

            Client = new HttpClient(handler);
            KappaMiddlewareAddress = Address;

            // Authentication as a Bearer token
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Key);

            // Accept only XML
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));
        }

        /// <summary>
        /// Get PR xml Report from Kappa
        /// </summary>
        /// <param name="endpoint">Kappa PR API endpoint</param>
        /// <returns>A XDocument</returns>
        public XDocument GetReport(string endpoint)
        {
            Builder = new UriBuilder(KappaMiddlewareAddress);
            SetEndpoint(endpoint);
            AddParameter("format", "xml");
            return CallReport().GetAwaiter().GetResult();
        }
        /// <summary>
        /// Call Kappi API to get a report
        /// </summary>
        /// <returns>A XDocument containig the report</returns>
        private async Task<XDocument> CallReport()
        {
            // Call the endpoint
            HttpResponseMessage response = await Client.GetAsync(Builder.Uri, HttpCompletionOption.ResponseHeadersRead);

            // Throws an exception if the request fails
            response.EnsureSuccessStatusCode();

            string content = await response.Content.ReadAsStringAsync();
            XDocument document = XDocument.Parse(content);

            return document;
        }
        /// <summary>
        /// Set the Kappa endpoint
        /// </summary>
        /// <param name="endpoint">the computed endpoint</param>
        private void SetEndpoint(string endpoint)
        {
            Builder.Path += endpoint;
        }

        /// <summary>
        /// Add an URL param for the endpoint URL
        /// </summary>
        /// <param name="key">name of the url param</param>
        /// <param name="value">value of the url param</param>
        private void AddParameter(string key, string value)
        {
            // Add the query if it doesn't exists already
            if (string.IsNullOrEmpty(Builder.Query))
            {
                Builder.Query = $"{key}={value}";
            }
            // If the query has some parameters, adding the new one at the end (without the first "?" and preceded by "&")
            else
            {
                Builder.Query = $"{Builder.Query[1..]}&{key}={value}";
            }
        }
    }
}

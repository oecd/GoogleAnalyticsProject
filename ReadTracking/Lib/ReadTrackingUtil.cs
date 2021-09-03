using Oecd.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using static Oecd.GoogleAnalyticsUtility.GoogleAnalyticsAPI;

namespace Oecd.ReadTracking.Lib
{
    public static class ReadTrackingUtil
    {
        private static readonly string _readURLWithRef = "read.oecd-ilibrary.org/view/?ref=";

        /// <summary>
        /// used to filter raw data from GA to get only urls related to PR
        /// </summary>
        /// <param name="url">url from GA raw data</param>
        /// <returns>boolean result</returns>
        public static bool IsRefUrl(string url)
        {
            url = url.ToLower();
            return
                //readURL part is present
                url.Contains(_readURLWithRef)
                // then part after "..?ref=" must have at least 21 chars
                && url.After(_readURLWithRef).Length >= 21; ;
        }

        /// <summary>
        /// get the Ilibrary Url contained in url or id for policy reponse
        /// </summary>
        /// <param name="url">url of GA</param>
        /// <returns>Ilibrary Url or id of the content</returns>
        public static string GetRefFromUrl(string url)
        {
            //remove all control and other non-printable characters
            url = Regex.Replace(url, @"\p{C}+", string.Empty).ToLower();
            return IsRefUrl(url) ? GetIdFromReadUrlWithRef(url) : GetIlibraryUrlFromClassicReadUrl(url);
        }

        /// <summary>
        /// get the policy reponse id from Read url (containing ?ref= in the url)
        /// </summary>
        /// <param name="url">url from Read url</param>
        /// <returns>id of the PR</returns>
        private static string GetIdFromReadUrlWithRef(string url)
        {
            // get the part after "...?ref="
            var urlcleaned = url.After(_readURLWithRef);
            // remove url params and other intrusive separators/words found in GA raw data
            urlcleaned = urlcleaned.Split(new string[] { "&", "?", "\\", "title", "country", "hyperlink", "and" }, StringSplitOptions.None)[0];
            return urlcleaned.Trim();
        }

        /// <summary>
        /// get the Ilibrary Url contained in url (not for Policy reponses)
        /// </summary>
        /// <param name="url">url of GA</param>
        /// <returns>Ilibrary Url  of the content</returns>
        private static string GetIlibraryUrlFromClassicReadUrl(string url)
        {
            // there are stil some commonwealth content.... (should not be there)
            var urlcleaned = url.Contains("/commonwealth/") ? url.After("read.oecd-ilibrary.org/commonwealth/") : url.After("read.oecd-ilibrary.org/");
            // remove url params and other intrusive separators
            urlcleaned = urlcleaned.Split(new string[] { "&", "?", "\\" }, StringSplitOptions.None)[0];
            return urlcleaned.Trim();
        }

        /// <summary>
        /// Get a Datatable from the XML document of the PR repor
        /// </summary>
        /// <param name="document">XML document of the PR report</param>
        /// <returns>A Datatable ready to be merged with GA data (extra REF column)</returns>
        public static DataTable GenerateKappaDataTable(XDocument document)
        {
            var dt = new DataTable();

            /* ************************* */
            /*        columns            */
            /* ************************* */
            // columns order : REF, all the others
            var columns = new List<DataColumn>();

            XElement root = document.Root;
            XElement firstRow = root.Descendants("row").First();

            List<XElement> xCols;
            //get header cells labels
            xCols = firstRow.Descendants("cell").ToList();
            //add 'REF' column to store the computed id of the policy responses
            columns.Add(new DataColumn("REF"));
            // all the other columns
            columns.AddRange(xCols.Select(c => new DataColumn(c.Value)));
            // insert colums in the datatable
            dt.Columns.AddRange(columns.ToArray());
            // set the primary key to be able to join with other GA datatable
            dt.PrimaryKey = new DataColumn[] { dt.Columns["REF"] };

            /* ************************* */
            /*          rows             */
            /* ************************* */
            List<XElement> xrows = root.Descendants("row").Skip(1).ToList();

            // columns label that contained the id 
            var cellsURLNames = new List<string> { "mediahub link" };
            foreach (XElement xrow in xrows)
            {
                List<XElement> xCells;
                xCells = xrow.Descendants("cell").ToList();
                var xUrl = xCells.FirstOrDefault(c => cellsURLNames.Contains(c.Attribute("name").Value) && c.Value.Length > 0)?.Value;
                // some policy responses has only a blog url (no oecd.org or read)
                if (string.IsNullOrEmpty(xUrl) || !IsRefUrl(xUrl))
                    continue;
                var refValue = GetRefFromUrl(xUrl);

                var r = dt.NewRow();
                var data = new List<string>
                {
                    refValue
                };
                // add all values
                data.AddRange(xCells.Select(c => c.Value));

                for (var i = 0; i < data.Count; i++)
                {
                    r[i] = data[i];
                }

                dt.Rows.Add(r);
            }

            return dt;
        }

        /// <summary>
        /// Transfrom an AnalyticReport into a datatable with an identifier (Ilibrary Url)
        /// </summary>
        /// <param name="report">AnalyticReport object generated by GoogleAnalyticsAPI class containing raw data basically filtered</param>
        /// <param name="KappaData">the datatable with Policy reponses metadata to get the ilibrary url for them</param>
        /// <returns>A datable with an extra 'REF' column containing the Ilibrary Url</returns>
        public static DataTable ToDataTableWithIdField(AnalyticReport report, DataTable KappaData)
        {
            var dt = new DataTable();

            var columns = new List<DataColumn>();
            var dimensionColumns = report.ColumnHeader.Dimensions;
            columns.AddRange(dimensionColumns.Select(c => new DataColumn(c)));

            var metricColumns = report.ColumnHeader.MetricHeader.MetricHeaderEntries;
            columns.AddRange(metricColumns.Select(m => new DataColumn(m.Name, Type.GetType("System.Int32"))));

            // Add an identifier field
            dt.Columns.Add(new DataColumn("REF"));
            dt.Columns.AddRange(columns.ToArray());

            int index = dt.Columns["ga:pagePath"].Ordinal - 1;

            var rows = report.Rows;
            foreach (var row in rows)
            {
                var dimensions = row.Dimensions;
                var xUrl = dimensions[index];

                if (string.IsNullOrEmpty(xUrl))
                    continue;

                var metrics = row.Metrics.First().Values;

                // get Ref from url (a ref for Policy reponses and ilibrary Url directly for other content )
                string refValue = GetRefFromUrl(xUrl);
                // for Policy response we need to get their ilibraryUrl with the ref from Kappa DataTable
                if (IsRefUrl(xUrl))
                {
                    DataRow result = KappaData.Select($"REF = '{refValue}'").FirstOrDefault();
                    if (result is null)
                        continue;
                    refValue = result["iLibrary URL"].ToString();
                    if (refValue.Length == 0)
                        continue;
                }

                var r = dt.NewRow();
                var data = new List<string>
                {
                    refValue
                };
                data.AddRange(dimensions);
                data.AddRange(metrics);
                for (var i = 0; i < data.Count; i++)
                {
                    r[i] = data[i];
                }
                dt.Rows.Add(r);
            }
            return dt;
        }

        /// <summary>
        /// Takes a datatable and retuns a new datable grouped by 'REF' column and sum "views" column alias
        /// </summary>
        /// <param name="dt">A datatable with REF column and "views" column alias</param>
        /// <returns>A datatable grouped by 'REF' values and sum "views" column alias</returns>
        public static DataTable GroupDataTableByRef(DataTable dt)
        {
            var dtGroupedBy = new DataTable();
            dtGroupedBy.Columns.Add("REF", typeof(string));
            dtGroupedBy.Columns.Add("pageviews", typeof(int));
            dtGroupedBy = dt.AsEnumerable()
                .GroupBy(row => row.Field<string>("REF"))
                .Select(g =>
                {
                    var row = dtGroupedBy.NewRow();
                    row.SetField("REF", g.Key);
                    row.SetField("pageviews", g.Sum(x => x.Field<int>("views")));
                    return row;
                })
                .CopyToDataTable();
            dtGroupedBy.PrimaryKey = new DataColumn[] { dtGroupedBy.Columns["REF"] };
            return dtGroupedBy;
        }

        /// <summary>
        /// Return a new datatable with same fields as the legacy FreePreview Raw Format
        /// REF and pageviews fields are copied to coresponding fields
        /// </summary>
        /// <param name="dt">datable with GA values</param>
        /// <returns>datable formated as the legacy FreePreview Raw Format</returns>
        public static DataTable ToFreePreviewRawFormat(DataTable dt)
        {
            var dtFp = new DataTable();
            dtFp.Columns.AddRange(new DataColumn[]
            {
                new DataColumn("label", typeof(string)),
                new DataColumn("nb_visits", typeof(Int32)),
                new DataColumn("nb_uniq_visitors", typeof(Int32)),
                new DataColumn("nb_hits", typeof(Int32)),
                new DataColumn("sum_time_spent", typeof(Int32)),
                new DataColumn("nb_hits_with_time_generation", typeof(Int32)),
                new DataColumn("min_time_generation", typeof(decimal)),
                new DataColumn("max_time_generation", typeof(decimal)),
                new DataColumn("entry_nb_uniq_visitors", typeof(Int32)),
                new DataColumn("entry_nb_visits", typeof(Int32)),
                new DataColumn("entry_nb_actions", typeof(Int32)),
                new DataColumn("entry_sum_visit_length", typeof(Int32)),
                new DataColumn("entry_bounce_count", typeof(Int32)),
                new DataColumn("exit_nb_uniq_visitors", typeof(Int32)),
                new DataColumn("exit_nb_visits", typeof(Int32)),
                new DataColumn("avg_time_on_page", typeof(Int32)),
                new DataColumn("bounce_rate", typeof(string)),
                new DataColumn("exit_rate", typeof(string)),
                new DataColumn("avg_time_generation", typeof(string)),
                new DataColumn("metadata_url", typeof(string)),
                new DataColumn("sum_time_generation", typeof(decimal))
            });
            foreach (DataRow dr in dt.Rows)
            {
                DataRow row = dtFp.NewRow();
                row["label"] = dr["REF"];
                row["nb_hits"] = dr["pageviews"];
                dtFp.Rows.Add(row);
            }
            return dtFp;
        }
    }
}

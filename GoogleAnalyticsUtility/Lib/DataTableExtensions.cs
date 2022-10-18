using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using static Oecd.GoogleAnalyticsUtility.GoogleAnalyticsAPI;

namespace Oecd.GoogleAnalyticsUtility.Lib
{
    public static class DataTableExtensions
    {
        /// <summary>
        /// Joins 2 DataTable based on a given function
        /// </summary>
        /// <param name="t1">First DataTable</param>
        /// <param name="t2">Second DataTable</param>
        /// <param name="joinOn">the funtion used to merged</param>
        /// <returns>The merged DataTable</returns>
        public static DataTable JoinDataTables(this DataTable t1, DataTable t2, params Func<DataRow, DataRow, bool>[] joinOn)
        {
            DataTable result = new();
            foreach (DataColumn col in t1.Columns)
            {
                if (result.Columns[col.ColumnName] == null)
                    result.Columns.Add(col.ColumnName, col.DataType);
            }
            foreach (DataColumn col in t2.Columns)
            {
                if (result.Columns[col.ColumnName] == null)
                    result.Columns.Add(col.ColumnName, col.DataType);
            }
            foreach (DataRow row1 in t1.Rows)
            {
                var joinRows = t2.AsEnumerable().Where(row2 =>
                {
                    foreach (var parameter in joinOn)
                    {
                        if (!parameter(row1, row2)) return false;
                    }
                    return true;
                });
                foreach (DataRow fromRow in joinRows)
                {
                    DataRow insertRow = result.NewRow();
                    foreach (DataColumn col1 in t1.Columns)
                    {
                        insertRow[col1.ColumnName] = row1[col1.ColumnName];
                    }
                    foreach (DataColumn col2 in t2.Columns)
                    {
                        insertRow[col2.ColumnName] = fromRow[col2.ColumnName];
                    }
                    result.Rows.Add(insertRow);
                }
            }
            return result;
        }

        /// <summary>
        /// Combine collection of dataTables
        /// ASSUMPTION: All tables must have the same columns !
        /// </summary>
        /// <param name="args">array of tables to combine</param>
        /// <returns>combined table</returns>
        public static DataTable CombineDataTables(params DataTable[] args)
        {
            return args.SelectMany(dt => dt.AsEnumerable()).CopyToDataTable();
        }

        /// <summary>
        /// SetOrdinal of DataTable columns based on the index of the columnNames array. Removes invalid column names first.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="columnNames"></param>
        /// <remarks> http://stackoverflow.com/questions/3757997/how-to-change-datatable-colums-order</remarks>
        public static void SetColumnsOrder(this DataTable dtbl, params string[] columnNames)
        {
            List<string> listColNames = columnNames.ToList();

            List<string> dtColumnNames = dtbl.Columns.Cast<DataColumn>()
                                 .Select(x => x.ColumnName)
                                 .ToList();
            //Remove missing column names.
            foreach (string dtcolName in dtColumnNames)
            {
                if (!listColNames.Contains(dtcolName))
                {
                    dtbl.Columns.Remove(dtcolName);
                }
            }

            //Remove invalid column names.
            foreach (string colName in columnNames)
            {
                if (!dtbl.Columns.Contains(colName))
                {
                    listColNames.Remove(colName);
                }
            }

            foreach (string colName in listColNames)
            {
                dtbl.Columns[colName].SetOrdinal(listColNames.IndexOf(colName));
            }
        }

        /// <summary>
        /// Transform an Analytic report to a datatable
        /// </summary>
        /// <param name="ar">the AnalyticReport</param>
        /// <returns>the datable</returns>
        public static DataTable AsDataTable(this AnalyticReport ar)
        {
            var dt = new DataTable();

            var columns = new List<DataColumn>();
            var dimensionColumns = ar.ColumnHeader.Dimensions;
            columns.AddRange(dimensionColumns.Select(c => new DataColumn(c)));

            var metricColumns = ar.ColumnHeader.MetricHeader.MetricHeaderEntries;
            columns.AddRange(metricColumns.Select(m => new DataColumn(m.Name, Type.GetType("System.Int32"))));

            dt.Columns.AddRange(columns.ToArray());

            var rows = ar.Rows;
            foreach (var row in rows)
            {
                var dimensions = row.Dimensions;
                var metrics = row.Metrics.First().Values;
                var r = dt.NewRow();
                var data = new List<string>();
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
    }
}

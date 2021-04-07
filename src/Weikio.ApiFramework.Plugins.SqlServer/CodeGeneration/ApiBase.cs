using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Weikio.ApiFramework.Plugins.SqlServer.Configuration;

namespace Weikio.ApiFramework.Plugins.SqlServer.CodeGeneration
{
    public abstract class ApiBase<T> where T : DtoBase, new()
    {
        protected abstract string TableName { get; }
        protected abstract Dictionary<string, string> ColumnMap { get; }
        protected abstract bool IsSqlCommand { get; }
        protected string CommandText { get; set; }
        protected List<Tuple<string, object>> CommandParameters { get; set; }
        
        public SqlServerOptions Configuration { get; set; }

        protected async IAsyncEnumerable<T> RunSelect(int? top)
        {
            var fields = new List<string>();

            using (var conn = new SqlConnection(Configuration.ConnectionString))
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    var queryAndParameters = CreateQuery(TableName, top, fields);

                    cmd.CommandText = queryAndParameters.Query;
                    foreach (var prm in queryAndParameters.Parameters)
                    {
                        if (prm.Item2 == null)
                        {
                            cmd.Parameters.AddWithValue(prm.Item1, DBNull.Value);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue(prm.Item1, prm.Item2);
                        }                        
                    }

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var item = new T();
                            var selectedColumns = ColumnMap;
                            if (fields?.Any() == true)
                            {
                                selectedColumns = ColumnMap.Where(x => fields.Contains(x.Key, StringComparer.OrdinalIgnoreCase)).ToDictionary(p => p.Key, p => p.Value);
                            }

                            foreach (var column in selectedColumns)
                            {
                                item[column.Value] = reader[column.Key] == DBNull.Value ? null : reader[column.Key];
                            }

                            yield return item;
                        }
                    }
                }
            }
        }

        protected abstract QueryData CreateQuery(string tableName, int? top, List<string> fields);
    }
}

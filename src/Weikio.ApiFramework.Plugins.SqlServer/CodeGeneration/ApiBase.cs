using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using Weikio.ApiFramework.Plugins.SqlServer.Configuration;
using Weikio.TypeGenerator.Types;

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

        protected async IAsyncEnumerable<T> RunSelect(string select, string filter, string orderby, int? top, int? skip, bool? count)
        {
            var fields = new List<string>();

            using (var conn = new SqlConnection(Configuration.ConnectionString))
            {
                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    var queryAndParameters = CreateQuery(TableName, select, filter, orderby, top, skip, count, fields);

                    cmd.CommandText = queryAndParameters.Query;
                    
                    foreach (var prm in queryAndParameters.Parameters)
                    {
                        if (prm.Value == null)
                        {
                            cmd.Parameters.AddWithValue(prm.Key, DBNull.Value);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue(prm.Key, prm.Value);
                        }                        
                    }

                    var selectedColumns = ColumnMap;
                    Type generatedType = null;
                    if (fields?.Any() == true)
                    {
                        selectedColumns = ColumnMap.Where(x => fields.Contains(x.Key, StringComparer.OrdinalIgnoreCase)).ToDictionary(p => p.Key, p => p.Value);

                        var wrapperOptions = new TypeToTypeWrapperOptions
                        {
                            IncludedProperties = new List<string>(selectedColumns.Select(x => x.Value)),
                            AssemblyGenerator = CodeGenerator.CodeToAssemblyGenerator
                        };

                        generatedType = new TypeToTypeWrapper().CreateType(typeof(T), wrapperOptions);
                    }
                    
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            if (generatedType != null)
                            {
                                dynamic item = Activator.CreateInstance(generatedType);

                                foreach (var column in selectedColumns)
                                {
                                    var dbColumnValue = reader[column.Key] == DBNull.Value ? null : reader[column.Key];

                                    generatedType.InvokeMember(column.Value,
                                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty,
                                        Type.DefaultBinder, item, new[] { dbColumnValue });
                                }

                                yield return item;
                            }
                            else
                            {
                                var item = new T();

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
        }

        protected abstract QueryData CreateQuery(string tableName, string select, string filter, string orderby, int? top, int? skip, bool? count, List<string> fields);
    }
}

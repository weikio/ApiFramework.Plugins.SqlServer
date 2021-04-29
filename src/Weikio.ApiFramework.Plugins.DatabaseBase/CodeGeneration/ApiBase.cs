using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using Weikio.TypeGenerator.Types;

namespace Weikio.ApiFramework.Plugins.DatabaseBase.CodeGeneration
{
    public abstract class ApiBase<T> where T : DtoBase, new()
    {
        protected abstract string TableName { get; }
        protected abstract Dictionary<string, string> ColumnMap { get; }
        protected abstract bool IsSqlCommand { get; }
        protected string CommandText { get; set; }
        protected List<Tuple<string, object>> CommandParameters { get; set; }
        private static ConcurrentDictionary<string, Type> _cachedTypes = new ConcurrentDictionary<string, Type>();

        public DatabaseOptionsBase Configuration { get; set; }

        protected async IAsyncEnumerable<object> RunSelect(string select, string filter, string orderby, int? top, int? skip, bool? count)
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

                        generatedType = GetFilteredType(selectedColumns);
                    }

                    if (queryAndParameters.IsCount)
                    {
                        var countResult = await cmd.ExecuteScalarAsync();

                        yield return countResult;
                    }
                    else
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                if (generatedType != null)
                                {
                                    var item = Activator.CreateInstance(generatedType);

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
        }

        private static Type GetFilteredType(Dictionary<string, string> selectedColumns)
        {
            var typeName = typeof(T).Name;
            var columnNames = string.Join("", selectedColumns.Select(x => x.Value));
            var typeId = Math.Abs(columnNames.GetHashCode());

            var key = typeName + typeId;

            return _cachedTypes.GetOrAdd(key, s =>
            {
                var wrapperOptions = new TypeToTypeWrapperOptions
                {
                    IncludedProperties = new List<string>(selectedColumns.Select(x => x.Value)),
                    AssemblyGenerator = CodeGenerator.CodeToAssemblyGenerator,
                    TypeName = key
                };

                var result = new TypeToTypeWrapper().CreateType(typeof(T), wrapperOptions);

                return result;
            });
        }

        protected abstract QueryData CreateQuery(string tableName, string select, string filter, string orderby, int? top, int? skip, bool? count,
            List<string> fields);
    }
}
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Weikio.ApiFramework.Plugins.SqlServer.Configuration;
using SqlCommand = System.Data.SqlClient.SqlCommand;

namespace Weikio.ApiFramework.Plugins.SqlServer.Schema
{
    public static class SchemaTableColumns
    {
        public const string COLUMN_TABLE_NAME = "TABLE_NAME";
        public const string COLUMN_TABLE_SCHEMA = "TABLE_SCHEMA";
        public const string COLUMN_COLUMN_NAME = "COLUMN_NAME";
        public const string COLUMN_IS_NULLABLE = "IS_NULLABLE";
        public const string COLUMN_DATA_TYPE = "DATA_TYPE";
    }

    public class SchemaReader : IDisposable
    {
        private readonly SqlServerOptions _options;

        private SqlConnection _connection;

        public SchemaReader(SqlServerOptions sqlServeroptions)
        {
            _options = sqlServeroptions;
        }

        public void Connect()
        {
            _connection = new SqlConnection(_options.ConnectionString);
            _connection.Open();
        }

        private void RequireConnection()
        {
            if (_connection == null)
            {
                throw new InvalidOperationException("SchemaReader is not connected to a database.");
            }
        }

        public List<Table> GetSchema()
        {
            RequireConnection();

            var schema = _connection.GetSchema("COLUMNS");

            var tableNameColumnIndex = schema.Columns.IndexOf(SchemaTableColumns.COLUMN_TABLE_NAME);

            if (tableNameColumnIndex < 0)
            {
                throw new InvalidOperationException("Schema is invalid, no table name column found.");
            }

            var tableSchemaColumnIndex = schema.Columns.IndexOf(SchemaTableColumns.COLUMN_TABLE_SCHEMA);

            if (tableSchemaColumnIndex < 0)
            {
                throw new InvalidOperationException("Schema is invalid, no table schema column found.");
            }

            var columnNameColumnIndex = schema.Columns.IndexOf(SchemaTableColumns.COLUMN_COLUMN_NAME);

            if (columnNameColumnIndex < 0)
            {
                throw new InvalidOperationException("Schema is invalid, no column name column found.");
            }

            var dataTypeColumnIndex = schema.Columns.IndexOf(SchemaTableColumns.COLUMN_DATA_TYPE);

            if (dataTypeColumnIndex < 0)
            {
                throw new InvalidOperationException("Schema is invalid, no data type column found.");
            }

            var isNullableColumnIndex = schema.Columns.IndexOf(SchemaTableColumns.COLUMN_IS_NULLABLE);

            if (isNullableColumnIndex < 0)
            {
                throw new InvalidOperationException("Schema is invalid, no nullability column found.");
            }

            var dataTypes = GetDataTypes();

            var tables = new Dictionary<string, Table>();

            for (var i = 0; i < schema.Rows.Count; i++)
            {
                AddColumn(tableNameColumnIndex, tableSchemaColumnIndex, columnNameColumnIndex, dataTypeColumnIndex, isNullableColumnIndex, tables, dataTypes,
                    schema.Rows[i]);
            }

            var result = tables.Values.ToList();

            return result;
        }

        private Dictionary<string, Type> GetDataTypes()
        {
            var result = new Dictionary<string, Type>();
            const string colSqlType = "TypeName";
            const string colNetType = "DataType";

            var dataTypesSchema = _connection.GetSchema("DATATYPES");

            var sqlTypeColumnIndex = dataTypesSchema.Columns.IndexOf(colSqlType);

            if (sqlTypeColumnIndex < 0)
            {
                throw new InvalidOperationException("Data type schema is invalid, no SQL type column found.");
            }

            var netTypeColumnIndex = dataTypesSchema.Columns.IndexOf(colNetType);

            if (netTypeColumnIndex < 0)
            {
                throw new InvalidOperationException("Data type schema is invalid, no .NET type column found.");
            }

            for (var i = 0; i < dataTypesSchema.Rows.Count; i++)
            {
                var row = dataTypesSchema.Rows[i];
                var sqlTypeName = row.ItemArray[sqlTypeColumnIndex] as string;
                var netTypeName = row.ItemArray[netTypeColumnIndex] as string;

                if (!string.IsNullOrEmpty(sqlTypeName) && !string.IsNullOrEmpty(netTypeName))
                {
                    var type = Type.GetType(netTypeName);
                    result.Add(sqlTypeName, type);
                }
            }

            return result;
        }

        private void AddColumn(
            int tableNameColumnIndex,
            int tableSchemaColumnIndex,
            int columnNameColumnIndex,
            int dataTypeColumnIndex,
            int isNullableColumnIndex,
            Dictionary<string, Table> tables,
            Dictionary<string, Type> dataTypes,
            DataRow row)
        {
            var schemaName = row[tableSchemaColumnIndex] as string;
            var tableName = row[tableNameColumnIndex] as string;

            var fullTableName = $"{schemaName}.{tableName}";

            if (!_options.Includes(tableName))
            {
                return;
            }

            if (!tables.ContainsKey(fullTableName))
            {
                tables.Add(fullTableName, new Table(tableName, schemaName, null));
            }

            var table = tables[fullTableName];

            var columnName = row[columnNameColumnIndex] as string;
            var isNullable = (row[isNullableColumnIndex] as string ?? "NO") == "YES";

            var dataTypeName = row[dataTypeColumnIndex] as string;

            if (!dataTypes.ContainsKey(dataTypeName))
            {
                //TODO: logging? Exception?
                return;
            }

            var dataType = dataTypes[dataTypeName];

            table.AddColumn(columnName, dataType, isNullable);
        }

        public IList<Table> GetSchemaFor(SqlCommands sqlCommands)
        {
            RequireConnection();

            var schema = new List<Table>();

            if (sqlCommands?.Any() != true)
            {
                return schema;
            }

            foreach (var sqlCommand in sqlCommands)
            {
                using (var sqlServerCommand = _connection.CreateCommand())
                {
                    sqlServerCommand.CommandType = CommandType.Text;
                    sqlServerCommand.CommandText = sqlCommand.Value.CommandText;
                    sqlServerCommand.CommandTimeout = (int) TimeSpan.FromMinutes(5).TotalSeconds;

                    if (sqlCommand.Value.Parameters != null)
                    {
                        foreach (var parameter in sqlCommand.Value.Parameters)
                        {
                            var parameterType = Type.GetType(parameter.Type);

                            object parameterValue = null;

                            if (parameterType.IsValueType)
                            {
                                parameterValue = Activator.CreateInstance(parameterType);
                            }
                            else
                            {
                                parameterValue = DBNull.Value;
                            }

                            sqlServerCommand.Parameters.AddWithValue(parameter.Name, parameterValue);
                        }
                    }

                    var columns = GetColumns(sqlServerCommand);
                    schema.Add(new Table($"{sqlCommand.Key}", "", columns, sqlCommand.Value));
                }
            }

            return schema;
        }

        public IList<Table> ReadSchemaFromDatabaseTables()
        {
            RequireConnection();

            var schema = new List<Table>();

            var schemaTables = _connection.GetSchema("Tables");

            foreach (DataRow schemaTable in schemaTables.Rows)
            {
                if (schemaTable["TABLE_TYPE"].ToString() != "TABLE")
                {
                    continue;
                }

                var tableQualifier = "";

                if (schemaTable.Table.Columns.Contains("TABLE_QUALIFIER"))
                {
                    tableQualifier = schemaTable["TABLE_QUALIFIER"].ToString();
                }
                else if (schemaTable.Table.Columns.Contains("TABLE_SCHEM"))
                {
                    tableQualifier = schemaTable["TABLE_SCHEM"].ToString();
                }

                var tableName = schemaTable["TABLE_NAME"].ToString();
                var tableNameWithQualifier = string.IsNullOrWhiteSpace(tableQualifier) ? tableName : $"{tableQualifier}.{tableName}";

                if (!_options.Includes(tableName))
                {
                    continue;
                }

                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = $"select * from {tableNameWithQualifier}";
                    command.CommandTimeout = (int) TimeSpan.FromMinutes(5).TotalSeconds;

                    var columns = GetColumns(command);
                    schema.Add(new Table(tableName, tableQualifier, columns));
                }
            }

            return schema;
        }

        public IList<Column> GetColumns(SqlCommand odbcCommand)
        {
            var columns = new List<Column>();

            using (var reader = odbcCommand.ExecuteReader())
            {
                using (var dtSchema = reader.GetSchemaTable())
                {
                    if (dtSchema != null)
                    {
                        foreach (DataRow schemaColumn in dtSchema.Rows)
                        {
                            var columnName = Convert.ToString(schemaColumn["ColumnName"]);
                            var dataType = (Type) schemaColumn["DataType"];
                            var isNullable = (bool) schemaColumn["AllowDBNull"];

                            columns.Add(new Column(columnName, dataType, isNullable));
                        }
                    }
                }
            }

            return columns;
        }

        #region IDisposable Support

        private bool disposedValue; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_connection != null)
                    {
                        _connection.Dispose();
                        _connection = null;
                    }
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }

        #endregion
    }
}

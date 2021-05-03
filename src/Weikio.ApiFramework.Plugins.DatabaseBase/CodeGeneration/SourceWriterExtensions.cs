using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Weikio.TypeGenerator.Types;

namespace Weikio.ApiFramework.Plugins.DatabaseBase.CodeGeneration
{
    public static class SourceWriterExtensions
    {
        public static void WriteNamespaceBlock(this StringBuilder writer, Table table,
            Action<StringBuilder> contentProvider)
        {
            writer.Namespace(typeof(DatabaseApiFactoryBase).Namespace + ".Generated" + table.Name);

            contentProvider.Invoke(writer);

            writer.FinishBlock(); // Finish the namespace
        }

        public static void WriteDataTypeClass(this StringBuilder writer, Table table)
        {
            writer.WriteLine($"public class {GetDataTypeName(table)} : Weikio.ApiFramework.Plugins.DatabaseBase.CodeGeneration.DtoBase");
            writer.StartBlock();

            foreach (var column in table.Columns)
            {
                var typeName = TypeToTypeWrapper.GetFriendlyName(column.Type, column.Type.Name);
                writer.WriteLine($"public {typeName} {GetPropertyName(column.Name)} {{ get;set; }}");
            }

            writer.WriteLine("");

            writer.WriteLine("public object this[string propertyName]");
            writer.WriteLine("{");
            writer.WriteLine("get{return this.GetType().GetProperty(propertyName).GetValue(this, null);}");
            writer.WriteLine("set{this.GetType().GetProperty(propertyName).SetValue(this, value, null);}");
            writer.FinishBlock(); // Finish the this-block

            writer.FinishBlock(); // Finish the class
        }
        
        public static void WriteNonQueryCommandApiClass(this StringBuilder writer, KeyValuePair<string, SqlCommand> command, DatabaseOptionsBase options)
        {
            writer.StartClass(GetApiClassName(command));

            writer.WriteLine("public DatabaseOptionsBase Configuration { get; set; }");

            writer.WriteCommandMethod(command.Key, command.Value, options);

            writer.FinishBlock(); // Finish the class
        }

        public static void WriteApiClass(this StringBuilder writer, Table table, DatabaseOptionsBase options, IConnectionCreator connectionCreator)
        {
            Cache.ConnectionCreator = connectionCreator;

            if (table.SqlCommand != null)
            {
                writer.WriteLine($"public class {GetApiClassName(table)} : CommandApiBase<{GetDataTypeName(table)}>");
                writer.WriteLine("{");

                writer.WriteLine($"public {GetApiClassName(table)}() {{");
                writer.WriteLine($"CommandText = \"{table.SqlCommand.CommandText}\";");
                writer.WriteLine("CommandParameters = new List<Tuple<string, object>>();");
                writer.WriteLine("}");

                writer.WriteCommandMethod(table, options);
            }
            else
            {
                writer.WriteLine($"public class {GetApiClassName(table)} : TableApiBase<{GetDataTypeName(table)}>");
                writer.WriteLine("{");
            }
            
            writer.WriteLine($"private readonly ILogger<{GetApiClassName(table)}> _logger;");
            writer.WriteLine($"public {GetApiClassName(table)} (ILogger<{GetApiClassName(table)}> logger)");
            writer.WriteLine("{");
            writer.WriteLine("_logger = logger;");
            writer.WriteLine("}");

            var columnMap = new Dictionary<string, string>();

            foreach (var column in table.Columns)
            {
                columnMap.Add(column.Name, GetPropertyName(column.Name));
            }

            writer.WriteLine("private Dictionary<string, string> _columnMap = new Dictionary<string, string>");
            writer.WriteLine("{");

            foreach (var columnPair in columnMap)
            {
                writer.Write($"    {{\"{columnPair.Key}\", \"{columnPair.Value}\"}},");
            }

            writer.WriteLine("};");

            writer.WriteLine("");

            writer.WriteLine($"protected override string TableName => \"{table.NameWithQualifier}\";");
            writer.WriteLine("protected override Dictionary<string, string> ColumnMap => _columnMap;");
            writer.WriteLine($"protected override bool IsSqlCommand => {(table.IsSqlCommand ? "true" : "false")};");

            if (table.SqlCommand == null)
            {
                writer.WriteLine($"[ProducesResponseType(200, Type = typeof(List<{GetDataTypeName(table)}>))]");

                writer.WriteLine(
                    "public async IAsyncEnumerable<object> Select(string select, string filter, string orderby, int? top, int? skip, bool? count)");
                writer.WriteLine("{");
                writer.WriteLine("await foreach (var item in RunSelect(select, filter, orderby, top, skip, count))");
                writer.WriteLine("{");
                writer.WriteLine("yield return item;");
                writer.WriteLine("}"); // Finish the await foreach

                writer.WriteLine("}"); // Finish the Select method
            }

            writer.WriteLine("}"); // Finish the class
        }

        private static void WriteCommandMethod(this StringBuilder writer, string commandName, SqlCommand sqlCommand, DatabaseOptionsBase databaseOptions)
        {
            var sqlMethod = sqlCommand.CommandText.Trim()
                .Split(new[] { ' ' }, 2)
                .First().ToLower();
            sqlMethod = sqlMethod.Substring(0, 1).ToUpper() + sqlMethod.Substring(1);

            var methodParameters = new List<string>();

            if (sqlCommand.Parameters != null)
            {
                foreach (var sqlCommandParameter in sqlCommand.Parameters)
                {
                    var methodParam = "";

                    if (sqlCommandParameter.Optional)
                    {
                        var paramType = Type.GetType(sqlCommandParameter.Type);

                        if (paramType.IsValueType)
                        {
                            methodParam += $"{sqlCommandParameter.Type}? {sqlCommandParameter.Name} = null";
                        }
                        else
                        {
                            methodParam += $"{sqlCommandParameter.Type} {sqlCommandParameter.Name} = null";
                        }
                    }
                    else
                    {
                        methodParam += $"{sqlCommandParameter.Type} {sqlCommandParameter.Name}";
                    }

                    methodParameters.Add(methodParam);
                }
            }

            var dataTypeName = sqlCommand.IsNonQuery() ? "int" : GetDataTypeName(commandName, sqlCommand);
            var returnType = sqlCommand.IsNonQuery() ? "int" : $"IAsyncEnumerable<{dataTypeName}>";

            var cmdMethods = $"public async Task<{returnType}> {sqlMethod}({string.Join(", ", methodParameters)})";

            if (sqlCommand.IsQuery())
            {
                cmdMethods = $"public async {returnType} {sqlMethod}({string.Join(", ", methodParameters)})";
            }

            writer.WriteLine(cmdMethods);
            writer.WriteLine("{");

            if (sqlCommand.IsQuery() == false)
            {
                writer.WriteLine($"{returnType} result;");
            }

            writer.WriteLine("");

            writer.UsingBlock($"var conn = new OdbcConnection(\"{databaseOptions.ConnectionString}\")", w =>
            {
                w.WriteLine("await conn.OpenAsync();");

                w.UsingBlock("var cmd = conn.CreateCommand()", cmdBlock =>
                {
                    cmdBlock.WriteLine($"cmd.CommandText = @\"{sqlCommand.GetEscapedCommandText()}\";");

                    if (sqlCommand.Parameters != null)
                    {
                        foreach (var sqlCommandParameter in sqlCommand.Parameters)
                        {
                            cmdBlock.WriteLine(@$"OdbcHelpers.AddParameter(cmd, ""{sqlCommandParameter.Name}"", {sqlCommandParameter.Name});");
                        }
                    }

                    if (sqlCommand.IsQuery())
                    {
                        cmdBlock.UsingBlock("var reader = await cmd.ExecuteReaderAsync()", readerBlock =>
                        {
                            readerBlock.WriteLine("while (await reader.ReadAsync())");
                            readerBlock.WriteLine("{");
                            readerBlock.WriteLine($"var item = new {dataTypeName}();");
                            readerBlock.WriteLine("{");
                            readerBlock.Write("foreach (var column in ColumnMap)");

                            readerBlock.Write(
                                "item[column.Value] = reader[column.Key] == DBNull.Value ? null : reader[column.Key];");
                            readerBlock.FinishBlock(); // Finish the column setting foreach loop

                            readerBlock.Write("yield return item;");
                            readerBlock.FinishBlock(); // Finish the while loop
                        });
                    }
                    else
                    {
                        cmdBlock.WriteLine("result = cmd.ExecuteNonQuery();");
                    }
                });
            });

            if (sqlCommand.IsQuery() == false)
            {
                writer.Write("return result;");
            }

            writer.FinishBlock(); // Finish the method

            // var tableName = table.Name;
            // var sqlCommand = table.SqlCommand;
            //
            // var sqlMethod = sqlCommand.CommandText.Trim()
            //     .Split(new[] { ' ' }, 2)
            //     .First().ToLower();
            // sqlMethod = sqlMethod.Substring(0, 1).ToUpper() + sqlMethod.Substring(1);
            //
            // var methodParameters = new List<string>();
            //
            // if (sqlCommand.Parameters != null)
            // {
            //     foreach (var sqlCommandParameter in sqlCommand.Parameters)
            //     {
            //         var methodParam = "";
            //
            //         if (sqlCommandParameter.Optional)
            //         {
            //             var paramType = Type.GetType(sqlCommandParameter.Type);
            //
            //             if (paramType.IsValueType)
            //             {
            //                 methodParam += $"{sqlCommandParameter.Type}? {sqlCommandParameter.Name} = null";
            //             }
            //             else
            //             {
            //                 methodParam += $"{sqlCommandParameter.Type} {sqlCommandParameter.Name} = null";
            //             }
            //         }
            //         else
            //         {
            //             methodParam += $"{sqlCommandParameter.Type} {sqlCommandParameter.Name}";
            //         }
            //
            //         methodParameters.Add(methodParam);
            //     }
            // }
            //
            // var dataTypeName = GetDataTypeName(table);
            //
            // writer.WriteLine($"[ProducesResponseType(200, Type = typeof(List<{GetDataTypeName(table)}>))]");
            // writer.WriteLine($"public async IAsyncEnumerable<object> {sqlMethod}({string.Join(", ", methodParameters)})");
            // writer.StartBlock();
            //
            // writer.WriteLine("");
            //
            // if (sqlCommand.Parameters?.Any() == true)
            // {
            //     foreach (var sqlCommandParameter in sqlCommand.Parameters)
            //     {
            //         writer.WriteLine($"CommandParameters.Add(new Tuple<string, object>(\"{sqlCommandParameter.Name}\", {sqlCommandParameter.Name}));");
            //     }
            // }
            //
            // writer.WriteLine("await foreach (var item in RunSelect(null, null, null, null, null, null))");
            // writer.WriteLine("{");
            // writer.WriteLine("yield return item;");
            // writer.WriteLine("}"); // Finish the Select method
            //
            // writer.FinishBlock(); // Finish the method
        }

        private static string GetApiClassName(Table table)
        {
            return $"{table.Name}Api";
        }

        private static string GetApiClassName(KeyValuePair<string, SqlCommand> command)
        {
            return $"{command.Key}Api";
        }
        
        private static string GetDataTypeName(Table table)
        {
            if (!string.IsNullOrEmpty(table.SqlCommand?.DataTypeName))
            {
                return table.SqlCommand.DataTypeName;
            }

            return table.Name + "Item";
        }
        
        private static string GetDataTypeName(string commandName, SqlCommand sqlCommand = null)
        {
            if (!string.IsNullOrEmpty(sqlCommand?.DataTypeName))
            {
                return sqlCommand.DataTypeName;
            }

            return commandName + "Item";
        }


        private static string GetPropertyName(string originalName)
        {
            var isValid = SyntaxFacts.IsValidIdentifier(originalName);

            if (isValid)
            {
                return originalName;
            }

            var result = originalName;

            if (result.Contains(" "))
            {
                result = result.Replace(" ", "").Trim();
            }

            if (SyntaxFacts.IsValidIdentifier(result))
            {
                return result;
            }

            return $"@{result}";
        }
    }
}

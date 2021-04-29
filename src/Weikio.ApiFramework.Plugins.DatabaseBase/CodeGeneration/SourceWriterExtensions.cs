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

        public static void WriteApiClass(this StringBuilder writer, Table table, DatabaseOptionsBase odbcOptions)
        {
            if (table.SqlCommand != null)
            {
                writer.WriteLine($"public class {GetApiClassName(table)} : CommandApiBase<{GetDataTypeName(table)}>");
                writer.WriteLine("{");

                writer.WriteLine($"public {GetApiClassName(table)}() {{");
                writer.WriteLine($"CommandText = \"{table.SqlCommand.CommandText}\";");
                writer.WriteLine("CommandParameters = new List<Tuple<string, object>>();");
                writer.WriteLine("}");

                writer.WriteSqlCommandMethod(table, odbcOptions);
            }
            else
            {
                writer.WriteLine($"public class {GetApiClassName(table)} : TableApiBase<{GetDataTypeName(table)}>");
                writer.WriteLine("{");
            }

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

        private static void WriteSqlCommandMethod(this StringBuilder writer, Table table, DatabaseOptionsBase databaseOptions)
        {
            var tableName = table.Name;
            var sqlCommand = table.SqlCommand;

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

            var dataTypeName = GetDataTypeName(table);

            writer.WriteLine($"[ProducesResponseType(200, Type = typeof(List<{GetDataTypeName(table)}>))]");
            writer.WriteLine($"public async IAsyncEnumerable<object> {sqlMethod}({string.Join(", ", methodParameters)})");
            writer.StartBlock();

            writer.WriteLine("");

            if (sqlCommand.Parameters?.Any() == true)
            {
                foreach (var sqlCommandParameter in sqlCommand.Parameters)
                {
                    writer.WriteLine($"CommandParameters.Add(new Tuple<string, object>(\"{sqlCommandParameter.Name}\", {sqlCommandParameter.Name}));");
                }
            }

            writer.WriteLine("await foreach (var item in RunSelect(null, null, null, null, null, null))");
            writer.WriteLine("{");
            writer.WriteLine("yield return item;");
            writer.WriteLine("}"); // Finish the Select method

            writer.FinishBlock(); // Finish the method
        }

        private static string GetApiClassName(Table table)
        {
            return $"{table.Name}Api";
        }

        private static string GetDataTypeName(Table table)
        {
            if (!string.IsNullOrEmpty(table.SqlCommand?.DataTypeName))
            {
                return table.SqlCommand.DataTypeName;
            }

            return table.Name + "Item";
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
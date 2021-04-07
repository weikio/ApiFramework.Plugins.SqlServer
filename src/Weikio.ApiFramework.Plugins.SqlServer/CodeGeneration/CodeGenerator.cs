using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Weikio.ApiFramework.Plugins.SqlServer.Configuration;
using Weikio.ApiFramework.Plugins.SqlServer.Schema;
using Weikio.TypeGenerator;

namespace Weikio.ApiFramework.Plugins.SqlServer.CodeGeneration
{
    public class CodeGenerator
    {
        public Assembly GenerateAssembly(IList<Table> schema, SqlServerOptions odbcOptions)
        {
            var generator = new CodeToAssemblyGenerator();
            generator.ReferenceAssembly(typeof(System.Console).Assembly);
            generator.ReferenceAssembly(typeof(System.Data.DataRow).Assembly);

            var assemblyCode = GenerateCode(schema, odbcOptions);

            try
            {
                generator.ReferenceAssembly(GetType().Assembly);
                var result = generator.GenerateAssembly(assemblyCode);

                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                throw;
            }
        }

        public string GenerateCode(IList<Table> schema, SqlServerOptions odbcOptions)
        {
            var source = new StringBuilder();
            source.UsingNamespace("System");
            source.UsingNamespace("System.Collections.Generic");
            source.UsingNamespace("Weikio.ApiFramework.Plugins.SqlServer.Configuration");
            source.UsingNamespace("Weikio.ApiFramework.Plugins.SqlServer.CodeGeneration");
            source.WriteLine("");

            foreach (var table in schema)
            {
                source.WriteNamespaceBlock(table, namespaceBlock =>
                {
                    namespaceBlock.WriteDataTypeClass(table);

                    namespaceBlock.WriteApiClass(table, odbcOptions);
                });
            }

            return source.ToString();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Weikio.TypeGenerator;

namespace Weikio.ApiFramework.Plugins.DatabaseBase.CodeGeneration
{
    public class CodeGenerator
    {
        public static CodeToAssemblyGenerator CodeToAssemblyGenerator { get; set; }

        public Assembly GenerateAssembly(IList<Table> schema, DatabaseOptionsBase databaseOptions)
        {
            CodeToAssemblyGenerator = new CodeToAssemblyGenerator();
            CodeToAssemblyGenerator.ReferenceAssembly(typeof(System.Console).Assembly);
            CodeToAssemblyGenerator.ReferenceAssembly(typeof(System.Data.DataRow).Assembly);
            CodeToAssemblyGenerator.ReferenceAssemblyContainingType<ProducesResponseTypeAttribute>();
            var assemblyCode = GenerateCode(schema, databaseOptions);

            try
            {
                CodeToAssemblyGenerator.ReferenceAssembly(GetType().Assembly);
                var result = CodeToAssemblyGenerator.GenerateAssembly(assemblyCode);

                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                throw;
            }
        }

        public string GenerateCode(IList<Table> schema, DatabaseOptionsBase databaseOptions)
        {
            var source = new StringBuilder();
            source.UsingNamespace("System");
            source.UsingNamespace("System.Collections.Generic");
            source.UsingNamespace("Weikio.ApiFramework.Plugins.DatabaseBase.CodeGeneration");
            source.UsingNamespace("Microsoft.AspNetCore.Http");
            source.UsingNamespace("Microsoft.AspNetCore.Mvc");
            source.WriteLine("");

            foreach (var table in schema)
            {
                source.WriteNamespaceBlock(table, namespaceBlock =>
                {
                    namespaceBlock.WriteDataTypeClass(table);

                    namespaceBlock.WriteApiClass(table, databaseOptions);
                });
            }

            return source.ToString();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Weikio.TypeGenerator;

namespace Weikio.ApiFramework.Plugins.DatabaseBase.CodeGeneration
{
    public class CodeGenerator
    {
        private readonly IConnectionCreator _connectionCreator;
        private readonly ILogger<CodeGenerator> _logger;

        public CodeGenerator(IConnectionCreator connectionCreator, ILogger<CodeGenerator> logger)
        {
            _connectionCreator = connectionCreator;
            _logger = logger;
        }

        public static CodeToAssemblyGenerator CodeToAssemblyGenerator { get; set; }

        public Assembly GenerateAssembly(IList<Table> tableSchema, SqlCommands nonQueryCommands, DatabaseOptionsBase databaseOptions)
        {
            CodeToAssemblyGenerator = new CodeToAssemblyGenerator();
            CodeToAssemblyGenerator.ReferenceAssembly(typeof(Console).Assembly);
            CodeToAssemblyGenerator.ReferenceAssembly(typeof(System.Data.DataRow).Assembly);
            CodeToAssemblyGenerator.ReferenceAssemblyContainingType<ProducesResponseTypeAttribute>();
            CodeToAssemblyGenerator.ReferenceAssembly(_connectionCreator.GetType().Assembly);
            
            var assemblyCode = GenerateCode(tableSchema, nonQueryCommands, databaseOptions);

            try
            {
                CodeToAssemblyGenerator.ReferenceAssembly(GetType().Assembly);
                var result = CodeToAssemblyGenerator.GenerateAssembly(assemblyCode);

                return result;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to generate assembly");

                throw;
            }
        }

        public string GenerateCode(IList<Table> tableSchema, SqlCommands nonQueryCommands, DatabaseOptionsBase databaseOptions)
        {
            var source = new StringBuilder();
            source.UsingNamespace("System");
            source.UsingNamespace("System.Collections.Generic");
            source.UsingNamespace("System.Reflection");
            source.UsingNamespace("System.Linq");
            source.UsingNamespace("System.Diagnostics");       
            source.UsingNamespace("System.Data");
            source.UsingNamespace("Weikio.ApiFramework.Plugins.DatabaseBase.CodeGeneration");
            source.UsingNamespace("Microsoft.AspNetCore.Http");
            source.UsingNamespace("Microsoft.AspNetCore.Mvc");
            source.UsingNamespace("Microsoft.Extensions.Logging");
            source.WriteLine("");

            foreach (var table in tableSchema)
            {
                source.WriteNamespaceBlock(table, namespaceBlock =>
                {
                    namespaceBlock.WriteDataTypeClass(table);

                    namespaceBlock.WriteApiClass(table, databaseOptions, _connectionCreator);
                });
            }
            
            foreach (var command in nonQueryCommands)
            {
                source.WriteNamespaceBlock(command, namespaceBlock =>
                {
                    namespaceBlock.WriteNonQueryCommandApiClass(command, odbcOptions);
                });
            }

            return source.ToString();
        }
    }
}

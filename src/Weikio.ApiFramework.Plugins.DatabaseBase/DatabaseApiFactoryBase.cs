using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Weikio.ApiFramework.Plugins.DatabaseBase.CodeGeneration;

namespace Weikio.ApiFramework.Plugins.DatabaseBase
{
    public class DatabaseApiFactoryBase
    {
        private readonly ILogger<DatabaseApiFactoryBase> _logger;
        private readonly ILoggerFactory _loggerFactory;

        protected DatabaseApiFactoryBase(ILogger<DatabaseApiFactoryBase> logger, ILoggerFactory loggerFactory)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
        }

        protected List<Type> Generate(DatabaseOptionsBase configuration, Func<DatabaseOptionsBase, ISchemaReader> schemaReaderFactory, Func<DatabaseOptionsBase, IConnectionCreator> connectionCreatorFactory)
        {
            try
            {
                var schema = new List<Table>();

                using (var schemaReader = schemaReaderFactory(configuration))
                {
                    schemaReader.Connect();

                    if (configuration.SqlCommands?.Any() == true)
                    {
                        var dbCommands = schemaReader.GetCommandSchema(configuration.SqlCommands);
                        schema.AddRange(dbCommands);
                    }

                    if (configuration.ShouldGenerateApisForTables())
                    {
                        var dbTables = schemaReader.GetTableSchema();
                        schema.AddRange(dbTables);
                    }
                }

                var generator = new CodeGenerator(connectionCreatorFactory.Invoke(configuration), _loggerFactory.CreateLogger<CodeGenerator>());
                var assembly = generator.GenerateAssembly(schema, configuration);

                var result = assembly.GetExportedTypes()
                    .Where(x => x.Name.EndsWith("Api"))
                    .ToList();

                return result;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to generate API");

                throw;
            }
        }
    }
}

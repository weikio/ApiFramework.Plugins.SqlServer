using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Weikio.ApiFramework.Plugins.DatabaseBase.CodeGeneration;

namespace Weikio.ApiFramework.Plugins.DatabaseBase
{
    public abstract class DatabaseApiFactoryBase
    {
        protected readonly ILogger<DatabaseApiFactoryBase> _logger;
        protected abstract ISchemaReader CreateSchemaReader(DatabaseOptionsBase configuration);
        
        protected DatabaseApiFactoryBase(ILogger<DatabaseApiFactoryBase> logger)
        {
            _logger = logger;
        }

        protected List<Type> Generate(DatabaseOptionsBase configuration)
        {
            try
            {
                var schema = new List<Table>();

                using (var schemaReader = CreateSchemaReader(configuration))
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

                var generator = new CodeGenerator();
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

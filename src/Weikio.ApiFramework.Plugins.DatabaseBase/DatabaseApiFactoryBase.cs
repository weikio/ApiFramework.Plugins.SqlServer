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

        protected List<Type> Generate(DatabaseOptionsBase configuration, Func<DatabaseOptionsBase, 
            ISchemaReader> schemaReaderFactory, Func<DatabaseOptionsBase, 
            IConnectionCreator> connectionCreatorFactory, Func<string, string> sqlColumnSelectFactory)
        {
            try
            {
                using var re = new SchemaReader(configuration, connectionCreatorFactory.Invoke(configuration), sqlColumnSelectFactory,
                    _loggerFactory.CreateLogger<SchemaReader>());

                re.Connect();
                var schema = re.GetSchema();
                
                var generator = new CodeGenerator(connectionCreatorFactory.Invoke(configuration), _loggerFactory.CreateLogger<CodeGenerator>());
                var assembly = generator.GenerateAssembly(schema.Tables, configuration);

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

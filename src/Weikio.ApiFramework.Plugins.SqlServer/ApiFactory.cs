using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Weikio.ApiFramework.Plugins.SqlServer.CodeGeneration;
using Weikio.ApiFramework.Plugins.SqlServer.Configuration;
using Weikio.ApiFramework.Plugins.SqlServer.Schema;

namespace Weikio.ApiFramework.Plugins.SqlServer
{
    public static class ApiFactory
    {
        public static Task<IEnumerable<Type>> Create(SqlServerOptions configuration)
        {
            var schema = new List<Table>();

            using (var schemaReader = new SchemaReader(configuration))
            {
                schemaReader.Connect();

                if (configuration.SqlCommands?.Any() == true)
                {
                    var dbCommands = schemaReader.GetSchemaFor(configuration.SqlCommands);
                    schema.AddRange(dbCommands);
                }

                if (configuration.ShouldGenerateApisForTables())
                {
                    var dbTables = schemaReader.GetSchema();
                    schema.AddRange(dbTables);
                }
            }

            var generator = new CodeGenerator();
            var assembly = generator.GenerateAssembly(schema, configuration);

            var result = assembly.GetExportedTypes()
                .Where(x => x.Name.EndsWith("Api"))
                .ToList();

            return Task.FromResult<IEnumerable<Type>>(result);
        }
    }
}

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Weikio.ApiFramework.Plugins.DatabaseBase;
using Weikio.ApiFramework.Plugins.SqlServer.Configuration;
using Weikio.ApiFramework.Plugins.SqlServer.Schema;

namespace Weikio.ApiFramework.Plugins.SqlServer
{
    public class ApiFactory : DatabaseApiFactoryBase
    {
        public ApiFactory(ILogger<DatabaseApiFactoryBase> logger) : base(logger)
        {
        }

        public List<Type> Create(SqlServerOptions configuration)
        {
            var result = Generate(configuration);

            return result;
        }
        
        protected override ISchemaReader CreateSchemaReader(DatabaseOptionsBase configuration)
        {
            return new SqlServerSchemaReader(configuration);
        }
    }
}

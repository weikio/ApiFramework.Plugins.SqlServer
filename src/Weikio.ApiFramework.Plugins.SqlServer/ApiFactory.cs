using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using SqlKata.Compilers;
using Weikio.ApiFramework.Plugins.DatabaseBase;
using Weikio.ApiFramework.Plugins.SqlServer.Configuration;
using Weikio.ApiFramework.Plugins.SqlServer.Schema;

namespace Weikio.ApiFramework.Plugins.SqlServer
{
    public class ApiFactory : DatabaseApiFactoryBase
    {
        public ApiFactory(ILogger<ApiFactory> logger, ILoggerFactory loggerFactory) : base(logger, loggerFactory)
        {
        }

        public List<Type> Create(SqlServerOptions configuration)
        {
            var result = Generate(configuration, 
                config => new SqlServerConnectionCreator(config),
                tableName => $"select * from {tableName}",
                new SqlServerCompiler()
                {
                    UseLegacyPagination = false
                });

            return result;
        }
    }
}

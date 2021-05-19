using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using SqlKata.Compilers;
using Weikio.ApiFramework.Plugins.SqlServer.Configuration;
using Weikio.ApiFramework.SDK.DatabasePlugin;

namespace Weikio.ApiFramework.Plugins.SqlServer
{
    public class ApiFactory : DatabaseApiFactoryBase
    {
        public ApiFactory(ILogger<ApiFactory> logger, ILoggerFactory loggerFactory) : base(logger, loggerFactory)
        {
        }

        public List<Type> Create(SqlServerOptions configuration)
        {
            var pluginSettings = new DatabasePluginSettings(config => new SqlServerConnectionCreator(config), 
                tableName => $"select top 0 * from {tableName}",
                new SqlServerCompiler() { UseLegacyPagination = false });

            var result = Generate(configuration, pluginSettings);

            return result;
        }
    }
}

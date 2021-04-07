using Microsoft.Extensions.DependencyInjection;
using Weikio.ApiFramework.Abstractions.DependencyInjection;
using Weikio.ApiFramework.Plugins.SqlServer.Configuration;
using Weikio.ApiFramework.SDK;

namespace Weikio.ApiFramework.Plugins.SqlServer
{
    public static class ServiceExtensions
    {
        public static IApiFrameworkBuilder AddSqlServer(this IApiFrameworkBuilder builder, string endpoint = null, SqlServerOptions configuration = null)
        {
            builder.Services.AddSqlServer(endpoint, configuration);

            return builder;
        }

        public static IServiceCollection AddSqlServer(this IServiceCollection services, string endpoint = null, SqlServerOptions configuration = null)
        {
            services.RegisterPlugin(endpoint, configuration);

            return services;
        }
    }
}

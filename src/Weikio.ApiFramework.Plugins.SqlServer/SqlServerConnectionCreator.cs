using System.Data.Common;
using System.Data.SqlClient;
using Weikio.ApiFramework.SDK.DatabasePlugin;

namespace Weikio.ApiFramework.Plugins.SqlServer
{
    public class SqlServerConnectionCreator : IConnectionCreator
    {
        private readonly DatabaseOptionsBase _configuration;

        public SqlServerConnectionCreator(DatabaseOptionsBase configuration)
        {
            _configuration = configuration;
        }

        public DbConnection CreateConnection(DatabaseOptionsBase options)
        {
            var result = new SqlConnection(options.ConnectionString);

            return result;
        }
    }
}

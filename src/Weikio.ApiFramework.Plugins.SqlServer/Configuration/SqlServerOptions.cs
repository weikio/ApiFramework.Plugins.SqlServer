using System.Data.Common;
using Microsoft.Data.SqlClient;
using SqlKata.Compilers;
using Weikio.ApiFramework.SDK.DatabasePlugin;

namespace Weikio.ApiFramework.Plugins.SqlServer.Configuration
{
    public class SqlServerOptions : DatabaseOptionsBase
    {
        public override DbConnection CreateConnection()
        {
            var result = new SqlConnection(ConnectionString);

            return result;
        }

        public override Compiler CreateCompiler()
        {
            return new SqlServerCompiler() { UseLegacyPagination = false };
        }
    }
}

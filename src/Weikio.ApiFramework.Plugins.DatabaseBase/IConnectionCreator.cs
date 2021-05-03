using System.Data;
using System.Data.Common;

namespace Weikio.ApiFramework.Plugins.DatabaseBase
{
    public interface IConnectionCreator
    {
        DbConnection CreateConnection(DatabaseOptionsBase options);
    }
}

using System.Collections.Generic;
using System.Linq;

namespace Weikio.ApiFramework.Plugins.SqlServer.CodeGeneration
{
    public abstract class CommandApiBase<T> : ApiBase<T> where T : DtoBase, new()
    {
        protected override QueryData CreateQuery(string tableName, int? top, List<string> fields)
        {
            var query = CommandText.Replace("\"", "\"\"");            
            return new QueryData { Query = query, Parameters = CommandParameters.ToList() };
        }
    }
}

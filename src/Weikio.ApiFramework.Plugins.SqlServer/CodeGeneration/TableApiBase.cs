using System.Collections.Generic;
using System.Linq;

namespace Weikio.ApiFramework.Plugins.SqlServer.CodeGeneration
{
    public abstract class TableApiBase<T> : ApiBase<T> where T : DtoBase, new()
    {
        public async IAsyncEnumerable<T> Select(int? top)
        {
            await foreach (var item in RunSelect(top))
            {
                yield return item;
            }
        }

        protected override QueryData CreateQuery(string tableName, int? top, List<string> fields)
        {
            var sqlQuery =
                $"SELECT {(top.GetValueOrDefault() > 0 ? "TOP " + top.ToString() : "")} {(fields?.Any() == true ? string.Join(",", fields.Select(f => f.ToUpper())) : " * ")} FROM {tableName} ";

            return new QueryData { Query = sqlQuery, Parameters = new List<System.Tuple<string, object>>() };
        }
    }
}

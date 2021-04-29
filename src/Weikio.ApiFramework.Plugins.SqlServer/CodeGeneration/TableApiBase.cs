using System;
using System.Collections.Generic;
using System.Linq;
using DynamicODataToSQL;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SqlKata.Compilers;

namespace Weikio.ApiFramework.Plugins.SqlServer.CodeGeneration
{
    public class MyProduct
    {
        public string Test { get; set; }
    }
    
    public abstract class TableApiBase<T> : ApiBase<T> where T : DtoBase, new()
    {
        private static ODataToSqlConverter _converter = new ODataToSqlConverter(new EdmModelBuilder(), new SqlServerCompiler() { UseLegacyPagination = false });

        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MyProduct[]))]
        public async IAsyncEnumerable<object> Select(string select, string filter, string orderby, int? top, int? skip, bool? count)
        {
            await foreach (var item in RunSelect(select, filter, orderby, top, skip, count))
            {
                yield return item;
            }
        }

        protected override QueryData CreateQuery(string tableName, string select, string filter, string orderby, int? top, int? skip, bool? count, List<string> fields)
        {
            var odataQueryParameters = new Dictionary<string, string>();

            if (!string.IsNullOrWhiteSpace(select))
            {
                odataQueryParameters.Add("select", select);
                fields.AddRange(select.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()));
            }
            
            if (!string.IsNullOrWhiteSpace(filter))
            {
                odataQueryParameters.Add("filter", filter);
            }
            
            if (!string.IsNullOrWhiteSpace(orderby))
            {
                odataQueryParameters.Add("orderby", orderby);
            }

            if (top != null)
            {
                odataQueryParameters.Add("top", top.GetValueOrDefault().ToString());
            }

            if (skip != null)
            {
                odataQueryParameters.Add("skip", skip.GetValueOrDefault().ToString());
            }

            var result = _converter.ConvertToSQL(
                tableName,
                odataQueryParameters, count.GetValueOrDefault());

            var sql = result.Item1;

            var sqlParams = result.Item2; 

            return new QueryData { Query = sql, Parameters = sqlParams, IsCount = count.GetValueOrDefault()};
        }
    }
}

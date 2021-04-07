using System;
using System.Collections.Generic;

namespace Weikio.ApiFramework.Plugins.SqlServer.CodeGeneration
{
    public class QueryData
    {
        public string Query { get; set; }
        public List<Tuple<string, object>> Parameters { get; set; }
    }
}

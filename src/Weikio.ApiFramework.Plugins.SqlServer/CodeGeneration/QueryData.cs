using System;
using System.Collections.Generic;

namespace Weikio.ApiFramework.Plugins.SqlServer.CodeGeneration
{
    public class QueryData
    {
        public string Query { get; set; }
        public IDictionary<string, object> Parameters { get; set; }
        public bool IsCount { get; set; }
    }
}

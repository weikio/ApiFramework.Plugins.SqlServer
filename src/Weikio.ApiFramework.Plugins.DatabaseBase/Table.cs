using System;
using System.Collections.Generic;

namespace Weikio.ApiFramework.Plugins.DatabaseBase
{
    public class Table
    {
        public string Name { get; }

        public string Qualifier { get; }

        public string NameWithQualifier { get; }

        public IList<Column> Columns { get; }

        public SqlCommand SqlCommand { get; set; }

        public bool IsSqlCommand => SqlCommand != null;

        public Table(string name, string qualifier, IList<Column> columns, SqlCommand sqlCommand = null)
        {
            Name = name;
            Qualifier = qualifier;
            NameWithQualifier = string.IsNullOrWhiteSpace(qualifier) ? name : $"{qualifier}.{name}";
            Columns = columns ?? new List<Column>();
            SqlCommand = sqlCommand;
        }

        public void AddColumn(string columnName, Type dataType, bool isNullable) 
        {
            Columns.Add(new Column(columnName, dataType, isNullable));
        }
    }
}

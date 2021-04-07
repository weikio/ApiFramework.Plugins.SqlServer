namespace Weikio.ApiFramework.Plugins.SqlServer.Configuration
{
    public class SqlCommandParameter
    {
        public string Name { get; set; }

        public string Type { get; set; }

        public bool Optional { get; set; }

        public object DefaultValue { get; set; } = null;
    }
}

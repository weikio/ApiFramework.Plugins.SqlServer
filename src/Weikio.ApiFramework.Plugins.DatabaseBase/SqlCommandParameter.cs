namespace Weikio.ApiFramework.Plugins.DatabaseBase
{
    public class SqlCommandParameter
    {
        public string Name { get; set; }

        public string Type { get; set; }

        public bool Optional { get; set; }

        public object DefaultValue { get; set; } = null;
    }
}

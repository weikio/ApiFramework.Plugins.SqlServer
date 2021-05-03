using SqlKata.Compilers;

namespace Weikio.ApiFramework.Plugins.DatabaseBase.CodeGeneration
{
    public static class Cache
    {
        public static IConnectionCreator ConnectionCreator { get; set; }
        public static Compiler DbCompiler { get; set; }
    }
}

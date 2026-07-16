using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace EffectExclusion.Tests;

internal static class TestAssemblyResolver
{
    [ModuleInitializer]
    internal static void Register()
    {
        var ymm4Dir = typeof(TestAssemblyResolver).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .First(static x => x.Key == "YMM4DirPath")
            .Value!;
        AssemblyLoadContext.Default.Resolving += (context, name) =>
        {
            var path = Path.Combine(ymm4Dir, name.Name + ".dll");
            return File.Exists(path) ? context.LoadFromAssemblyPath(path) : null;
        };
    }
}

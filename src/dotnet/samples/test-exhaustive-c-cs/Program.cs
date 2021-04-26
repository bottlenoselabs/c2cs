using System;
using System.IO;
using System.Reflection;
using C2CS.Tools;

namespace Exhaustive_C
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "bootstrap")
            {
                var rootDirectory = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "../../../.."));
                BuildLibrary(rootDirectory);
                GenerateLibraryBindings(rootDirectory);
            }
            else
            {
                UseLibraryBindings();
            }
        }
        
        private static void BuildLibrary(string rootDirectory)
        {
            var cMakeDirectoryPath = Path.Combine(rootDirectory, "src/c/samples/exhaustive-c");
            var currentApplicationBaseDirectoryPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location);

            "cmake -S . -B build-temp -G 'Unix Makefiles' -DCMAKE_BUILD_TYPE=Release".Bash(cMakeDirectoryPath);
            "make -C ./build-temp".Bash(cMakeDirectoryPath);
            $"cp -a {cMakeDirectoryPath}/lib/* {currentApplicationBaseDirectoryPath}".Bash();
            "rm -rf ./build-temp".Bash(cMakeDirectoryPath);
        }

        private static void GenerateLibraryBindings(string rootDirectory)
        {
            var arguments = @$"
-i
{rootDirectory}/src/c/samples/exhaustive-c/include/library.h
-s
{rootDirectory}/src/c/samples/exhaustive-c/include
-o
{rootDirectory}/src/dotnet/samples/exhaustive-c-cs/exhaustive-c.cs
-u
-l
exhaustive-c
-c
exhaustive_c
";
            var argumentsArray =
                arguments.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            C2CS.Program.Main(argumentsArray);
        }

        private static void UseLibraryBindings()
        {
            exhaustive_c.hello_world();
        }
    }
}
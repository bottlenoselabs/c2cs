# Sokol

This is an example of how to build the shared library, generate the C# bindings, and use from C#: https://github.com/floooh/sokol

1. Run the C# project [`sokol-c`](/src/dotnet/examples/sokol/sokol-01_clear/Program.cs) to build the shared library and generate the bindings. This project will have an example of how to use C2CS.
2. Run any of the C# projects named [`sokol-XX_Y`](/src/dotnet/examples/sokol).

If you just want the bindings, you can see [`sokol.cs`](/src/dotnet/examples/sokol/sokol-cs/sokol.cs). However this includes all the sokol headers.

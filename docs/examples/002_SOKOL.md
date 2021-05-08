This is an example of how to build the shared library, generate the C# bindings, and use from C#: https://github.com/floooh/sokol

1. Run the C# project [`sokol-c`](/src/dotnet/examples/sokol/sokol-c/Program.cs) to build the shared library and generate the bindings. This project will have an example of how to use C2CS.
2. Run any of the C# projects named [`sokol-XX_Y`](/src/dotnet/examples/sokol).

If you just want the bindings, you can see [`sokol.cs`](/src/dotnet/examples/sokol/sokol-cs/sokol.cs). However this includes all the sokol headers.


# `sokol`

Here you will find examples of how to build the shared library, generate the C# bindings, and use from C#: https://github.com/floooh/sokol

## Generating bindings + Building native library

Run the C# project [`sokol-c`](/src/dotnet/examples/sokol/sokol-c/Program.cs) to build the native library and generate the bindings. This project serves as an example of how to use `C2CS`.

## Examples

The following are examples of how to use the generated bindings for `sokol` in C#. They match the C examples found here: https://github.com/floooh/sokol-samples/tree/master/sapp

|#|Name|Description|
|-|-|-|
|1|[clear][1]|[Minimal example to get something working; clear the framebuffer.][1]|

[1]: /src/dotnet/examples/sokol/sokol-01_clear/Program.cs
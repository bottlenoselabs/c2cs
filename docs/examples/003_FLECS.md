# `flecs`

Here you will find examples of how to build the shared library, generate the C# bindings, and use from C#: https://github.com/SanderMertens/flecs

## Generating bindings + Building native library

Run the C# project [`flecs-c`](/src/dotnet/examples/flecs/flecs-c/Program.cs) to build the native library and generate the bindings. This project serves as an example of how to use `C2CS`.

## Examples

The following are examples of how to use the generated bindings for `flecs` in C#. They match the C examples found here: https://github.com/SanderMertens/flecs/tree/master/examples/c

|#|Name|Description|
|-|-|-|
|1|[helloworld][1]|[Minimal example to get something working.][1]|
|2|[simple_system][2]|[1 system, 1 component.][2]|
|3|[move_system][3]|[1 system, 2 components.][3]|

[1]: /src/dotnet/examples/flecs/flecs-01_helloworld/Program.cs
[2]: /src/dotnet/examples/flecs/flecs-02_simple_system/Program.cs
[3]: /src/dotnet/examples/flecs/flecs-03_move_system/Program.cs
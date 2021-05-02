# libclang

This is an example of how to generate C# bindings for: https://github.com/llvm/llvm-project/tree/main/clang/include/clang-c

Run the C# project [`clang-c`](/src/dotnet/prod/libclang-c/Program.cs) to build the shared library and generate the bindings. This project is not like other examples because the generated bindings are used as part of C2CS in the next build. This means that C2CS generates bindings for libclang which then can generate bindings for libclang.

If you just want the bindings, you can see [`libclang.cs`](/src/dotnet/prod/libclang-cs/libclang.cs).

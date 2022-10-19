# README

This folder contains 3 minimal C# example projects named "helloworld". Build and run them in the following order to
demonstrate the minimal usage of C2CS.

1. helloworld-bindgen-plugin: The C# library as a plugin for controlling the bindgen.
2. helloworld-compile-c-library-and-generate-bindings: The C# application that hosts plugins and generates the bindings for an example C library.
   Includes the C source code using CMake.
3. helloworld-app: The C# application that uses the built C library and generated C# bindings.

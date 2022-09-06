# README

This folder contains 3 minimal C# example projects named "helloworld". Build and run them in the following order to
demonstrate the minimal usage of C2CS.

1. helloworld-bindgen-plugin: The C# library to has a C# plugin for customizing the bindgen.
2. helloworld-my_c_library: The C# application that hosts building and generating the binidngs for an example C library.
   Includes the C source code using CMake.
3. helloworld-app: The C# application that uses the built C library and generated C# bindings.

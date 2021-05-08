# `sokol`

Here you will find examples of how to build the shared library, generate the C# bindings, and use from C#: https://github.com/floooh/sokol

## Generating bindings + Building native library

Run the C# project [`sokol-c`](/src/dotnet/examples/sokol/sokol-c/Program.cs) to build the native library and generate the bindings. This project serves as an example of how to use `C2CS`.

## Examples

The following are examples of how to use the generated bindings for `sokol` in C#. They match the C examples found here: https://github.com/floooh/sokol-samples/tree/master/sapp

|#|Name|Description|
|-|-|-|
|1|[clear][1]|[Minimal example to get something working; clears the framebuffer with a changing color.][1]|
|2|triangle|Draw a triangle in clip space using a vertex buffer and a index buffer.|
|3|quad|Draw a quad in clip space using a vertex buffer and a index buffer.|
|4|bufferoffsets|Draw a triangle and a quad in clip space using the same vertex buffer and and index buffer.|
|5|cube|Draw a cube using a vertex buffer, a index buffer, and a Model, View, Projection matrix (MVP).|
|6|noninterleaved|Draw a cube using a vertex buffer with non-interleaved vertices, a index buffer, and a Model, View, Projection matrix (MVP).|
|7|texcube|Draw a textured cube using a vertex buffer, a index buffer, and a Model, View, Projection matrix (MVP).|
|8|offscreen|Draw a non-textured cube off screen to a render target and use the result as as the texture when drawing a cube to the framebuffer.|
|9|instancing|Draw multiple particles using one immutable vertex, one immutable index buffer, and one vertex buffer with streamed instance data.|
|10|mrt|Draw a cube to multiple render targets and then blend the results.|
|11|arraytex|Draw a cube with multiple 2D textures using one continous block of texture data (texture array).|
|12|dyntex|Draw a cube with streamed 2D texture data. The data is updated to with the rules of Conway's Game of Life.|

[1]: /src/dotnet/examples/sokol/sokol-01_clear/Program.cs
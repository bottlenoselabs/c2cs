#pragma once

typedef void (*TypeDef_FunctionPointer_ReturnVoid_ArgsVoid) (void);

FFI_API_DECL void TypeDef_FunctionPointer_ReturnVoid_ArgsVoid__invoke(TypeDef_FunctionPointer_ReturnVoid_ArgsVoid functionPointer)
{
    functionPointer();
}
#define SOKOL_IMPL
#define SOKOL_DLL
#define SOKOL_NO_ENTRY

#if _WIN32
    #ifndef _WIN64
        #error "Compiling for Windows 32-bit ARM or x86 is not supported."
    #endif
    #define WIN32_LEAN_AND_MEAN
    #define NOCOMM
    #ifndef WINAPI
        #define WINAPI __stdcall
    #endif
    #define APIENTRY WINAPI
    #define SOKOL_LOG(s) OutputDebugStringA(s)
    #include <windows.h>
#elif __APPLE__
    #error "To compile for Apple platforms please use the .m file instead."
#elif __linux__
#elif __unix__
    #error "Unknown unix platform"
#elif defined(_POSIX_VERSION)
    #error "Unknown posix platform"
#else   
    #error "Unknown platform"
#endif

#include "sokol.h"
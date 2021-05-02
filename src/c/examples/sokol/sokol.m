#define SOKOL_IMPL
#define SOKOL_DLL
#define SOKOL_NO_ENTRY

#include <TargetConditionals.h>
#if TARGET_OS_MAC
    #include <Cocoa/Cocoa.h>
#else
    #error "Not supported Apple platform."    
#endif

#if SOKOL_METAL
    #import <Metal/Metal.h>
#elif SOKOL_GLCORE33
    #ifndef GL_SILENCE_DEPRECATION
        #define GL_SILENCE_DEPRECATION
    #endif
    #if TARGET_OS_MAC
        #include <OpenGL/gl3.h>   
    #endif
#endif

#include "sokol.h"
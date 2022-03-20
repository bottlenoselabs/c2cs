#if defined(_WIN32)
    #if defined(_M_AMD64) || defined(_M_X64)
        #define C2CS_RUNTIME_PLATFORM_NAME "win-x64"
    #elif defined(_M_IX86)
        #define C2CS_RUNTIME_PLATFORM_NAME "win-x86"
    #elif defined(_M_ARM)
        #define C2CS_RUNTIME_PLATFORM_NAME "win-arm"
    #elif defined(_M_ARM64)
        #define C2CS_RUNTIME_PLATFORM_NAME "win-arm64"
    #else
        #error "Failed to determine runtime platform name: Unknown computer architecture for Windows."
    #endif
#elif defined(__linux__)
    #if defined(__x86_64__) || defined(_M_AMD64) || defined(_M_X64)
        #define C2CS_RUNTIME_PLATFORM_NAME "linux-x64"
    #elif defined(i386) || defined(__i386__) || defined(__i386) || defined(_M_IX86)
        #define C2CS_RUNTIME_PLATFORM_NAME "linux-x86"
    #elif defined(__aarch64__) || defined(_M_ARM64)
        #define C2CS_RUNTIME_PLATFORM_NAME "linux-arm64"
    #else
        #error "Failed to determine runtime platform name: Linux not yet implemented."
    #endif
#elif defined(__APPLE__) && defined(__MACH__)
    #include <TargetConditionals.h>
    #if TARGET_IPHONE_SIMULATOR || TARGET_OS_IPHONE
        #if TARGET_CPU_X86
            #define C2CS_RUNTIME_PLATFORM_NAME "ios-x86"
        #elif TARGET_CPU_X86_64
            #define C2CS_RUNTIME_PLATFORM_NAME "ios-x64"
        #elif TARGET_CPU_ARM
            #define C2CS_RUNTIME_PLATFORM_NAME "ios-arm"
        #elif TARGET_CPU_ARM64
            #define C2CS_RUNTIME_PLATFORM_NAME "ios-arm64"
        #else
            #error "Failed to determine runtime platform name: Unknown computer architecture for iOS."
        #endif
    #elif TARGET_OS_MAC
        #if TARGET_CPU_X86
            #define C2CS_RUNTIME_PLATFORM_NAME "osx-x86"
        #elif TARGET_CPU_X86_64
            #define C2CS_RUNTIME_PLATFORM_NAME "osx-x64"
        #elif TARGET_CPU_ARM
            #define C2CS_RUNTIME_PLATFORM_NAME "osx-arm"
        #elif TARGET_CPU_ARM64
            #define C2CS_RUNTIME_PLATFORM_NAME "osx-arm64"
        #else
            #error "Failed to determine runtime platform name: Unknown computer architecture for macOS."
        #endif
    #else
        #error "Failed to determine runtime platform name: Unknown operating system."
    #endif
#else
    #define C2CS_RUNTIME_PLATFORM_NAME NULL
#endif

#if defined(WIN32) || defined(_WIN32) || defined(__WIN32__)
    #define C2CS_API_DECL __declspec(dllexport)
#else
    #define C2CS_API_DECL extern
#endif

C2CS_API_DECL const char* c2cs_get_runtime_platform_name()
{
    return (C2CS_RUNTIME_PLATFORM_NAME == NULL) ? "" : C2CS_RUNTIME_PLATFORM_NAME;
}
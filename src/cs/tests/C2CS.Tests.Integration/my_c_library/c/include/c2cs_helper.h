#if defined(_WIN32)
    #if defined(__x86_64__) || defined(_M_AMD64) || defined(_M_X64)
        #define C2CS_RUNTIME_PLATFORM_NAME "x86_64-pc-windows"
    #elif defined(i386) || defined(__i386__) || defined(__i386) || defined(_M_IX86)
        #define C2CS_RUNTIME_PLATFORM_NAME "i686-pc-windows"
    #elif defined(__aarch64__) || defined(_M_ARM64)
        #define C2CS_RUNTIME_PLATFORM_NAME "aarch64-pc-windows"
    #else
        #error "Failed to determine runtime platform name: Unknown computer architecture for Windows."
    #endif
#elif defined(__linux__)
    #if defined(__x86_64__) || defined(_M_AMD64) || defined(_M_X64)
        #define C2CS_RUNTIME_PLATFORM_NAME "x86_64-unknown-linux-gnu"
    #elif defined(i386) || defined(__i386__) || defined(__i386) || defined(_M_IX86)
        #define C2CS_RUNTIME_PLATFORM_NAME "i686-unknown-linux-gnu"
    #elif defined(__aarch64__) || defined(_M_ARM64)
        #define C2CS_RUNTIME_PLATFORM_NAME "aarch64-unknown-linux-gnu"
    #else
        #error "Failed to determine runtime platform name: Unknown computer architecture for Linux."
    #endif
#elif defined(__APPLE__) && defined(__MACH__)
    #if __has_include("TargetConditionals.h")
        #include <TargetConditionals.h>
        #if TARGET_OS_MAC
            #if TARGET_CPU_X86
                #define C2CS_RUNTIME_PLATFORM_NAME "i686-apple-darwin"
            #elif TARGET_CPU_X86_64
                #define C2CS_RUNTIME_PLATFORM_NAME "x86_64-apple-darwin"
            #elif TARGET_CPU_ARM64
                #define C2CS_RUNTIME_PLATFORM_NAME "aarch64-apple-darwin"
            #else
                #error "Failed to determine runtime platform name: Unknown computer architecture for macOS."
            #endif
        #elif TARGET_IPHONE_SIMULATOR || TARGET_OS_IPHONE
            #if TARGET_CPU_X86_64
                #define C2CS_RUNTIME_PLATFORM_NAME "x86_64-apple-ios"
            #elif TARGET_CPU_ARM64
                #define C2CS_RUNTIME_PLATFORM_NAME "aarch64-apple-ios"
            #else
                #error "Failed to determine runtime platform name: Unknown computer architecture for iOS."
            #endif
        #else
            #error "Failed to determine runtime platform name: Unknown operating system."
        #endif
    #else
         #if defined(__i386__)
            #define C2CS_RUNTIME_PLATFORM_NAME "i686-apple-darwin"
         #elif defined(__x86_64__)
            #define C2CS_RUNTIME_PLATFORM_NAME "x86_64-apple-darwin"
         #elif defined(__arm64__)
            #define C2CS_RUNTIME_PLATFORM_NAME "aarch64-apple-darwin"
         #else
            #error "Failed to determine runtime platform name: Unknown computer architecture for macOS."
         #endif
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
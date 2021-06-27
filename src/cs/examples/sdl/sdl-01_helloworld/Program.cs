using System;
using static SDL;

internal static unsafe class Program
{
    private static int Main()
    {
        var errorCode = SDL_Init(0x20); // 0x20 = SDL_INIT_VIDEO
        CheckError(errorCode);

        var window = SDL_CreateWindow(
            "hello_sdl2",
            0, 0,
            800, 600,
            0x4 // 0x4 = SDL_WINDOW_SHOWN 
        );
        
        if (window == null)
        {
            CheckError();
        }
        
        var screenSurface = SDL_GetWindowSurface(window);
        errorCode = SDL_FillRect(screenSurface, default, SDL_MapRGB(screenSurface->format, 0xFF, 0x00, 0x00));
        CheckError(errorCode);
        errorCode = SDL_UpdateWindowSurface(window);
        CheckError(errorCode);
       
        while (true)
        {
            // Get the next event
            SDL_Event e;
            if (SDL_PollEvent(&e) != 0)
            {
                if (e.type == 0x100) // 0x100 = SDL_QUIT
                {
                    // Break out of the loop on quit
                    break;
                }
            }
        }
        
        SDL_DestroyWindow(window);
        SDL_Quit();
        
        return 0;
    }

    private static void CheckError(int? errorCode = -1)
    {
        if (errorCode >= 0)
        {
            return;
        }

        string error = SDL_GetError();
        Console.Error.WriteLine($"could not initialize sdl2: {error}");
        Environment.Exit(1);
    }
}
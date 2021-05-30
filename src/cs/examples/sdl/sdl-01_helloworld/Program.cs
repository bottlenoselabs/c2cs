internal static class Program
{
    private static void Main()
    {
        // TODO: Need a way to get to SDL_INIT_VIDEO... Probably will need custom flags to say I want to transpile a
        //  specific cursor
        
        // SDL_Window* window;
        // SDL_Surface* screenSurfaceL;
        // if (SDL_Init(SDL_INIT_VIDEO) < 0) {
        //     fprintf(stderr, "could not initialize sdl2: %s\n", SDL_GetError());
        //     return 1;
        // }
        // window = SDL_CreateWindow(
        //     "hello_sdl2",
        //     SDL_WINDOWPOS_UNDEFINED, SDL_WINDOWPOS_UNDEFINED,
        //     SCREEN_WIDTH, SCREEN_HEIGHT,
        //     SDL_WINDOW_SHOWN
        // );
        // if (window == NULL) {
        //     fprintf(stderr, "could not create window: %s\n", SDL_GetError());
        //     return 1;
        // }
        // screenSurface = SDL_GetWindowSurface(window);
        // SDL_FillRect(screenSurface, NULL, SDL_MapRGB(screenSurface->format, 0xFF, 0xFF, 0xFF));
        // SDL_UpdateWindowSurface(window);
        // SDL_Delay(2000);
        // SDL_DestroyWindow(window);
        // SDL_Quit();
    }
}
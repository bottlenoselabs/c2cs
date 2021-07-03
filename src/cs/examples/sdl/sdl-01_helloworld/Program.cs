// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using static SDL;

internal static unsafe class Program
{
    private struct ProgramState
    {
        public SDL_Window* Window;
    }

    private static ProgramState _state;

    private static int Main()
    {
        var errorCode = SDL_Init(0x20); // 0x20 = SDL_INIT_VIDEO
        CheckError(errorCode);

        CreateWindow();

        while (true)
        {
            // Get the next event
            SDL_Event e;
            if (SDL_PollEvent(&e) != 0)
            {
                Frame();

                // 0x100 = SDL_QUIT
                if (e.type == 0x100)
                {
                    // Break out of the loop on quit
                    break;
                }
            }
        }

        SDL_DestroyWindow(_state.Window);
        SDL_Quit();

        return 0;
    }

    private static void Frame()
    {
        var screenSurface = SDL_GetWindowSurface(_state.Window);
        var errorCode = SDL_FillRect(screenSurface, default, SDL_MapRGB(screenSurface->format, 0xFF, 0x00, 0x00));
        CheckError(errorCode);
        errorCode = SDL_UpdateWindowSurface(_state.Window);
        CheckError(errorCode);
    }

    private static void CreateWindow()
    {
        _state.Window = SDL_CreateWindow(
            "SDL2: Hello, world!",
            100,
            100,
            800,
            600,
            0x4 | 0x20); // 0x4 = SDL_WINDOW_SHOWN

        if (_state.Window == null)
        {
            CheckError();
        }

        PrintWindowFlags(_state.Window);
    }

    private static void PrintWindowFlags(SDL_Window* window)
    {
        // See: SDL_WindowFlags @ SDL_video.h
        var windowFlags = SDL_GetWindowFlags(window);

        Console.WriteLine(@$"Window: ""Fullscreen"" = {(windowFlags & 0x1) != 0}");
        Console.WriteLine(@$"WindowL ""OpenGL"" = {(windowFlags & 0x2) != 0}");
        Console.WriteLine(@$"Window: ""Shown"" = {(windowFlags & 0x4) != 0}");
        Console.WriteLine(@$"Window: ""Hidden"" = {(windowFlags & 0x8) != 0}");
        Console.WriteLine(@$"Window: ""Borderless"" = {(windowFlags & 0x10) != 0}");
        Console.WriteLine(@$"Window: ""Resizeable"" = {(windowFlags & 0x20) != 0}");
        Console.WriteLine(@$"Window: ""Minimized"" = {(windowFlags & 0x40) != 0}");
        Console.WriteLine(@$"Window: ""Maximized"" = {(windowFlags & 0x40) != 0}");
        Console.WriteLine(@$"Window: ""Mouse grabbed"" = {(windowFlags & 0x100) != 0}");
        Console.WriteLine(@$"Window: ""Input focus"" = {(windowFlags & 0x200) != 0}");
        Console.WriteLine(@$"Window: ""Mouse focus"" = {(windowFlags & 0x400) != 0}");
        Console.WriteLine(@$"Window: ""Fullscreen desktop"" = {((windowFlags & 0x1) | (windowFlags & 0x1000)) != 0}");
        Console.WriteLine(@$"Window: ""Foreign"" = {(windowFlags & 0x800) != 0}");
        Console.WriteLine(@$"Window: ""Allow high dots per inch"" = {(windowFlags & 0x2000) != 0}");
        Console.WriteLine(@$"Window: ""Mouse capture"" = {(windowFlags & 0x4000) != 0}");
        Console.WriteLine(@$"Window: ""Always on top"" = {(windowFlags & 0x8000) != 0}");
        Console.WriteLine(@$"Window: ""Skip taskbar"" = {(windowFlags & 0x10000) != 0}");
        Console.WriteLine(@$"Window: ""Utility"" = {(windowFlags & 0x20000) != 0}");
        Console.WriteLine(@$"Window: ""Tooltip"" = {(windowFlags & 0x20000) != 0}");
        Console.WriteLine(@$"Window: ""Popup menu"" = {(windowFlags & 0x40000) != 0}");
        Console.WriteLine(@$"Window: ""Keyboard grabbed"" = {(windowFlags & 0x80000) != 0}");
        Console.WriteLine(@$"Window: ""Vulkan"" = {(windowFlags & 0x100000) != 0}");
        Console.WriteLine(@$"Window: ""Metal"" = {(windowFlags & 0x200000) != 0}");
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

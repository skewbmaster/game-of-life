using System;
using SDL2;
using static SDL2.SDL;

namespace GameOfLife
{
    internal struct Vector2
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Vector2(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    internal static class Program
    {
        private const int Width = 800;
        private const int Height = 800;

        private static int worldSize;
        private const int WorldDivisor = 4;

        private static int[,] currentGeneration;
        private static int[,] nextGeneration;

        private static bool editing = true;
        private static bool runGenerations = false;
        private static bool incrementGeneration = false;
        private static Vector2 selectedCell = new Vector2(0, 0);
        
        private static int generationCount = 0;

        private static readonly SDL_Color WhiteColor = new SDL_Color { a=255, r=255, g=255, b=255 };

        public static void Main()
        {
            #region SDL Initialisation
            SDL_Init(SDL_INIT_VIDEO);
            
            IntPtr window = SDL_CreateWindow("Game of Life",
                SDL_WINDOWPOS_CENTERED,
                SDL_WINDOWPOS_CENTERED,
                Width,
                Height,
                SDL_WindowFlags.SDL_WINDOW_SHOWN);

            IntPtr renderer = SDL_CreateRenderer(window, -1, 
                SDL_RendererFlags.SDL_RENDERER_ACCELERATED | SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);

            SDL_ttf.TTF_Init();
            IntPtr displayFont = SDL_ttf.TTF_OpenFont("VarelaRound.ttf", 28);
            #endregion

            worldSize = Width / WorldDivisor;
            
            currentGeneration = GenerateGrid(worldSize);
            nextGeneration = GenerateGrid(worldSize);

            
            IntPtr generationText = MakeTextTexture("Generation: 0", displayFont, renderer, WhiteColor, out int generationTextWidth, out int generationTextHeight);
            SDL_Rect generationTextRect = new SDL_Rect { h=generationTextHeight, w=generationTextWidth, x=Width - generationTextWidth, y=Height - generationTextHeight };

            bool running = true;
            while (running)
            {
                while (SDL_PollEvent(out SDL_Event ev) == 1)
                {
                    switch (ev.type)
                    {
                        case SDL_EventType.SDL_QUIT:
                            running = false;
                            break;
                        
                        case SDL_EventType.SDL_KEYDOWN:
                            ProcessInputs(ev.key.keysym);
                            break;
                    }
                }
                
                SDL_SetRenderDrawColor(renderer, 0, 0, 0, 255);
                SDL_RenderClear(renderer);

                RenderWorld(renderer);
                
                if (editing)
                {
                    SDL_Rect selectedCellRect = new SDL_Rect
                        {h = WorldDivisor, w = WorldDivisor, x = selectedCell.X * WorldDivisor, y = selectedCell.Y * WorldDivisor};
                    
                    if (currentGeneration[selectedCell.Y, selectedCell.X] == 1)
                    {
                        SDL_SetRenderDrawColor(renderer, 255, 100, 128, 255);
                    }
                    else
                    {
                        SDL_SetRenderDrawColor(renderer, 200, 0, 0, 255);
                    }
                    
                    SDL_RenderFillRect(renderer, ref selectedCellRect);
                }
                else if (incrementGeneration || runGenerations)
                {
                    UpdateGrid();
                    generationCount++;
                    SDL_DestroyTexture(generationText);
                    generationText = MakeTextTexture("Generation: " + generationCount, displayFont, renderer, WhiteColor, out generationTextRect.w, out generationTextRect.h);
                    generationTextRect.x = Width - generationTextRect.w;
                    generationTextRect.y = Height - generationTextRect.h;
                    incrementGeneration = false;
                }

                SDL_RenderCopy(renderer, generationText, IntPtr.Zero, ref generationTextRect);

                SDL_RenderPresent(renderer);
            }
            
            SDL_ttf.TTF_CloseFont(displayFont);
            SDL_DestroyRenderer(renderer);
            SDL_DestroyWindow(window);
            SDL_Quit();
        }

        private static void RenderWorld(IntPtr renderer)
        {
            SDL_SetRenderDrawColor(renderer, 0, 128, 0, 255);
            for (int y = 0; y < worldSize; y++)
            {
                for (int x = 0; x < worldSize; x++)
                {
                    if (currentGeneration[y, x] != 1) continue;
                    
                    SDL_Rect currentSquare = new SDL_Rect {h = WorldDivisor, w = WorldDivisor, x = x * WorldDivisor, y = y * WorldDivisor};
                    SDL_RenderFillRect(renderer, ref currentSquare);
                }
            }
        }

        private static void UpdateGrid()
        {
            for (int y = 0; y < worldSize; y++)
            {
                for (int x = 0; x < worldSize; x++)
                {
                    int cells = GetSurroundingCells(x, y);

                    switch (cells)
                    {
                        case 2:
                            nextGeneration[y, x] = currentGeneration[y, x];
                            break;
                        case 3:
                            nextGeneration[y, x] = 1;
                            break;
                        default:
                            nextGeneration[y, x] = 0;
                            break;
                    }
                }
            }

            for (int i = 0; i < worldSize; i++)
            {
                for (int j = 0; j < worldSize; j++)
                {
                    currentGeneration[j, i] = nextGeneration[j, i];
                }
            }
        }

        private static int GetSurroundingCells(int x, int y)
        {
            int count = 0;
            
            for (int i = Math.Max(x - 1, 0); i <= Math.Min(x + 1, worldSize - 1); i++)
            {
                for (int j = Math.Max(y - 1, 0); j <= Math.Min(y + 1, worldSize - 1); j++)
                {
                    if (currentGeneration[j, i] == 1 && !(j == y && i == x))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static void ProcessInputs(SDL_Keysym key)
        {
            if (key.sym == SDL_Keycode.SDLK_e)
            {
                editing = !editing;
                return;
            }

            if (!editing)
            {
                if (key.sym == SDL_Keycode.SDLK_SPACE)
                {
                    incrementGeneration = true;
                }
                else if (key.sym == SDL_Keycode.SDLK_r)
                {
                    runGenerations = !runGenerations;
                }
                else if (key.sym == SDL_Keycode.SDLK_BACKSPACE)
                {
                    currentGeneration = GenerateGrid(worldSize);
                    generationCount = 0;
                }
                return;
            }
            switch (key.sym)
            {
                case SDL_Keycode.SDLK_UP:
                {
                    selectedCell.Y--;
                    if (selectedCell.Y < 0)
                        selectedCell.Y = worldSize;
                    break;
                }
                case SDL_Keycode.SDLK_RIGHT:
                {
                    selectedCell.X++;
                    if (selectedCell.X > worldSize)
                        selectedCell.X = 0;
                    break;
                }
                case SDL_Keycode.SDLK_DOWN:
                {
                    selectedCell.Y++;
                    if (selectedCell.Y > worldSize)
                        selectedCell.Y = 0;
                    break;
                }
                case SDL_Keycode.SDLK_LEFT:
                {
                    selectedCell.X--;
                    if (selectedCell.X < 0)
                        selectedCell.X = worldSize;
                    break;
                }
                case SDL_Keycode.SDLK_SPACE:
                {
                    currentGeneration[selectedCell.Y, selectedCell.X] ^= 1;
                    break;
                }
            }

        }

        private static IntPtr MakeTextTexture(string text, IntPtr font, IntPtr renderer, SDL_Color color, out int width, out int height)
        {
            IntPtr textTexture = SDL_CreateTextureFromSurface(renderer,
                SDL_ttf.TTF_RenderText_Blended(font, text, color));

            SDL_QueryTexture(textTexture, out _, out _, out width, out height);

            return textTexture;
        }

        private static int[,] GenerateGrid(int size)
        {
            int[,] grid = new int[size, size];
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    grid[x, y] = 0;
                }
            }

            return grid;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Media;

namespace Maze
{
    class Program
    {
        private static readonly GameManager manager = new GameManager();
        private static readonly PlayerController controller = new PlayerController("O");  // Player character

        private static void Main()
        {
            SetConsoleSettings();

            DisplayLogo();
            StartGame();
        }

        private static void SetConsoleSettings()
        {
            Console.Title = "Maze";
            Console.CursorVisible = false;
        }

        private static void DisplayLogo()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.SetCursorPosition(0, Console.BufferHeight - 18);
            Console.WriteLine
            (@"
                      ███╗   ███╗ █████╗ ███████╗███████╗
                      ████╗ ████║██╔══██╗╚══███╔╝██╔════╝
                      ██╔████╔██║███████║  ███╔╝ █████╗  
                      ██║╚██╔╝██║██╔══██║ ███╔╝  ██╔══╝  
                      ██║ ╚═╝ ██║██║  ██║███████╗███████╗
                      ╚═╝     ╚═╝╚═╝  ╚═╝╚══════╝╚══════╝"
            );

            Console.ResetColor();

            Console.SetCursorPosition(Console.BufferWidth - 47, Console.BufferHeight - 10);
            Console.WriteLine("by Wirmaple73");

            Console.SetCursorPosition(Console.BufferWidth - 78, Console.BufferHeight - 2);
            Console.WriteLine("ASCII Art: patorjk.com");

            Console.SetCursorPosition(Console.BufferWidth - 8, Console.BufferHeight - 2);
            Console.WriteLine("v1.0.0");

            System.Threading.Thread.Sleep(2500);
            Console.Clear();
        }

        private static void StartGame()
        {
            manager.InitializeGame();

            while (true)
            {
                for (byte i = 0; i < manager.MaxRounds; i++)
                {
                    manager.GenerateRound();
                    controller.Spawn();

                    while (manager.IsGameRunning)
                    {
                        controller.GetInput();
                        manager.CheckCollisions();

                        CheckRespawn();
                    }

                    PromptRestart();
                }

                manager.DisplayScore();
                manager.PromptNewGame();
            }
        }

        private static void CheckRespawn()
        {
            if (manager.HasCollided)
            {
                controller.Respawn();
                manager.HasCollided = false;
            }
        }

        private static void PromptRestart()
        {
            Console.ResetColor();
            Console.WriteLine("\n\nPress any key to continue into the next round, or \"ESC\" to exit...");

            GameManager.PromptExit();

            Console.Clear();
            manager.IsGameRunning = true;
        }
    }

    class GameManager
    {
        private readonly Random rnd = new Random();

        private List<short> wallList  = new List<short>();
        private List<short> exitList  = new List<short>();
        private List<short> bonusList = new List<short>();

        private int score = 0, topScore = int.MinValue;
        private byte round = 0;
        private bool isGameRunning = true, hasCollided = false;

        private const byte WINNING_SCORE     = 10,
                           BONUS_SCORE       = 5,
                           COLLISION_PENALTY = 5,
                           MAX_ROUNDS        = 10;

        private ConsoleColor wallColor;

        public byte MaxRounds
        {
            get { return MAX_ROUNDS; }
        }

        public bool IsGameRunning
        {
            get { return isGameRunning; }
            set { isGameRunning = value; }
        }

        public bool HasCollided
        {
            get { return hasCollided; }
            set { hasCollided = value; }
        }

        public void InitializeGame()
        {
            Console.WriteLine("--- Controls ---\n");
            WriteColored("Arrow keys", "Move around", ConsoleColor.Cyan);

            Console.WriteLine("\n\n--- Legend ---\n");
            WriteColored("0", "Player", ConsoleColor.Green);
            WriteColored("■", string.Format("Exit point  (+{0} points)", WINNING_SCORE), ConsoleColor.Red);
            WriteColored("■", string.Format("Bonus point  (+{0} points)", BONUS_SCORE), ConsoleColor.Yellow);
            WriteColored("█", string.Format("Wall  (-{0} points)", COLLISION_PENALTY), ConsoleColor.DarkGray);

            Console.WriteLine(@"

The main goal of the game is to reach the exit point (red square),
while avoiding the walls as much as possible.

Your score also gets determined in {0} rounds.


Press any key to start the game...", MAX_ROUNDS);

            Console.ReadKey(true);
            Console.Clear();
        }

        private void WriteColored(string str1, string str2, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write(str1);

            Console.ResetColor();
            Console.WriteLine(" - " + str2);
        }

        public void GenerateRound()
        {
            ClearLists();  // Clear the old data (coordinates) from the lists for a new round
            GetRandomWallColor();  // Wall/Border color

            GenerateBorders();
            GenerateWalls();
            GenerateExitPoint();
            GenerateBonusPoints();

            SoundManager.Play(Properties.Resources.game_start);
        }

        private void ClearLists()
        {
            wallList.Clear();
            exitList.Clear();
            bonusList.Clear();
        }

        private void GenerateBorders()
        {
            for (byte i = 0; i < Console.BufferHeight; i++)  // Vertical borders
            {
                PaintBorder(0, i);
                PaintBorder(Console.BufferWidth - 1, i);
            }

            for (byte i = 0; i < Console.BufferWidth - 1; i++)  // Horizontal borders
            {
                PaintBorder(i, 0);
                PaintBorder(i, Console.BufferHeight - 1);
            }
        }

        private void GetRandomWallColor()
        {
            byte[] wallColors =
            {
                2,  // DarkGreen
                3,  // DarkCyan
                5,  // DarkMagenta
                6,  // DarkYellow
                7,  // Gray
                8,  // DarkGray
                // 9,  // Blue
                11  // Cyan
            };

            wallColor = (ConsoleColor)wallColors[rnd.Next(0, wallColors.Length)];
            Console.ForegroundColor = wallColor;
        }

        private void PaintBorder(int curLeft, int curTop)
        {
            wallList.Add((short)curLeft);
            wallList.Add((short)curTop);

            Console.SetCursorPosition(curLeft, curTop);
            Console.Write("█");
        }

        private void GenerateWalls()
        {
            short numWalls = (short)rnd.Next(Console.BufferWidth * 6, Console.BufferWidth * 8);

            for (short i = 0; i < numWalls; i++)
            {
                int curLeft = rnd.Next(1, Console.BufferWidth - 1),  // Choose a random wall position
                    curTop = rnd.Next(1, Console.BufferHeight - 1);

                // Add the current wall position to the wall list (used for detecting collisions)
                wallList.Add((short)curLeft);
                wallList.Add((short)curTop);

                Console.SetCursorPosition(curLeft, curTop);
                Console.Write("█");
            }
        }

        private void GenerateExitPoint()
        {
            int curLeft = rnd.Next(2, Console.BufferWidth - 2),
                curTop = rnd.Next(2, Console.BufferHeight - 2);

            exitList.Add((short)curLeft);
            exitList.Add((short)curTop);

            Console.ForegroundColor = ConsoleColor.Red;

            Console.SetCursorPosition(curLeft, curTop);
            Console.Write("■");
        }

        private void GenerateBonusPoints()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;

            // 0-3 bonus points per round
            byte numPoints = (byte)rnd.Next(0, 4);

            for (byte i = 0; i < numPoints; i++)
            {

                int curLeft = rnd.Next(Console.BufferWidth - 78, Console.BufferWidth - 2),
                     curTop = rnd.Next(Console.BufferHeight - 23, Console.BufferHeight - 2);

                bonusList.Add((short)curLeft);
                bonusList.Add((short)curTop);

                Console.SetCursorPosition(curLeft, curTop);
                Console.Write("■");

                // Remove the bonus points from the wall list
                for (short j = 0; j < wallList.Count - 1; j += 2)
                {
                    if (HasHitWall(j, curLeft, curTop))
                    {
                        wallList.RemoveAt(j);
                        wallList.RemoveAt(j + 1);
                    }
                }
            }
        }

        public void CheckCollisions()
        {
            // Check if the player has collided with a bonus point
            for (byte i = 0; i < bonusList.Count - 1; i += 2)
            {
                if ((bonusList[i] == Console.CursorLeft) && (bonusList[i + 1] == Console.CursorTop))
                {
                    // Remove the current bonus point coordinates to prevent score farming
                    bonusList.RemoveRange(i, 2);
                    score += BONUS_SCORE;

                    SoundManager.Play(Properties.Resources.col_bonus);
                }
            }

            // Check if player has reached the exit
            if ((exitList[0] == Console.CursorLeft) && (exitList[1] == Console.CursorTop))
            {
                SoundManager.Play(Properties.Resources.col_exit);
                EndRound();
            }

            if (isGameRunning)
            {
                // Check if player has collided with a wall/border
                for (short i = 0; i < wallList.Count - 1; i += 2)
                {
                    if (HasHitWall(i, Console.CursorLeft, Console.CursorTop))
                    {
                        // Respawn the wall, deduct the player's score and play the wall collision sound
                        Console.ForegroundColor = wallColor;
                        Console.Write("█");

                        score -= COLLISION_PENALTY;
                        hasCollided = true;

                        SoundManager.Play(Properties.Resources.col_wall);
                    }
                }
            }
        }

        private bool HasHitWall(int i, int curLeft, int curTop)
        {
            return ((wallList[i] == curLeft) && (wallList[i + 1] == curTop));
        }

        private void EndRound()
        {
            Console.ResetColor();
            Console.Clear();

            // Show the results for the current round
            Console.WriteLine("--- Game Results (Round {0}) ---\n", ++round);
            Console.WriteLine("Score: {0}", score += WINNING_SCORE);

            isGameRunning = false;
        }

        public void DisplayScore()
        {
            SoundManager.Play(Properties.Resources.game_end);

            if (score > topScore)
                topScore = score;

            Console.WriteLine("Your final score: {0}", score);
            Console.WriteLine("Top score: {0}", topScore);
        }

        public void PromptNewGame()
        {
            Console.ResetColor();
            Console.WriteLine("\n\nPress any key to start a new game, or \"ESC\" to exit...");

            PromptExit();

            Console.Clear();

            score = 0;
            round = 0;
        }

        public static void PromptExit()
        {
            if (Console.ReadKey(true).Key == ConsoleKey.Escape)
                Environment.Exit(0);
        }
    }

    class PlayerController
    {
        private readonly Random rnd = new Random();
        private string playerChar;

        private int initialCurLeft, initialCurTop, curLeft, curTop;

        public PlayerController(string playerChar)
        {
            this.playerChar = playerChar;
        }

        public void Spawn()
        {
            initialCurLeft = rnd.Next(Console.BufferWidth - 70, Console.BufferWidth - 11);
            initialCurTop = rnd.Next(Console.BufferHeight - 20, Console.BufferHeight - 6);

            SpawnPlayer();
        }

        public void Respawn()
        {
            Console.SetCursorPosition(curLeft, curTop);
            Console.Write("\b \b");

            SpawnPlayer();
        }

        private void SpawnPlayer()
        {
            curLeft = initialCurLeft;
            curTop = initialCurTop;

            Console.SetCursorPosition(curLeft, curTop);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(playerChar + "\b");
        }

        public void GetInput()
        {
            switch (Console.ReadKey(false).Key)
            {
                case ConsoleKey.UpArrow:
                    --curTop;
                    break;

                case ConsoleKey.LeftArrow:
                    --curLeft;
                    break;

                case ConsoleKey.DownArrow:
                    ++curTop;
                    break;

                case ConsoleKey.RightArrow:
                    ++curLeft;
                    break;

                default:
                    Console.Write("\b \b");
                    break;
            }

            Console.ForegroundColor = ConsoleColor.Green;

            Console.SetCursorPosition(curLeft, curTop);
            Console.Write(playerChar + "\b");
        }
    }

    static class SoundManager
    {
        private static readonly SoundPlayer player = new SoundPlayer();

        public static void Play(System.IO.Stream stream)
        {
            using (player)
            {
                using (stream)
                {
                    player.Stream = stream;
                    player.Play();
                }
            }
        }
    }
}

using System;

namespace piškvorky
{
    internal class Program
    {
        static void Main(string[] args)
        {
            RunGame();
        }

        static void RunGame()
        {
            do
            {
                PlaySingleGame();
                Console.Write("Play again? (y/n): ");
            }
            while (Console.ReadLine()?.Trim().ToLower() == "y");
        }

        static void PlaySingleGame()
        {
            char[,] board = new char[3, 3];
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                    board[r, c] = ' ';

            char current = 'X';
            while (true)
            {
                PrintBoard(board);
                Console.WriteLine($"Player {current}, enter move as 'col-row' (e.g. 3-2):");
                var input = Console.ReadLine();
                if (input == null) return;
                var parts = input.Split(new[] { '-', ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2 || !int.TryParse(parts[0], out int c) || !int.TryParse(parts[1], out int r))
                {
                    Console.WriteLine("Invalid input. Use format 'col-row' with numbers 1-3, e.g. 3-2.");
                    continue;
                }
                r--; c--;
                if (r < 0 || r > 2 || c < 0 || c > 2)
                {
                    Console.WriteLine("Coordinates out of range. Use numbers 1-3.");
                    continue;
                }
                if (board[r, c] != ' ')
                {
                    Console.WriteLine("Cell already taken. Choose another.");
                    continue;
                }

                board[r, c] = current;

                if (CheckWin(board, current))
                {
                    PrintBoard(board);
                    Console.WriteLine($"Player {current} wins!");
                    break;
                }

                if (IsFull(board))
                {
                    PrintBoard(board);
                    Console.WriteLine("Draw!");
                    break;
                }

                current = current == 'X' ? 'O' : 'X';
            }
        }

        static void PrintBoard(char[,] b)
        {
            Console.Clear();
            Console.WriteLine("  1 2 3");
            for (int r = 0; r < 3; r++)
            {
                Console.Write((r + 1) + " ");
                for (int c = 0; c < 3; c++)
                {
                    Console.Write(b[r, c]);
                    if (c < 2) Console.Write('|');
                }
                Console.WriteLine();
                if (r < 2) Console.WriteLine("  -+-+-");
            }
            Console.WriteLine();
        }

        static bool IsFull(char[,] b)
        {
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                    if (b[r, c] == ' ') return false;
            return true;
        }

        static bool CheckWin(char[,] b, char player)
        {
            // rows
            for (int r = 0; r < 3; r++)
                if (b[r, 0] == player && b[r, 1] == player && b[r, 2] == player)
                    return true;
            // cols
            for (int c = 0; c < 3; c++)
                if (b[0, c] == player && b[1, c] == player && b[2, c] == player)
                    return true;
            // diagonals
            if (b[0, 0] == player && b[1, 1] == player && b[2, 2] == player) return true;
            if (b[0, 2] == player && b[1, 1] == player && b[2, 0] == player) return true;
            return false;
        }
    }
}

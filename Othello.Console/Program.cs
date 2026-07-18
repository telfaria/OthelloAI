using Othello.AI;
using Othello.Core;

namespace Othello.Console;

internal static class Program
{
    private static void Main(string[] args)
    {
        bool showThinking = ResolveThinkingVisibility(args);
        IOthelloAI blackAI = new GreedyAI();
        IOthelloAI whiteAI = new GreedyAI();
        Board board = Board.CreateInitial();

        global::System.Console.WriteLine("CPU vs CPU match started.");
        global::System.Console.WriteLine($"Black={blackAI.Name}, White={whiteAI.Name}, ShowThinking={showThinking}");

        while (!board.IsGameOver())
        {
            RenderBoard(board);

            IOthelloAI currentAI = board.CurrentPlayer == Disc.Black ? blackAI : whiteAI;
            AIDecision decision = currentAI.DecideMove(board.Clone());

            if (decision.Move is null)
            {
                bool passed = board.TryPass();
                if (!passed)
                {
                    throw new InvalidOperationException("AI returned no move while legal moves exist.");
                }

                global::System.Console.WriteLine($"{GetPlayerName(board.CurrentPlayer == Disc.Black ? Disc.White : Disc.Black)} passed.");
                continue;
            }

            bool applied = board.TryApplyMove(decision.Move.Value);
            if (!applied)
            {
                throw new InvalidOperationException($"AI selected an illegal move: {ToCoordinate(decision.Move.Value)}");
            }

            if (showThinking)
            {
                global::System.Console.WriteLine($"{currentAI.Name} ({GetPlayerName(board.CurrentPlayer == Disc.Black ? Disc.White : Disc.Black)}): {decision.Thought}");
            }
            else
            {
                global::System.Console.WriteLine($"{currentAI.Name} played {ToCoordinate(decision.Move.Value)}");
            }
        }

        RenderBoard(board);
        PrintResult(board);
    }

    private static bool ResolveThinkingVisibility(string[] args)
    {
        if (args.Length > 0)
        {
            string first = args[0].Trim();
            if (string.Equals(first, "--thinking=on", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(first, "--thinking", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(first, "--thinking=off", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        global::System.Console.Write("Show AI thinking? (y/n, default: y): ");
        string? input = global::System.Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
        {
            return true;
        }

        return input.Trim().StartsWith("y", StringComparison.OrdinalIgnoreCase);
    }

    private static void RenderBoard(Board board)
    {
        global::System.Console.WriteLine();
        global::System.Console.WriteLine("  A B C D E F G H");

        for (int row = 0; row < Board.BoardSize; row++)
        {
            global::System.Console.Write(row + 1);
            global::System.Console.Write(' ');

            for (int col = 0; col < Board.BoardSize; col++)
            {
                Disc disc = board.GetDisc(new Position(row, col));
                char symbol = disc switch
                {
                    Disc.Black => 'B',
                    Disc.White => 'W',
                    _ => '.'
                };

                global::System.Console.Write(symbol);
                if (col < Board.BoardSize - 1)
                {
                    global::System.Console.Write(' ');
                }
            }

            global::System.Console.WriteLine();
        }

        global::System.Console.WriteLine();
    }

    private static void PrintResult(Board board)
    {
        int blackCount = board.CountDiscs(Disc.Black);
        int whiteCount = board.CountDiscs(Disc.White);

        global::System.Console.WriteLine("Game over.");
        global::System.Console.WriteLine($"Black: {blackCount}, White: {whiteCount}");

        if (blackCount > whiteCount)
        {
            global::System.Console.WriteLine("Winner: Black");
            return;
        }

        if (whiteCount > blackCount)
        {
            global::System.Console.WriteLine("Winner: White");
            return;
        }

        global::System.Console.WriteLine("Draw");
    }

    private static string GetPlayerName(Disc player)
    {
        return player == Disc.Black ? "Black" : "White";
    }

    private static string ToCoordinate(Move move)
    {
        char file = (char)('A' + move.Col);
        int rank = move.Row + 1;
        return $"{file}{rank}";
    }
}

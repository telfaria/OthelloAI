using System.Diagnostics;
using Othello.AI;
using Othello.Core;

namespace Othello.Console;

internal static class Program
{
    private const int DefaultGameCount = 1000;

    private static void Main(string[] args)
    {
        int gameCount = ResolveGameCount(args);
        bool showThinking = ResolveThinkingVisibility(args);

        IOthelloAI blackAI = new MctsAI();
        IOthelloAI whiteAI = new MctsAI();

        global::System.Console.WriteLine("CPU vs CPU simulation started.");
        global::System.Console.WriteLine($"Black={blackAI.Name}, White={whiteAI.Name}, Games={gameCount}, ShowThinking={showThinking}");

        SimulationSummary summary = RunSimulation(blackAI, whiteAI, gameCount, showThinking);
        PrintSummary(summary, blackAI, whiteAI, gameCount);
    }

    private static SimulationSummary RunSimulation(IOthelloAI blackAI, IOthelloAI whiteAI, int gameCount, bool showThinking)
    {
        var stopwatch = Stopwatch.StartNew();
        var summary = new SimulationSummary();
        int progressInterval = gameCount >= 10 ? Math.Max(1, gameCount / 10) : int.MaxValue;

        for (int gameIndex = 1; gameIndex <= gameCount; gameIndex++)
        {
            blackAI.Reset();
            whiteAI.Reset();

            Board board = Board.CreateInitial();
            int movesInGame = 0;
            int passesInGame = 0;

            while (!board.IsGameOver())
            {
                if (showThinking)
                {
                    global::System.Console.WriteLine($"[Game {gameIndex}] Turn={GetPlayerName(board.CurrentPlayer)}");
                    RenderBoard(board);
                }

                IOthelloAI currentAI = board.CurrentPlayer == Disc.Black ? blackAI : whiteAI;
                AIDecision decision = currentAI.DecideMove(board.Clone());

                if (decision.Move is null)
                {
                    bool passed = board.TryPass();
                    if (!passed)
                    {
                        throw new InvalidOperationException("AI returned no move while legal moves exist.");
                    }

                    passesInGame++;
                    if (showThinking)
                    {
                        global::System.Console.WriteLine($"{currentAI.Name} ({GetPlayerName(board.CurrentPlayer == Disc.Black ? Disc.White : Disc.Black)}): pass, {decision.Thought}");
                    }

                    continue;
                }

                bool applied = board.TryApplyMove(decision.Move.Value);
                if (!applied)
                {
                    throw new InvalidOperationException($"AI selected an illegal move: {ToCoordinate(decision.Move.Value)}");
                }

                movesInGame++;
                if (showThinking)
                {
                    global::System.Console.WriteLine($"{currentAI.Name} played {ToCoordinate(decision.Move.Value)} | {decision.Thought}");
                }
            }

            int blackCount = board.CountDiscs(Disc.Black);
            int whiteCount = board.CountDiscs(Disc.White);
            int margin = Math.Abs(blackCount - whiteCount);

            summary.TotalBlackDiscs += blackCount;
            summary.TotalWhiteDiscs += whiteCount;
            summary.TotalMoves += movesInGame;
            summary.TotalPasses += passesInGame;
            summary.TotalMargin += margin;

            if (margin > summary.MaxMargin)
            {
                summary.MaxMargin = margin;
            }

            if (blackCount > whiteCount)
            {
                summary.BlackWins++;
            }
            else if (whiteCount > blackCount)
            {
                summary.WhiteWins++;
            }
            else
            {
                summary.Draws++;
            }

            if (!showThinking && gameIndex % progressInterval == 0)
            {
                global::System.Console.WriteLine($"Progress: {gameIndex}/{gameCount}");
            }
        }

        stopwatch.Stop();
        summary.Elapsed = stopwatch.Elapsed;
        return summary;
    }

    private static int ResolveGameCount(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i].Trim();

            if (arg.StartsWith("--games=", StringComparison.OrdinalIgnoreCase))
            {
                string gamesText = arg[8..];
                if (int.TryParse(gamesText, out int parsed) && parsed > 0)
                {
                    return parsed;
                }

                throw new ArgumentException("--games must be a positive integer.");
            }

            if (string.Equals(arg, "--games", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                if (int.TryParse(args[i + 1], out int parsed) && parsed > 0)
                {
                    return parsed;
                }

                throw new ArgumentException("--games value must be a positive integer.");
            }
        }

        global::System.Console.Write($"Number of games? (default: {DefaultGameCount}): ");
        string? input = global::System.Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
        {
            return DefaultGameCount;
        }

        if (int.TryParse(input.Trim(), out int value) && value > 0)
        {
            return value;
        }

        global::System.Console.WriteLine($"Invalid number. Use default: {DefaultGameCount}");
        return DefaultGameCount;
    }

    private static bool ResolveThinkingVisibility(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i].Trim();

            if (string.Equals(arg, "--thinking=on", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "--thinking", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(arg, "--thinking=off", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        global::System.Console.Write("Show AI thinking per turn? (y/n, default: n): ");
        string? input = global::System.Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        return input.Trim().StartsWith("y", StringComparison.OrdinalIgnoreCase);
    }

    private static void PrintSummary(SimulationSummary summary, IOthelloAI blackAI, IOthelloAI whiteAI, int gameCount)
    {
        double blackWinRate = 100.0 * summary.BlackWins / gameCount;
        double whiteWinRate = 100.0 * summary.WhiteWins / gameCount;
        double drawRate = 100.0 * summary.Draws / gameCount;
        double avgBlackDiscs = (double)summary.TotalBlackDiscs / gameCount;
        double avgWhiteDiscs = (double)summary.TotalWhiteDiscs / gameCount;
        double avgMoves = (double)summary.TotalMoves / gameCount;
        double avgPasses = (double)summary.TotalPasses / gameCount;
        double avgMargin = (double)summary.TotalMargin / gameCount;
        double gamesPerSecond = gameCount / Math.Max(0.001, summary.Elapsed.TotalSeconds);

        global::System.Console.WriteLine();
        global::System.Console.WriteLine("=== Simulation Summary ===");
        global::System.Console.WriteLine($"Black AI: {blackAI.Name}");
        global::System.Console.WriteLine($"White AI: {whiteAI.Name}");
        global::System.Console.WriteLine($"Games: {gameCount}");
        global::System.Console.WriteLine($"Black Wins: {summary.BlackWins} ({blackWinRate:F2}%)");
        global::System.Console.WriteLine($"White Wins: {summary.WhiteWins} ({whiteWinRate:F2}%)");
        global::System.Console.WriteLine($"Draws: {summary.Draws} ({drawRate:F2}%)");
        global::System.Console.WriteLine($"Average Discs - Black: {avgBlackDiscs:F2}, White: {avgWhiteDiscs:F2}");
        global::System.Console.WriteLine($"Average Moves/Game: {avgMoves:F2}");
        global::System.Console.WriteLine($"Average Passes/Game: {avgPasses:F2}");
        global::System.Console.WriteLine($"Average Final Margin: {avgMargin:F2}");
        global::System.Console.WriteLine($"Max Final Margin: {summary.MaxMargin}");
        global::System.Console.WriteLine($"Elapsed: {summary.Elapsed.TotalSeconds:F2}s");
        global::System.Console.WriteLine($"Throughput: {gamesPerSecond:F2} games/s");
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

    private sealed class SimulationSummary
    {
        public int BlackWins { get; set; }
        public int WhiteWins { get; set; }
        public int Draws { get; set; }
        public long TotalBlackDiscs { get; set; }
        public long TotalWhiteDiscs { get; set; }
        public long TotalMoves { get; set; }
        public long TotalPasses { get; set; }
        public long TotalMargin { get; set; }
        public int MaxMargin { get; set; }
        public TimeSpan Elapsed { get; set; }
    }
}

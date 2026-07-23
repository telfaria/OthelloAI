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

        Func<IOthelloAI> blackAIFactory = ResolveAIFactory(args, "black");
        Func<IOthelloAI> whiteAIFactory = ResolveAIFactory(args, "white");

        IOthelloAI blackAIInfo = blackAIFactory();
        IOthelloAI whiteAIInfo = whiteAIFactory();

        string selfPlayDirectoryPath = Path.Combine("data", "selfplay");
        var kifuStore = new SelfPlayKifuStore(selfPlayDirectoryPath, DateOnly.FromDateTime(DateTime.UtcNow));
        var gameWriter = new GameWriter(kifuStore);
        var manager = new SelfPlayManager(
            gameCount,
            showThinking,
            maxDegreeOfParallelism: Environment.ProcessorCount,
            blackAIFactory,
            whiteAIFactory,
            gameWriter,
            static message => global::System.Console.WriteLine(message),
            RenderBoard);

        global::System.Console.WriteLine("CPU vs CPU simulation started.");
        global::System.Console.WriteLine($"Black={blackAIInfo.Name}, White={whiteAIInfo.Name}, Games={gameCount}, ShowThinking={showThinking}");
        global::System.Console.WriteLine($"Kifu JSONL={gameWriter.FilePath}");

        using var cts = new CancellationTokenSource();
        SelfPlaySimulationSummary summary = manager.Run(cts.Token);
        PrintSummary(summary, blackAIInfo, whiteAIInfo, gameCount);
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

    private static Func<IOthelloAI> ResolveAIFactory(string[] args, string side)
    {
        string optionName = $"--{side}-ai";
        string? aiName = TryGetOptionValue(args, optionName);

        if (string.IsNullOrWhiteSpace(aiName))
        {
            // 既定は現行挙動を維持
            return static () => new MctsAI(3000, maxDegreeOfParallelism: 30);
        }

        switch (aiName.Trim().ToLowerInvariant())
        {
            case "random":
                return static () => new RandomAI();
            case "greedy":
                return static () => new GreedyAI();
            case "minimax":
                return static () => new MinimaxAI(4);
            case "alphabeta":
                return static () => new AlphaBetaAI(6);
            case "mcts":
                return static () => new MctsAI(3000, maxDegreeOfParallelism: 30);
            case "neural":
            case "neuralonnx":
            {
                string? modelPath = TryGetOptionValue(args, "--onnx-model");
                if (string.IsNullOrWhiteSpace(modelPath))
                {
                    throw new ArgumentException("NeuralOnnxAI を使う場合は --onnx-model=<path> を指定してください。");
                }

                return () => new NeuralOnnxAI(modelPath);
            }
            default:
                throw new ArgumentException($"Unsupported AI for {optionName}: {aiName}");
        }
    }

    private static string? TryGetOptionValue(string[] args, string optionName)
    {
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i].Trim();

            if (arg.StartsWith(optionName + "=", StringComparison.OrdinalIgnoreCase))
            {
                return arg[(optionName.Length + 1)..];
            }

            if (string.Equals(arg, optionName, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                return args[i + 1];
            }
        }

        return null;
    }

    private static void PrintSummary(SelfPlaySimulationSummary summary, IOthelloAI blackAI, IOthelloAI whiteAI, int gameCount)
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

    }

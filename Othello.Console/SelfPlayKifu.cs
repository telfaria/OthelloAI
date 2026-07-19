using System.Globalization;
using System.Text.Json;
using Othello.Core;

namespace Othello.Console;

/// <summary>
/// Represents one move entry in a self-play game record.
/// </summary>
public sealed class SelfPlayMoveRecord
{
    /// <summary>
    /// Gets or sets the ply index starting from 1.
    /// </summary>
    public int Ply { get; set; }

    /// <summary>
    /// Gets or sets the player name for this ply.
    /// </summary>
    public required string Player { get; set; }

    /// <summary>
    /// Gets or sets the selected move. Null when this turn is a pass.
    /// </summary>
    public SelfPlayMovePosition? Move { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this turn is a pass.
    /// </summary>
    public bool IsPass { get; set; }

    /// <summary>
    /// Gets or sets the board snapshot before the move.
    /// </summary>
    public required string[] BoardBefore { get; set; }

    /// <summary>
    /// Gets or sets the board snapshot after the move.
    /// </summary>
    public required string[] BoardAfter { get; set; }

    /// <summary>
    /// Gets or sets optional AI thought text.
    /// </summary>
    public string? Thought { get; set; }
}

/// <summary>
/// Represents row/column move coordinates.
/// </summary>
public readonly record struct SelfPlayMovePosition(int Row, int Col);

/// <summary>
/// Represents final game result information.
/// </summary>
public sealed class SelfPlayGameResult
{
    /// <summary>
    /// Gets or sets winner label: Black, White or Draw.
    /// </summary>
    public required string Winner { get; set; }

    /// <summary>
    /// Gets or sets final black disc count.
    /// </summary>
    public int BlackCount { get; set; }

    /// <summary>
    /// Gets or sets final white disc count.
    /// </summary>
    public int WhiteCount { get; set; }
}

/// <summary>
/// Represents one self-play game record for JSONL persistence.
/// </summary>
public sealed class SelfPlayGameRecord
{
    /// <summary>
    /// Gets or sets unique game identifier.
    /// </summary>
    public required string GameId { get; set; }

    /// <summary>
    /// Gets or sets game start timestamp in UTC.
    /// </summary>
    public DateTime StartedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets game end timestamp in UTC.
    /// </summary>
    public DateTime FinishedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets black AI name.
    /// </summary>
    public required string BlackAI { get; set; }

    /// <summary>
    /// Gets or sets white AI name.
    /// </summary>
    public required string WhiteAI { get; set; }

    /// <summary>
    /// Gets or sets final game result.
    /// </summary>
    public required SelfPlayGameResult Result { get; set; }

    /// <summary>
    /// Gets or sets move records including snapshots and thought.
    /// </summary>
    public required List<SelfPlayMoveRecord> Moves { get; set; }
}

/// <summary>
/// Builds self-play game records during simulation.
/// </summary>
public sealed class SelfPlayKifuRecorder
{
    private readonly DateTime _startedAtUtc;
    private readonly List<SelfPlayMoveRecord> _moves;

    /// <summary>
    /// Initializes a new recorder for one game.
    /// </summary>
    public SelfPlayKifuRecorder(string blackAI, string whiteAI)
    {
        GameId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        BlackAI = blackAI;
        WhiteAI = whiteAI;
        _startedAtUtc = DateTime.UtcNow;
        _moves = new List<SelfPlayMoveRecord>();
    }

    /// <summary>
    /// Gets game id.
    /// </summary>
    public string GameId { get; }

    /// <summary>
    /// Gets black AI name.
    /// </summary>
    public string BlackAI { get; }

    /// <summary>
    /// Gets white AI name.
    /// </summary>
    public string WhiteAI { get; }

    /// <summary>
    /// Appends one move record.
    /// </summary>
    public void AppendMove(int ply, Disc player, Move? move, string[] boardBefore, string[] boardAfter, string? thought)
    {
        SelfPlayMovePosition? movePosition = move is null ? null : new SelfPlayMovePosition(move.Value.Row, move.Value.Col);

        _moves.Add(new SelfPlayMoveRecord
        {
            Ply = ply,
            Player = player == Disc.Black ? "Black" : "White",
            Move = movePosition,
            IsPass = move is null,
            BoardBefore = boardBefore,
            BoardAfter = boardAfter,
            Thought = thought
        });
    }

    /// <summary>
    /// Completes this game and returns immutable export record.
    /// </summary>
    public SelfPlayGameRecord Finalize(int blackCount, int whiteCount)
    {
        string winner = blackCount > whiteCount
            ? "Black"
            : whiteCount > blackCount
                ? "White"
                : "Draw";

        return new SelfPlayGameRecord
        {
            GameId = GameId,
            StartedAtUtc = _startedAtUtc,
            FinishedAtUtc = DateTime.UtcNow,
            BlackAI = BlackAI,
            WhiteAI = WhiteAI,
            Result = new SelfPlayGameResult
            {
                Winner = winner,
                BlackCount = blackCount,
                WhiteCount = whiteCount
            },
            Moves = new List<SelfPlayMoveRecord>(_moves)
        };
    }

    /// <summary>
    /// Captures current board into 8 strings for JSON serialization.
    /// </summary>
    public static string[] CaptureSnapshot(Board board)
    {
        var rows = new string[Board.BoardSize];

        for (int row = 0; row < Board.BoardSize; row++)
        {
            var chars = new char[Board.BoardSize];
            for (int col = 0; col < Board.BoardSize; col++)
            {
                Disc disc = board.GetDisc(new Position(row, col));
                chars[col] = disc switch
                {
                    Disc.Black => 'B',
                    Disc.White => 'W',
                    _ => '.'
                };
            }

            rows[row] = new string(chars);
        }

        return rows;
    }
}

/// <summary>
/// Persists self-play records to JSON Lines files.
/// </summary>
public sealed class SelfPlayKifuStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    /// <summary>
    /// Initializes a store bound to one run output file.
    /// </summary>
    public SelfPlayKifuStore(string directoryPath, DateOnly runDate)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new ArgumentException("Directory path is required.", nameof(directoryPath));
        }

        DirectoryPath = directoryPath;
        FilePath = ResolveNextFilePath(directoryPath, runDate);
    }

    /// <summary>
    /// Gets output directory path.
    /// </summary>
    public string DirectoryPath { get; }

    /// <summary>
    /// Gets selected run file path.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Appends one game record as a single JSONL line.
    /// </summary>
    public void Append(SelfPlayGameRecord record)
    {
        Directory.CreateDirectory(DirectoryPath);

        string json = JsonSerializer.Serialize(record, JsonOptions);
        File.AppendAllText(FilePath, json + Environment.NewLine);
    }

    /// <summary>
    /// Resolves the next file path in yyyyMMdd_NNN.jsonl format.
    /// </summary>
    public static string ResolveNextFilePath(string directoryPath, DateOnly runDate)
    {
        string datePrefix = runDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        int maxSequence = 0;

        if (Directory.Exists(directoryPath))
        {
            string pattern = $"{datePrefix}_*.jsonl";
            string[] files = Directory.GetFiles(directoryPath, pattern, SearchOption.TopDirectoryOnly);

            foreach (string file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                if (fileName.Length != datePrefix.Length + 4)
                {
                    continue;
                }

                string sequenceText = fileName[(datePrefix.Length + 1)..];
                if (!int.TryParse(sequenceText, NumberStyles.None, CultureInfo.InvariantCulture, out int sequence))
                {
                    continue;
                }

                if (sequence > maxSequence)
                {
                    maxSequence = sequence;
                }
            }
        }

        int nextSequence = maxSequence + 1;
        string fileNameWithSequence = $"{datePrefix}_{nextSequence:000}.jsonl";
        return Path.Combine(directoryPath, fileNameWithSequence);
    }
}

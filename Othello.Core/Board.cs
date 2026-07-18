namespace Othello.Core;

/// <summary>
/// Represents the Othello board state and rule operations.
/// </summary>
public sealed class Board
{
    /// <summary>
    /// The board size of Othello.
    /// </summary>
    public const int BoardSize = 8;

    private static readonly (int RowDelta, int ColDelta)[] Directions =
    [
        (-1, -1),
        (-1, 0),
        (-1, 1),
        (0, -1),
        (0, 1),
        (1, -1),
        (1, 0),
        (1, 1)
    ];

    private readonly Disc[,] _cells;

    /// <summary>
    /// Gets the current player.
    /// </summary>
    public Disc CurrentPlayer { get; private set; }

    private Board(Disc[,] cells, Disc currentPlayer)
    {
        _cells = cells;
        CurrentPlayer = currentPlayer;
    }

    /// <summary>
    /// Creates the initial board state.
    /// </summary>
    /// <returns>The initialized board.</returns>
    public static Board CreateInitial()
    {
        var cells = new Disc[BoardSize, BoardSize];
        var center = BoardSize / 2;

        cells[center - 1, center - 1] = Disc.White;
        cells[center - 1, center] = Disc.Black;
        cells[center, center - 1] = Disc.Black;
        cells[center, center] = Disc.White;

        return new Board(cells, Disc.Black);
    }

    /// <summary>
    /// Creates a board from an existing cell state.
    /// </summary>
    /// <param name="cells">Source cells with 8x8 size.</param>
    /// <param name="currentPlayer">Current player.</param>
    /// <returns>A board initialized with the given state.</returns>
    public static Board FromCells(Disc[,] cells, Disc currentPlayer)
    {
        ArgumentNullException.ThrowIfNull(cells);

        if (cells.GetLength(0) != BoardSize || cells.GetLength(1) != BoardSize)
        {
            throw new ArgumentException("Board cells must be 8x8.", nameof(cells));
        }

        if (currentPlayer is Disc.Empty)
        {
            throw new ArgumentException("Current player must be Black or White.", nameof(currentPlayer));
        }

        return new Board((Disc[,])cells.Clone(), currentPlayer);
    }

    /// <summary>
    /// Creates a deep copy of this board.
    /// </summary>
    /// <returns>A copied board instance.</returns>
    public Board Clone()
    {
        return new Board((Disc[,])_cells.Clone(), CurrentPlayer);
    }

    /// <summary>
    /// Gets the disc at the specified position.
    /// </summary>
    /// <param name="position">Target position.</param>
    /// <returns>The disc at the position.</returns>
    public Disc GetDisc(Position position)
    {
        EnsureInBounds(position);
        return _cells[position.Row, position.Col];
    }

    /// <summary>
    /// Returns legal moves for the current player.
    /// </summary>
    /// <returns>Legal moves.</returns>
    public IReadOnlyList<Move> GetLegalMoves()
    {
        var moves = new List<Move>();

        for (var row = 0; row < BoardSize; row++)
        {
            for (var col = 0; col < BoardSize; col++)
            {
                var position = new Position(row, col);
                if (IsLegalMoveForPlayer(position, CurrentPlayer))
                {
                    moves.Add(new Move(position));
                }
            }
        }

        return moves;
    }

    /// <summary>
    /// Determines whether the specified move is legal for the current player.
    /// </summary>
    /// <param name="move">Move.</param>
    /// <returns><c>true</c> if legal; otherwise <c>false</c>.</returns>
    public bool IsLegalMove(Move move)
    {
        return IsLegalMove(move.Position);
    }

    /// <summary>
    /// Determines whether the specified position is legal for the current player.
    /// </summary>
    /// <param name="position">Move position.</param>
    /// <returns><c>true</c> if legal; otherwise <c>false</c>.</returns>
    public bool IsLegalMove(Position position)
    {
        return IsLegalMoveForPlayer(position, CurrentPlayer);
    }

    /// <summary>
    /// Applies a move for the current player if legal.
    /// </summary>
    /// <param name="move">Move.</param>
    /// <returns><c>true</c> if move was applied; otherwise <c>false</c>.</returns>
    public bool TryApplyMove(Move move)
    {
        return TryApplyMove(move.Position);
    }

    /// <summary>
    /// Applies a move position for the current player if legal.
    /// </summary>
    /// <param name="position">Move position.</param>
    /// <returns><c>true</c> if move was applied; otherwise <c>false</c>.</returns>
    public bool TryApplyMove(Position position)
    {
        if (!IsInBounds(position) || _cells[position.Row, position.Col] != Disc.Empty)
        {
            return false;
        }

        var flips = CollectFlips(position, CurrentPlayer);
        if (flips.Count == 0)
        {
            return false;
        }

        _cells[position.Row, position.Col] = CurrentPlayer;
        foreach (var flip in flips)
        {
            _cells[flip.Row, flip.Col] = CurrentPlayer;
        }

        CurrentPlayer = GetOpponent(CurrentPlayer);
        return true;
    }

    /// <summary>
    /// Determines whether the current player must pass.
    /// </summary>
    /// <returns><c>true</c> if no legal move exists; otherwise <c>false</c>.</returns>
    public bool CanPass()
    {
        return !HasAnyLegalMove(CurrentPlayer);
    }

    /// <summary>
    /// Passes the turn when the current player has no legal moves.
    /// </summary>
    /// <returns><c>true</c> if turn was passed; otherwise <c>false</c>.</returns>
    public bool TryPass()
    {
        if (!CanPass())
        {
            return false;
        }

        CurrentPlayer = GetOpponent(CurrentPlayer);
        return true;
    }

    /// <summary>
    /// Determines whether the game is over.
    /// </summary>
    /// <returns><c>true</c> if both players have no legal moves; otherwise <c>false</c>.</returns>
    public bool IsGameOver()
    {
        return !HasAnyLegalMove(Disc.Black) && !HasAnyLegalMove(Disc.White);
    }

    /// <summary>
    /// Counts discs for the specified color.
    /// </summary>
    /// <param name="disc">Disc color to count.</param>
    /// <returns>The number of discs.</returns>
    public int CountDiscs(Disc disc)
    {
        if (disc is Disc.Empty)
        {
            throw new ArgumentException("Count target must be Black or White.", nameof(disc));
        }

        var count = 0;

        for (var row = 0; row < BoardSize; row++)
        {
            for (var col = 0; col < BoardSize; col++)
            {
                if (_cells[row, col] == disc)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private bool HasAnyLegalMove(Disc player)
    {
        for (var row = 0; row < BoardSize; row++)
        {
            for (var col = 0; col < BoardSize; col++)
            {
                if (IsLegalMoveForPlayer(new Position(row, col), player))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsLegalMoveForPlayer(Position position, Disc player)
    {
        if (!IsInBounds(position) || _cells[position.Row, position.Col] != Disc.Empty)
        {
            return false;
        }

        return CollectFlips(position, player).Count > 0;
    }

    private List<Position> CollectFlips(Position position, Disc player)
    {
        var result = new List<Position>();
        var opponent = GetOpponent(player);

        foreach (var direction in Directions)
        {
            var row = position.Row + direction.RowDelta;
            var col = position.Col + direction.ColDelta;
            var line = new List<Position>();

            while (IsInBounds(row, col) && _cells[row, col] == opponent)
            {
                line.Add(new Position(row, col));
                row += direction.RowDelta;
                col += direction.ColDelta;
            }

            if (line.Count > 0 && IsInBounds(row, col) && _cells[row, col] == player)
            {
                result.AddRange(line);
            }
        }

        return result;
    }

    private static Disc GetOpponent(Disc player)
    {
        return player switch
        {
            Disc.Black => Disc.White,
            Disc.White => Disc.Black,
            _ => throw new ArgumentException("Player must be Black or White.", nameof(player))
        };
    }

    private static bool IsInBounds(Position position)
    {
        return IsInBounds(position.Row, position.Col);
    }

    private static bool IsInBounds(int row, int col)
    {
        return row >= 0 && row < BoardSize && col >= 0 && col < BoardSize;
    }

    private static void EnsureInBounds(Position position)
    {
        if (!IsInBounds(position))
        {
            throw new ArgumentOutOfRangeException(nameof(position), "Position is out of board range.");
        }
    }
}

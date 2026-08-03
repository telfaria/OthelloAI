using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Othello.Core;
using Othello.WinUI.Models;

namespace Othello.WinUI.ViewModel;

/// <summary>
/// Othello の盤面表示情報を管理する ViewModel です。
/// </summary>
public sealed partial class BoardViewModel : ObservableObject
{
    private Board _board;

    /// <summary>
    /// <see cref="BoardViewModel"/> クラスの新しいインスタンスを初期化します。
    /// </summary>
    public BoardViewModel()
    {
        _board = Board.CreateInitial();
        Cells = new ObservableCollection<BoardCellDisplayModel>();
        Refresh();
    }

    /// <summary>
    /// 盤面セルの表示一覧を取得します。
    /// </summary>
    public ObservableCollection<BoardCellDisplayModel> Cells { get; }

    private string _currentTurn = string.Empty;
    private int _blackDiscCount;
    private int _whiteDiscCount;

    /// <summary>
    /// 現在手番表示を取得します。
    /// </summary>
    public string CurrentTurn
    {
        get => _currentTurn;
        private set => SetProperty(ref _currentTurn, value);
    }

    /// <summary>
    /// 黒石数を取得します。
    /// </summary>
    public int BlackDiscCount
    {
        get => _blackDiscCount;
        private set => SetProperty(ref _blackDiscCount, value);
    }

    /// <summary>
    /// 白石数を取得します。
    /// </summary>
    public int WhiteDiscCount
    {
        get => _whiteDiscCount;
        private set => SetProperty(ref _whiteDiscCount, value);
    }

    /// <summary>
    /// 新しい盤面を表示対象として設定します。
    /// </summary>
    /// <param name="board">表示対象の盤面です。</param>
    public void SetBoard(Board board)
    {
        ArgumentNullException.ThrowIfNull(board);
        _board = board.Clone();
        Refresh();
    }

    /// <summary>
    /// 現在の盤面表示を更新します。
    /// 将来のリアルタイム更新では本メソッドを定期呼び出しできます。
    /// </summary>
    public void Refresh()
    {
        Cells.Clear();

        for (int row = 0; row < Board.BoardSize; row++)
        {
            for (int col = 0; col < Board.BoardSize; col++)
            {
                Disc disc = _board.GetDisc(new Position(row, col));
                Cells.Add(new BoardCellDisplayModel
                {
                    Row = row,
                    Column = col,
                    DiscSymbol = disc switch
                    {
                        Disc.Black => "●",
                        Disc.White => "○",
                        _ => string.Empty
                    },
                    DiscName = disc switch
                    {
                        Disc.Black => "Black",
                        Disc.White => "White",
                        _ => "Empty"
                    }
                });
            }
        }

        BlackDiscCount = _board.CountDiscs(Disc.Black);
        WhiteDiscCount = _board.CountDiscs(Disc.White);
        CurrentTurn = _board.CurrentPlayer == Disc.Black ? "Black" : "White";
    }
}

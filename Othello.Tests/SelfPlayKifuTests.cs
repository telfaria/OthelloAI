using Othello.Core;
using Othello.Console;

namespace Othello.Console.Tests;

public class SelfPlayKifuTests
{
    [Fact]
    public void CaptureSnapshot_InitialBoard_ReturnsEightRows()
    {
        Board board = Board.CreateInitial();

        string[] snapshot = SelfPlayKifuRecorder.CaptureSnapshot(board);

        Assert.Equal(Board.BoardSize, snapshot.Length);
        Assert.All(snapshot, row => Assert.Equal(Board.BoardSize, row.Length));
        Assert.Equal("...WB...", snapshot[3]);
        Assert.Equal("...BW...", snapshot[4]);
    }

    [Fact]
    public void Recorder_Finalize_IncludesMoveSnapshotAndThought()
    {
        Board board = Board.CreateInitial();
        var recorder = new SelfPlayKifuRecorder("MctsAI", "MctsAI");

        string[] before = SelfPlayKifuRecorder.CaptureSnapshot(board);
        bool applied = board.TryApplyMove(new Move(2, 3));
        Assert.True(applied);

        string[] after = SelfPlayKifuRecorder.CaptureSnapshot(board);
        recorder.AppendMove(
            ply: 1,
            player: Disc.Black,
            move: new Move(2, 3),
            boardBefore: before,
            boardAfter: after,
            thought: "Iterations=500");

        SelfPlayGameRecord record = recorder.Finalize(blackCount: 4, whiteCount: 1);

        Assert.Equal("Black", record.Result.Winner);
        Assert.Single(record.Moves);
        SelfPlayMoveRecord move = record.Moves[0];
        Assert.Equal(1, move.Ply);
        Assert.Equal("Black", move.Player);
        Assert.False(move.IsPass);
        Assert.NotNull(move.Move);
        Assert.Equal(2, move.Move!.Value.Row);
        Assert.Equal(3, move.Move!.Value.Col);
        Assert.Equal("Iterations=500", move.Thought);
        Assert.Equal(before, move.BoardBefore);
        Assert.Equal(after, move.BoardAfter);
    }

    [Fact]
    public void ResolveNextFilePath_UsesDailyIncrementingSequence()
    {
        string directory = Path.Combine(Path.GetTempPath(), "othello-selfplay-kifu-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            var date = new DateOnly(2026, 7, 19);

            string first = SelfPlayKifuStore.ResolveNextFilePath(directory, date);
            Assert.EndsWith(Path.Combine("", "20260719_001.jsonl").TrimStart(Path.DirectorySeparatorChar), first, StringComparison.Ordinal);
            File.WriteAllText(first, "{}");

            string second = SelfPlayKifuStore.ResolveNextFilePath(directory, date);
            Assert.EndsWith(Path.Combine("", "20260719_002.jsonl").TrimStart(Path.DirectorySeparatorChar), second, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}

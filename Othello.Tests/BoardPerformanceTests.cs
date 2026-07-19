using Othello.Core;

namespace Othello.Tests;

public class BoardPerformanceTests
{
    [Fact]
    public void IsLegalMove_HighFrequencyCalls_HasLowAllocations()
    {
        var board = Board.CreateInitial();
        const int iterationCount = 200_000;

        for (var i = 0; i < 10_000; i++)
        {
            _ = board.IsLegalMove(new Move(i % Board.BoardSize, (i / Board.BoardSize) % Board.BoardSize));
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();

        for (var i = 0; i < iterationCount; i++)
        {
            var move = new Move(i % Board.BoardSize, (i / Board.BoardSize) % Board.BoardSize);
            _ = board.IsLegalMove(move);
        }

        long allocatedAfter = GC.GetAllocatedBytesForCurrentThread();
        long allocatedBytes = allocatedAfter - allocatedBefore;

        Assert.True(
            allocatedBytes <= 1024,
            $"IsLegalMove should avoid allocation-heavy paths. Allocated={allocatedBytes} bytes for {iterationCount} calls.");
    }

    [Fact]
    public void EnumerateLegalMoves_HighFrequencyCalls_ZeroAllocations()
    {
        var board = Board.CreateInitial();
        const int iterationCount = 200_000;

        // ウォームアップ
        Span<Move> warmup = stackalloc Move[64];
        for (var i = 0; i < 10_000; i++)
        {
            _ = board.EnumerateLegalMoves(warmup);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();

        Span<Move> buffer = stackalloc Move[64];
        for (var i = 0; i < iterationCount; i++)
        {
            _ = board.EnumerateLegalMoves(buffer);
        }

        long allocatedAfter = GC.GetAllocatedBytesForCurrentThread();
        long allocatedBytes = allocatedAfter - allocatedBefore;

        Assert.True(
            allocatedBytes == 0,
            $"EnumerateLegalMoves with stackalloc buffer must not allocate. Allocated={allocatedBytes} bytes for {iterationCount} calls.");
    }

    [Fact]
    public void TryApplyMove_HighFrequencyCalls_HasLowAllocations()
    {
        // Board を事前生成して GC リセット後は TryApplyMove のアロケーション分のみを計測する
        const int iterationCount = 10_000;
        var boards = new Board[iterationCount];

        for (int i = 0; i < iterationCount; i++)
        {
            boards[i] = Board.CreateInitial();
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();

        for (var i = 0; i < iterationCount; i++)
        {
            boards[i].TryApplyMove(new Move(2, 3));
        }

        long allocatedAfter = GC.GetAllocatedBytesForCurrentThread();
        long allocatedBytes = allocatedAfter - allocatedBefore;

        // CollectFlips由来の List<Position> がなければほぼゼロになるはず
        // (旧実装では List<Position>(12) ≈ 80 bytes/call = 800_000 bytes 以上)
        Assert.True(
            allocatedBytes <= 1024,
            $"TryApplyMove should not allocate a flip List<Position>. Allocated={allocatedBytes} bytes for {iterationCount} calls.");
    }
}

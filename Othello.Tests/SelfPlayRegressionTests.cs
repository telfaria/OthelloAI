using Othello.AI;
using Othello.Core;

namespace Othello.Tests;

public class SelfPlayRegressionTests
{
    [Fact]
    public void ParallelRootMcts_DoesNotAlwaysSelectSameOpeningMove_ForBlack()
    {
        var ai = new MctsAI(iterations: 200, maxDegreeOfParallelism: 4);
        var board = Board.CreateInitial();

        var selectedMoves = new HashSet<Move>();

        for (int i = 0; i < 40; i++)
        {
            AIDecision decision = ai.DecideMove(board);
            Assert.NotNull(decision.Move);
            selectedMoves.Add(decision.Move!.Value);
        }

        Assert.True(
            selectedMoves.Count >= 2,
            "Parallel root MCTS should not collapse to a single deterministic opening move across repeated trials.");
    }
}

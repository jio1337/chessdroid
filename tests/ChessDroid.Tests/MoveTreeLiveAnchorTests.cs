using ChessDroid.Models;

namespace ChessDroid.Tests;

/// <summary>
/// Locks in the exact behavior AnalysisBoardForm's "browse while a live engine match plays"
/// feature depends on: a live match's moves must always extend the game's own actual line,
/// never whatever position the viewer happens to have navigated CurrentNode to. Before
/// AddMoveAt existed, the only way to append a move was AddMove, which always appends to
/// CurrentNode — so navigating backward during a live match would silently fork the live
/// game into a variation branching off the browsed position instead of continuing it.
/// </summary>
public class MoveTreeLiveAnchorTests
{
    private const string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    private const string AfterE4 = "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1";
    private const string AfterE4E5 = "rnbqkbnr/pppp1ppp/8/4p3/4P3/8/PPPP1PPP/RNBQKBNR w KQkq e6 0 2";

    [Fact]
    public void AddMoveAt_DoesNotMoveCurrentNode()
    {
        var tree = new MoveTree(StartFen);
        var anchor = tree.Root;

        var node = tree.AddMoveAt(anchor, "e2e4", "e4", AfterE4);

        Assert.Same(anchor, tree.CurrentNode); // CurrentNode untouched
        Assert.Same(node, anchor.Children[0]); // move still landed in the tree
    }

    [Fact]
    public void AddMoveAt_ExtendsTheAnchorEvenWhileViewerBrowsesElsewhere()
    {
        var tree = new MoveTree(StartFen);
        var liveAnchor = tree.AddMove("e2e4", "e4", AfterE4); // anchor = after 1.e4

        // Viewer navigates back to the start position to browse, independent of the anchor.
        tree.GoToStart();
        Assert.Same(tree.Root, tree.CurrentNode);

        // A second live move arrives while the viewer is still looking at the start position.
        var newNode = tree.AddMoveAt(liveAnchor, "e7e5", "e5", AfterE4E5);

        // The live line extended from the anchor (1.e4 e5), not from wherever the viewer is.
        Assert.Same(newNode, liveAnchor.Children[0]);
        Assert.Equal(AfterE4E5, newNode.FEN);
        Assert.Empty(tree.Root.Children.FindAll(c => c != liveAnchor));
        Assert.Single(tree.Root.Children); // no stray variation branched off Root

        // The viewer's own browsing position is completely unaffected.
        Assert.Same(tree.Root, tree.CurrentNode);
    }

    [Fact]
    public void AddMoveAt_ReturnsExistingChild_WhenMoveAlreadyPlayed()
    {
        var tree = new MoveTree(StartFen);
        var anchor = tree.Root;
        var first = tree.AddMoveAt(anchor, "e2e4", "e4", AfterE4);

        var second = tree.AddMoveAt(anchor, "e2e4", "e4", AfterE4);

        Assert.Same(first, second);
        Assert.Single(anchor.Children);
    }
}

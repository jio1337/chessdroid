namespace ChessDroid.Models
{
    /// <summary>
    /// Represents a node in the move tree for analysis board variations.
    /// Supports branching variations where the user can explore alternative lines.
    /// </summary>
    public class MoveNode
    {
        /// <summary>UCI notation of the move (e.g., "e2e4")</summary>
        public string UciMove { get; set; } = "";

        /// <summary>SAN notation of the move (e.g., "e4")</summary>
        public string SanMove { get; set; } = "";

        /// <summary>FEN after this move is played</summary>
        public string FEN { get; set; } = "";

        /// <summary>FEN before this move (parent's FEN or initial position)</summary>
        public string ParentFEN { get; set; } = "";

        /// <summary>Parent node (null for root)</summary>
        public MoveNode? Parent { get; set; }

        /// <summary>Child nodes (alternative moves from this position)</summary>
        public List<MoveNode> Children { get; set; } = new List<MoveNode>();

        /// <summary>Half-move number (0-based, ply count from start)</summary>
        public int HalfMove { get; set; }

        /// <summary>Whether this is the main line (first child of parent)</summary>
        public bool IsMainLine => Parent == null || Parent.Children.Count == 0 || Parent.Children[0] == this;

        /// <summary>Variation depth (0 for main line, increments for each branch)</summary>
        public int VariationDepth { get; set; }

        /// <summary>
        /// Gets the move number for display (1-based, full move number)
        /// </summary>
        public int MoveNumber => (HalfMove / 2) + 1;

        /// <summary>
        /// Whether this is a white move
        /// </summary>
        public bool IsWhiteMove => HalfMove % 2 == 0;

        /// <summary>
        /// Add a child node (variation)
        /// </summary>
        public MoveNode AddChild(string uciMove, string sanMove, string fen)
        {
            var child = new MoveNode
            {
                UciMove = uciMove,
                SanMove = sanMove,
                FEN = fen,
                ParentFEN = this.FEN,
                Parent = this,
                HalfMove = this.HalfMove + 1,
                VariationDepth = this.VariationDepth
            };

            // If this is not the first child, it's a variation and goes deeper
            if (Children.Count > 0)
            {
                child.VariationDepth = this.VariationDepth + 1;
            }

            Children.Add(child);
            return child;
        }

        /// <summary>
        /// Check if a move already exists as a child
        /// </summary>
        public MoveNode? FindChild(string uciMove)
        {
            return Children.FirstOrDefault(c => c.UciMove == uciMove);
        }

        /// <summary>
        /// Get the main line continuation (first child)
        /// </summary>
        public MoveNode? GetMainLine()
        {
            return Children.Count > 0 ? Children[0] : null;
        }

        /// <summary>
        /// Get all variations (children except the first one)
        /// </summary>
        public List<MoveNode> GetVariations()
        {
            return Children.Skip(1).ToList();
        }

        /// <summary>
        /// Navigate to next move in main line
        /// </summary>
        public MoveNode? Next()
        {
            return GetMainLine();
        }

        /// <summary>
        /// Navigate to previous move
        /// </summary>
        public MoveNode? Previous()
        {
            return Parent;
        }

        /// <summary>
        /// Get the path from root to this node
        /// </summary>
        public List<MoveNode> GetPathFromRoot()
        {
            var path = new List<MoveNode>();
            var node = this;
            while (node != null && !string.IsNullOrEmpty(node.UciMove))
            {
                path.Insert(0, node);
                node = node.Parent;
            }
            return path;
        }

        /// <summary>
        /// Check if this node has variations (siblings with different moves)
        /// </summary>
        public bool HasSiblingVariations()
        {
            return Parent != null && Parent.Children.Count > 1;
        }

        /// <summary>
        /// Get index of this node among siblings
        /// </summary>
        public int GetVariationIndex()
        {
            if (Parent == null) return 0;
            return Parent.Children.IndexOf(this);
        }

        /// <summary>
        /// Navigate to next variation at same level
        /// </summary>
        public MoveNode? NextVariation()
        {
            if (Parent == null) return null;
            int idx = GetVariationIndex();
            if (idx < Parent.Children.Count - 1)
                return Parent.Children[idx + 1];
            return null;
        }

        /// <summary>
        /// Navigate to previous variation at same level
        /// </summary>
        public MoveNode? PreviousVariation()
        {
            if (Parent == null) return null;
            int idx = GetVariationIndex();
            if (idx > 0)
                return Parent.Children[idx - 1];
            return null;
        }
    }

    /// <summary>
    /// Represents the entire game tree with a root node
    /// </summary>
    public class MoveTree
    {
        /// <summary>Root node (represents starting position, no move)</summary>
        public MoveNode Root { get; private set; }

        /// <summary>Current position in the tree</summary>
        public MoveNode CurrentNode { get; set; }

        public MoveTree(string initialFEN)
        {
            Root = new MoveNode
            {
                FEN = initialFEN,
                HalfMove = -1, // Root is before first move
                VariationDepth = 0
            };
            CurrentNode = Root;
        }

        /// <summary>
        /// Add a move from the current position.
        /// If the move already exists, navigate to it.
        /// If we're not at the end of a line, creates a variation.
        /// </summary>
        public MoveNode AddMove(string uciMove, string sanMove, string fen)
        {
            // Check if move already exists
            var existing = CurrentNode.FindChild(uciMove);
            if (existing != null)
            {
                CurrentNode = existing;
                return existing;
            }

            // Add new move
            var newNode = CurrentNode.AddChild(uciMove, sanMove, fen);
            CurrentNode = newNode;
            return newNode;
        }

        /// <summary>
        /// Navigate to beginning (root)
        /// </summary>
        public void GoToStart()
        {
            CurrentNode = Root;
        }

        /// <summary>
        /// Navigate to end of current line
        /// </summary>
        public void GoToEnd()
        {
            while (CurrentNode.Children.Count > 0)
            {
                CurrentNode = CurrentNode.Children[0];
            }
        }

        /// <summary>
        /// Navigate forward one move (main line)
        /// </summary>
        public bool GoForward()
        {
            var next = CurrentNode.Next();
            if (next != null)
            {
                CurrentNode = next;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Navigate backward one move
        /// </summary>
        public bool GoBack()
        {
            if (CurrentNode.Parent != null)
            {
                CurrentNode = CurrentNode.Parent;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Navigate to a specific node
        /// </summary>
        public void GoToNode(MoveNode node)
        {
            CurrentNode = node;
        }

        /// <summary>
        /// Get the main line from root
        /// </summary>
        public List<MoveNode> GetMainLine()
        {
            var line = new List<MoveNode>();
            var node = Root;
            while (node.Children.Count > 0)
            {
                node = node.Children[0];
                line.Add(node);
            }
            return line;
        }

        /// <summary>
        /// Clear everything and reset to initial position
        /// </summary>
        public void Clear(string initialFEN)
        {
            Root = new MoveNode
            {
                FEN = initialFEN,
                HalfMove = -1,
                VariationDepth = 0
            };
            CurrentNode = Root;
        }

        /// <summary>
        /// Get total number of moves in main line
        /// </summary>
        public int MainLineLength => GetMainLine().Count;

        /// <summary>
        /// Check if we have any variations anywhere in the tree
        /// </summary>
        public bool HasVariations()
        {
            return HasVariationsRecursive(Root);
        }

        private bool HasVariationsRecursive(MoveNode node)
        {
            if (node.Children.Count > 1) return true;
            foreach (var child in node.Children)
            {
                if (HasVariationsRecursive(child)) return true;
            }
            return false;
        }
    }
}

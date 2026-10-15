namespace Features.MatrixModule.Scripts {
    public class Node {
        public int X { get; }
        public int Y { get; }
        public float TotalCost { get; set; }
        public float CostSoFar { get; set; }
        public float HeuristicCost { get; set; }
        public Node Parent { get; set; }

        public Node(int x, int y) {
            X = x;
            Y = y;
        }

        public override bool Equals(object obj) {
            if (!(obj is Node node))
                return false;

            return X == node.X && Y == node.Y;
        }

        public override int GetHashCode() =>
            X ^ Y;

    }
}
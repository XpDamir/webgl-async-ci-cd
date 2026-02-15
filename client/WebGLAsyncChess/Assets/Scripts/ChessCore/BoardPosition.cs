namespace ChessCore
{
    public struct BoardPosition
    {
        public int X;
        public int Y;

        public BoardPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is BoardPosition)) return false;
            var other = (BoardPosition)obj;
            return X == other.X && Y == other.Y;
        }

        public override int GetHashCode()
        {
            return X * 31 + Y;
        }
    }
}
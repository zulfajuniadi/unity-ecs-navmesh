namespace Town.Geom
{
    public struct Rectangle
    {
        public static readonly Rectangle Zero = new Rectangle(0, 0, 0, 0);
        public static readonly Rectangle One = new Rectangle(1, 1, 1, 1);

        public float X, Y, Width, Height;

        public Rectangle(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public float Left => X;

        public float Right => X + Width;

        public float Top => Y;

        public float Bottom => Y + Height;

        public Vector2 TopLeft => new Vector2(Left, Top);

        public Vector2 BottomRight => new Vector2(Right, Bottom);

        public Rectangle Expand(float amount)
        {
            return new Rectangle(X - amount, Y - amount, Width + 2 * amount, Height + 2 * amount);
        }

        public string ToSvgViewport()
        {
            return $"{X} {Y} {Width} {Height}";
        }
    }
}
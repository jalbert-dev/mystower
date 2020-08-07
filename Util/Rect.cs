using System;

namespace Util
{
    public struct Rect : IEquatable<Rect>
    {
        private int x, y, w, h;

        public static Rect FromBounds(int left, int top, int right, int bottom) => new Rect()
        {
            Left = left,
            Top = top,
            Right = right,
            Bottom = bottom,
        };

        public static Rect FromSize(int x, int y, int width, int height) => new Rect()
        {
            Left = x,
            Top = y,
            Width = width,
            Height = height,
        };

        public int Width
        {
            get => w;
            set => w = value;
        }
        public int Height
        {
            get => h;
            set => h = value;
        }

        public int Left
        {
            get => x;
            set => x = value;
        }
        public int Top
        {
            get => y;
            set => y = value;
        }
        public int Right
        {
            get => x + w;
            set => w = value - x;
        }
        public int Bottom
        {
            get => y + h;
            set => h = value - y;
        }

        public static bool Intersects(Rect a, Rect b)
             => a.Left <= b.Right && a.Right >= b.Left &&
                a.Top <= b.Bottom && a.Bottom >= b.Top;

        public static Rect ClampBounds(Rect a, int left, int top, int right, int bottom)
            => new Rect() {
                Left = Math.Max(a.Left, left), 
                Top = Math.Max(a.Top, top),
                Right = Math.Min(a.Right, right),
                Bottom = Math.Min(a.Bottom, bottom),
            };

        public bool Equals(Rect o) => x == o.x && y == o.y && w == o.w && h == o.h;
        public override bool Equals(object? obj) => obj is Rect v && Equals(v);
        public override int GetHashCode() => (x, y, w, h).GetHashCode();
        public static bool operator==(Rect a, Rect b) => a.Equals(b);
        public static bool operator!=(Rect a, Rect b) => !(a == b);
        public override string ToString() => $"{{{Left}x, {Top}y, {Width}w, {Height}h}}";
    }
}
namespace Sandaab.Core.Entities
{
    public record ScreenArea
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;
        public float DpiX;
        public float DpiY;

        public override int GetHashCode()
        {
            int hash = base.GetHashCode();
            hash = (hash * 7) + X.GetHashCode();
            hash = (hash * 7) + Y.GetHashCode();
            hash = (hash * 7) + Width.GetHashCode();
            hash = (hash * 7) + Height.GetHashCode();
            hash = (hash * 7) + DpiX.GetHashCode();
            hash = (hash * 7) + DpiY.GetHashCode();
            return hash;
        }
    }
}

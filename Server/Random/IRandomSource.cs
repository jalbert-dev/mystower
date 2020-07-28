namespace Server.Random
{
    public interface IRandomSource
    {
        int Next(int min, int max);
    }
}
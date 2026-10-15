using Core.Interfaces;

namespace Core.Services;

public class RandomProvider : IRandomProvider
{
    public int Next(int maxValue)
    {
        return Random.Shared.Next(maxValue);
    }
}

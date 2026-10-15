namespace Presentation.Extensions;

internal sealed class ModePin(Action restore) : IDisposable
{
    public void Dispose()
    {
        restore();
    }
}

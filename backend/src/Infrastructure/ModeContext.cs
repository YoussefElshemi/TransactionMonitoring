using Core.Enums;
using Core.Interfaces;

namespace Infrastructure;

public class ModeContext : IModeContext
{
    private Mode? _current;

    public Mode? Current
    {
        get => _current;
        set => _current = value;
    }

    public Mode CurrentOrLive => Current ?? Mode.Live;

    public IDisposable Pin(Mode mode)
    {
        var previous = _current;

        _current = mode;

        return new ModePin(() => _current = previous);
    }
}

internal sealed class ModePin(Action restore) : IDisposable
{
    public void Dispose()
    {
        restore();
    }
}

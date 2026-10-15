using Core.Enums;
using Core.Interfaces;

namespace Presentation.Extensions;

// Resolved on demand, not in middleware: the ApiClient scheme is only authenticated by the endpoint's policy, so the principal isn't settled until a repository asks.
public class HttpModeContext(IHttpContextAccessor httpContextAccessor) : IModeContext
{
    private Mode? _assigned;
    private bool _hasAssignment;

    public Mode? Current
    {
        get => _hasAssignment ? _assigned : Resolve();
        set
        {
            _hasAssignment = true;
            _assigned = value;
        }
    }

    public Mode CurrentOrLive => Current ?? Mode.Live;

    // Restore the latch as well as the value, or the rest of the request stays pinned (to live, for billing).
    public IDisposable Pin(Mode mode)
    {
        var hadAssignment = _hasAssignment;
        var assigned = _assigned;

        _hasAssignment = true;
        _assigned = mode;

        return new ModePin(() =>
        {
            _hasAssignment = hadAssignment;
            _assigned = assigned;
        });
    }

    private Mode? Resolve()
    {
        return ModeHeaderHelper.Resolve(httpContextAccessor.HttpContext);
    }
}

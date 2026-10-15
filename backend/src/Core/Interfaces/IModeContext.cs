using Core.Enums;

namespace Core.Interfaces;

public interface IModeContext
{
    // Null means no mode established (jobs, unauthenticated requests): reads are unfiltered and writes fall back to live.
    Mode? Current { get; set; }

    Mode CurrentOrLive { get; }

    IDisposable Pin(Mode mode);
}

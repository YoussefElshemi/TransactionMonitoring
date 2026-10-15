using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BehaviouralTests.TestHelpers;

// Counts round trips per async flow, not per interceptor, because the outbox background service shares the DbContext.
public class CommandCountingInterceptor : DbCommandInterceptor
{
    private readonly AsyncLocal<CountingScope?> _scope = new();

    private CountingScope? _lastStarted;

    public int Count => Current?.Count ?? 0;

    public IReadOnlyList<string> Commands => Current?.Snapshot() ?? [];

    private CountingScope? Current => _scope.Value ?? _lastStarted;

    public void Start()
    {
        var scope = new CountingScope();

        _lastStarted = scope;
        _scope.Value = scope;
    }

    public void Stop()
    {
        _scope.Value = null;
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        Record(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Record(command);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    private void Record(DbCommand command)
    {
        _scope.Value?.Record(command.CommandText);
    }

    private sealed class CountingScope
    {
        private readonly List<string> _commands = [];

        private int _count;

        public int Count => Volatile.Read(ref _count);

        public void Record(string commandText)
        {
            Interlocked.Increment(ref _count);

            lock (_commands)
            {
                _commands.Add(commandText);
            }
        }

        public IReadOnlyList<string> Snapshot()
        {
            lock (_commands)
            {
                return _commands.ToList();
            }
        }
    }
}

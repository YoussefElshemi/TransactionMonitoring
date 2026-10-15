namespace Core.Interfaces;

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken stoppingToken);
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken stoppingToken);
}
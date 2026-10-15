using Core.Models.Revenue;

namespace Core.Interfaces.Services;

public interface IRevenueService
{
    Task<List<RecurringRevenueMonth>> ListRecurringRevenueAsync(ListRecurringRevenueRequest listRecurringRevenueRequest, CancellationToken stoppingToken);
    Task<RevenueConcentration> GetConcentrationAsync(GetRevenueConcentrationRequest getRevenueConcentrationRequest, CancellationToken stoppingToken);
    Task<List<CustomerMargin>> ListMarginsAsync(ListCustomerMarginRequest listCustomerMarginRequest, CancellationToken stoppingToken);
    Task<RecoveryStatistic> GetRecoveryAsync(GetRecoveryStatisticRequest getRecoveryStatisticRequest, CancellationToken stoppingToken);
    Task<List<CustomerHealth>> ListHealthAsync(ListCustomerHealthRequest listCustomerHealthRequest, CancellationToken stoppingToken);
    Task<ExportedRevenueRecognition> ExportRecognitionAsync(ExportRevenueRecognitionRequest exportRevenueRecognitionRequest, CancellationToken stoppingToken);
}

using Core.Exceptions;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Models.Availability;
using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Services;

public class AvailabilityCommitmentService(
    IAvailabilityCommitmentRepository availabilityCommitmentRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IAvailabilityCommitmentService
{
    public async Task<AvailabilityCommitment> CreateAsync(CreateAvailabilityCommitmentRequest createAvailabilityCommitmentRequest, CancellationToken stoppingToken)
    {
        var availabilityCommitment = createAvailabilityCommitmentRequest.ToModel(timeProvider);
        var effectiveFrom = (DateTimeOffset)availabilityCommitment.EffectiveFrom;

        if (availabilityCommitment.EffectiveTo is not null &&
            availabilityCommitment.EffectiveTo.Value <= effectiveFrom)
        {
            throw new InvalidTimeRangeException("availability commitment");
        }

        var current = await availabilityCommitmentRepository.GetInEffectAsync(
            availabilityCommitment.OrganizationId, effectiveFrom, stoppingToken);

        if (current is not null)
        {
            if (current.EffectiveFrom > effectiveFrom)
            {
                throw new AvailabilityCommitmentOverlapException(availabilityCommitment.OrganizationId);
            }

            await availabilityCommitmentRepository.CloseAsync(
                current.AvailabilityCommitmentId, new EffectiveTo(effectiveFrom), stoppingToken);
        }

        await availabilityCommitmentRepository.AddAsync(availabilityCommitment, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await GetAsync(availabilityCommitment.AvailabilityCommitmentId, stoppingToken);
    }

    public Task<AvailabilityCommitment> GetAsync(AvailabilityCommitmentId availabilityCommitmentId, CancellationToken stoppingToken)
    {
        return availabilityCommitmentRepository.GetAsync(availabilityCommitmentId, stoppingToken);
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, AvailabilityCommitmentId availabilityCommitmentId, CancellationToken stoppingToken)
    {
        return availabilityCommitmentRepository.ExistsAsync(organizationId, availabilityCommitmentId, stoppingToken);
    }

    public Task<AvailabilityCommitment?> GetInEffectAsync(OrganizationId organizationId, DateTimeOffset asOf, CancellationToken stoppingToken)
    {
        return availabilityCommitmentRepository.GetInEffectAsync(organizationId, asOf, stoppingToken);
    }

    public Task<List<AvailabilityCommitment>> ListInEffectAsync(DateTimeOffset asOf, CancellationToken stoppingToken)
    {
        return availabilityCommitmentRepository.ListInEffectAsync(asOf, stoppingToken);
    }

    public async Task DeleteAsync(AvailabilityCommitmentId availabilityCommitmentId, CancellationToken stoppingToken)
    {
        await availabilityCommitmentRepository.DeleteAsync(availabilityCommitmentId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public Task<PagedResponse<AvailabilityCommitment>> ListAsync(ListAvailabilityCommitmentRequest listAvailabilityCommitmentRequest, CancellationToken stoppingToken)
    {
        return availabilityCommitmentRepository.ListAsync(listAvailabilityCommitmentRequest, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListAvailabilityCommitmentRequest listAvailabilityCommitmentRequest, CancellationToken stoppingToken)
    {
        return availabilityCommitmentRepository.GetCountAsync(listAvailabilityCommitmentRequest, stoppingToken);
    }
}

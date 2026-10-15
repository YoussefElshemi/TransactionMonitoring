using Core.Models.BackTests;

namespace Core.Interfaces.Services;

public interface IBackTestResultService : IPagedService<ListBackTestResultRequest, BackTestResult>;

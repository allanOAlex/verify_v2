using Verify.Application.Abstractions.IRepositories;

namespace Verify.Application.Abstractions.Interfaces;
public interface IUnitOfWork
{
    ILogRepository LogRepository { get; }


    Task<int> CompleteAsync();
}

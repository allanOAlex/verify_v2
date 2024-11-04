using Verify.Application.Abstractions.Interfaces;
using Verify.Application.Abstractions.IRepositories;
using Verify.Persistence.DataContext;

namespace Verify.Infrastructure.Implementations.Interfaces;
public class UnitOfWork : IUnitOfWork
{
    public ILogRepository LogRepository { get; private set; }

    private readonly DbContext _context;

    public UnitOfWork(
        ILogRepository logRepository,
        DbContext context)
    {
        LogRepository = logRepository;
        _context = context;
    }



    public Task<int> CompleteAsync()
    {
        var result = _context.SaveChangesAsync();
        return result;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);

    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _context.Dispose();
        }
    }
}


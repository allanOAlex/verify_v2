using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Verify.Application.Abstractions.Interfaces;
using Verify.Application.Abstractions.IRepositories;
using Verify.Persistence.DataContext;

namespace Verify.Infrastructure.Implementations.Interfaces;
public class UnitOfWork : IUnitOfWork
{
    public ILogRepository LogRepository { get; private set; }

    private readonly DBContext context;

    public UnitOfWork(
        ILogRepository logRepository,
        DBContext Context)
    {
        LogRepository = logRepository;
        context = Context;
    }



    public Task<int> CompleteAsync()
    {
        var result = context.SaveChangesAsync();
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
            context.Dispose();
        }
    }
}


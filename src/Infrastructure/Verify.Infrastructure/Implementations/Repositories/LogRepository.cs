using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using Verify.Application.Abstractions.IRepositories;
using Verify.Domain.Entities;

namespace Verify.Infrastructure.Implementations.Repositories;
internal sealed class LogRepository : IBaseRepository<Log>, ILogRepository
{
    public LogRepository()
    {
        
    }

    public Task<Log> CreateAsync(Log entity)
    {
        throw new NotImplementedException();
    }

    public Task<Log> DeleteAsync(Log entity)
    {
        throw new NotImplementedException();
    }

    public IQueryable<Log> FindAll()
    {
        throw new NotImplementedException();
    }

    public IQueryable<Log> FindByCondition(Expression<Func<Log, bool>> expression)
    {
        throw new NotImplementedException();
    }

    public Task<Log?> FindByIdAsync(int Id)
    {
        throw new NotImplementedException();
    }

    public Task<Log> UpdateAsync(Log entity)
    {
        throw new NotImplementedException();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using Verify.Application.Abstractions.IRepositories;

namespace Verify.Infrastructure.Implementations.Repositories;
public class BaseRepository<T> : IBaseRepository<T> where T : class
{
    public BaseRepository()
    {

    }

    public Task<T> CreateAsync(T entity)
    {
        throw new NotImplementedException();
    }

    public Task<T> DeleteAsync(T entity)
    {
        throw new NotImplementedException();
    }

    public IQueryable<T> FindAll()
    {
        throw new NotImplementedException();
    }

    public IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression)
    {
        throw new NotImplementedException();
    }

    public Task<T?> FindByIdAsync(int Id)
    {
        throw new NotImplementedException();
    }

    public Task<T> UpdateAsync(T entity)
    {
        throw new NotImplementedException();
    }

    
}

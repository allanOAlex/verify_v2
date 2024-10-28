using System.Linq.Expressions;
using Verify.Application.Abstractions.IRepositories;

namespace Verify.Infrastructure.Implementations.Repositories;
public class BaseRepository<T> : IBaseRepository<T> where T : class
{
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

    public Task<T?> FindByIdAsync(int id)
    {
        throw new NotImplementedException();
    }

    public Task<T> UpdateAsync(T entity)
    {
        throw new NotImplementedException();
    }

    
}

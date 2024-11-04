using System.Linq.Expressions;

namespace Verify.Application.Abstractions.IRepositories;
public interface IBaseRepository<T> where T : class
{
    Task<T> CreateAsync(T entity);
    Task<T> DeleteAsync(T entity);
    IQueryable<T> FindAll();
    IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression);
    Task<T?> FindByIdAsync(int id);
    Task<T> UpdateAsync(T entity);
    

}

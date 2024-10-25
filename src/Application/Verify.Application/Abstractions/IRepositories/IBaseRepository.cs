using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Verify.Application.Abstractions.IRepositories;
public interface IBaseRepository<T> where T : class
{
    Task<T> CreateAsync(T entity);
    Task<T> DeleteAsync(T entity);
    IQueryable<T> FindAll();
    IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression);
    Task<T?> FindByIdAsync(int Id);
    Task<T> UpdateAsync(T entity);
    

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using Verify.Application.Dtos.Common;
using Verify.Application.Dtos.Log;



namespace Verify.Application.Abstractions.IServices;

public interface ILogService
{
    Task<Response<LogResponse>> CreateAsync(CreateLogRequest request);
    Task<Response<LogResponse>> DeleteAsync(int Id);
    Task<Response<List<LogResponse>>> FindAllAsync(PaginationSetting paginationSetting);
    Task<Response<LogResponse>> FindByIdAsync(int Id);
    Task<Response<List<LogResponse>>> SearchAsync(SearchRequest searchRequest);    
    

}

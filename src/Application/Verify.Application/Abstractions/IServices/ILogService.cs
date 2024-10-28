using Verify.Application.Dtos.Common;
using Verify.Application.Dtos.Logging;



namespace Verify.Application.Abstractions.IServices;

public interface ILogService
{
    Task<Response<LogResponse>> CreateAsync(CreateLogRequest request);
    Task<Response<LogResponse>> DeleteAsync(int id);
    Task<Response<List<LogResponse>>> FindAllAsync(PaginationSetting paginationSetting);
    Task<Response<LogResponse>> FindByIdAsync(int id);
    Task<Response<List<LogResponse>>> SearchAsync(SearchRequest searchRequest);    
    

}

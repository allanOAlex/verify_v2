using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verify.Application.Abstractions.IServices;
using Verify.Application.Dtos.Common;
using Verify.Application.Dtos.Log;


namespace Verify.Infrastructure.Implementations.Services;

internal sealed class LogService : ILogService
{
    public LogService()
    {
            
    }

    public Task<Response<LogResponse>> CreateAsync(CreateLogRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<Response<LogResponse>> DeleteAsync(int Id)
    {
        throw new NotImplementedException();
    }

    public Task<Response<List<LogResponse>>> FindAllAsync(PaginationSetting paginationSetting)
    {
        throw new NotImplementedException();
    }

    public Task<Response<LogResponse>> FindByIdAsync(int Id)
    {
        throw new NotImplementedException();
    }

    public Task<Response<List<LogResponse>>> SearchAsync(SearchRequest searchRequest)
    {
        throw new NotImplementedException();
    }

}

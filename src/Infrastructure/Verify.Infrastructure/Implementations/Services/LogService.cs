using Serilog;
using Verify.Application.Abstractions.IServices;
using Verify.Application.Dtos.Common;
using Verify.Application.Dtos.Logging;


namespace Verify.Infrastructure.Implementations.Services;

internal sealed class LogService : ILogService
{
    public Task<Response<LogResponse>> CreateAsync(CreateLogRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<Response<LogResponse>> DeleteAsync(int id)
    {
        throw new NotImplementedException();
    }

    public Task<Response<List<LogResponse>>> FindAllAsync(PaginationSetting paginationSetting)
    {
        throw new NotImplementedException();
    }

    public async Task<Response<LogResponse>> FindByIdAsync(int id)
    {
        Log.Information("FindByIdAsync called with id: {Id}", id);
        try
        {
            // Simulating a data-fetch operation. Replace with actual data-fetch logic.
            LogResponse? log = await FetchLogByIdAsync();

            if (log == null)
            {
                Log.Warning("Log not found for id: {Id}", id);
                return Response<LogResponse>.Failure("Log not found");
            }
                
            Log.Information("Log found for id: {Id}", id);
            return Response<LogResponse>.Success("Log found", log);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while fetching the log for id: {Id}", id);
            return Response<LogResponse>.Failure("An error occurred while fetching the log", null, ex);
        }
    }

    public Task<Response<List<LogResponse>>> SearchAsync(SearchRequest searchRequest)
    {
        throw new NotImplementedException();
    }


    private async Task<LogResponse?> FetchLogByIdAsync()
    {
        // Simulate fetching the log entry. Replace with actual repository fetch logic.
        await Task.Delay(100); // Simulating some async work.

        // This is just for simulation. In real-life scenario, the log would be fetched from database or any persistence storage.
        var log = new LogResponse();
        return log; // or return null if not found
    }

}

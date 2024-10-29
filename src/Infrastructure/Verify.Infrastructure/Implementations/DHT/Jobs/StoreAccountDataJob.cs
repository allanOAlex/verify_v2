using Quartz;
using Verify.Application.Abstractions.DHT;
using Verify.Application.Abstractions.DHT.Jobs;

namespace Verify.Infrastructure.Implementations.DHT.Jobs;


internal sealed class StoreAccountDataJob : IStoreAccountDataJob
{
    private readonly IDhtRedisService _dHtRedisService;
    private readonly IHashingService _hashingService;


    public StoreAccountDataJob(
        IDhtRedisService dHtRedisService,
        IHashingService hashingService)
    {
        _dHtRedisService = dHtRedisService;
        _hashingService = hashingService;
    }

    public Task Execute(IJobExecutionContext context)
    {
        throw new NotImplementedException();
    }
}

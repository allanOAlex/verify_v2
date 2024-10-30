using Polly;
using Quartz;
using Quartz.Util;
using StackExchange.Redis;
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

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var accountHash = context.MergedJobDataMap["AccountHash"] as byte[] ?? [];
            var serializedAccountInfo = context.MergedJobDataMap["SerializedAccountInfo"] as string ?? string.Empty;

            if (accountHash != null && !serializedAccountInfo.IsNullOrWhiteSpace())
            {
                // Retry policy in case of transient Redis failures
                await Policy
                    .Handle<RedisException>()
                    .RetryAsync(3)
                    .ExecuteAsync(async () =>
                        await _dHtRedisService.SetNodeAsync($"dht:accounts", accountHash, serializedAccountInfo, TimeSpan.FromHours(24)));

                await _dHtRedisService.SetNodeAsync($"dht:accounts", accountHash, serializedAccountInfo!, TimeSpan.FromHours(24));
            }
        }
        catch (Exception)
        {

            throw;
        }
        
    }
}

using Verify.Application.Dtos.Common;

namespace Verify.Application.Abstractions.DHT;
public interface IHashingService
{
    Task<DhtResponse<byte[]>> ByteHash(string valueToHash);
    Task<DhtResponse<string>> StringHash(string valueToHash);

    
}

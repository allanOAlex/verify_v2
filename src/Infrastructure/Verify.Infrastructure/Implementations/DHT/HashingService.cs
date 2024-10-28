using System.Security.Cryptography;
using System.Text;
using Verify.Application.Abstractions.DHT;
using Verify.Application.Dtos.Common;

namespace Verify.Infrastructure.Implementations.DHT;
internal sealed class HashingService : IHashingService
{
	public async Task<DhtResponse<byte[]>> ByteHash(string valueToHash)
	{
		using var sha256 = SHA256.Create();
		return await Task.FromResult(
			DhtResponse<byte[]>.Success(
				"", 
				sha256.ComputeHash(Encoding.UTF8.GetBytes(valueToHash))
			));
	}

    public async Task<DhtResponse<string>> StringHash(string valueToHash)
    {
	    using var sha256 = SHA256.Create();
	    var bytes = Encoding.UTF8.GetBytes(valueToHash);
	    var hash = sha256.ComputeHash(bytes);
	    return await Task.FromResult(
		    DhtResponse<string>.Success(
			    "", Convert.ToBase64String(hash)
		    ));
    }

}

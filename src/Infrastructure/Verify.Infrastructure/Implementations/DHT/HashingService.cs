using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Verify.Application.Abstractions.DHT;
using Verify.Application.Dtos.Common;

namespace Verify.Infrastructure.Implementations.DHT;
internal sealed class HashingService : IHashingService
{
	public HashingService()
	{
			
	}

    public async Task<DHTResponse<byte[]>> ByteHash(string valueToHash)
    {
		try
		{
            using var sha256 = SHA256.Create();
            return await Task.FromResult(
                DHTResponse<byte[]>.Success(
                "", 
                sha256.ComputeHash(Encoding.UTF8.GetBytes(valueToHash))
                ));
        }
		catch (Exception)
		{

			throw;
		}
    }

    public async Task<DHTResponse<string>> StringHash(string valueToHash)
    {
		try
		{
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(valueToHash);
            var hash = sha256.ComputeHash(bytes);
            return await Task.FromResult(
                DHTResponse<string>.Success(
                    "", Convert.ToBase64String(hash)
                    ));
        }
		catch (Exception)
		{

			throw;
		}
    }

}

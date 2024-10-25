using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verify.Application.Dtos.Common;

namespace Verify.Application.Abstractions.DHT;
public interface IHashingService
{
    Task<DHTResponse<byte[]>> ByteHash(string valueToHash);
    Task<DHTResponse<string>> StringHash(string valueToHash);

    
}

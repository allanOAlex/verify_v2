using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Verify.Shared.Exceptions;
public class BadRequestException : CustomException
{
    public BadRequestException(string message) : base(message, null, HttpStatusCode.BadRequest) { }
}

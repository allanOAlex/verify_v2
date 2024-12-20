﻿using System.Net;

namespace Verify.Shared.Exceptions;
public class BadRequestException : CustomException
{
    public BadRequestException(string message) : base(message, null, HttpStatusCode.BadRequest) { }
}

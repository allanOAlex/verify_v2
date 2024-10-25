﻿using FluentValidation;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Verify.Application.Abstractions.Interfaces;
using Verify.Application.Dtos.Account;
using Verify.Application.Dtos.Common;
using Verify.Application.Validations.Account.RequestValidators;
using Verify.Shared.Exceptions;

namespace Verify.Api.Controllers;



[Route("api/[controller]")]
[ApiController]
public class DHTController : ControllerBase
{
    private readonly IServiceManager serviceManager;

    public DHTController(IServiceManager ServiceManager)
    {
        serviceManager = ServiceManager;
    }

    [HttpPost("fetchaccountinfo")]
    public async Task<ActionResult<AccountInfo>> FetchAccountData([FromBody] AccountRequest fetchAccountRequest)
    {
        var validator = new AccountRequestValidator();
        if (!validator.Validate(fetchAccountRequest).IsValid)
        {
            throw new ValidationException("Request Object is Invalid", errors: validator.Validate(fetchAccountRequest).Errors);
        }

        var serviceResponse = await serviceManager.DHTService.FetchAccountData(fetchAccountRequest);
        if (!serviceResponse.Successful)
        {
            if (serviceResponse.Exception is NoContentException || serviceResponse.Data == null)
                return NoContent();

            return BadRequest(serviceResponse);
        }

        if (serviceResponse.Data == null)
            return NoContent();

        return Ok(serviceResponse.Data);
    }

   


}


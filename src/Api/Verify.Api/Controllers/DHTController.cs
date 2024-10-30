using FluentValidation;
using Microsoft.AspNetCore.Mvc;

using Verify.Application.Abstractions.Interfaces;
using Verify.Application.Dtos.Account;
using Verify.Application.Validations.Account.RequestValidators;
using Verify.Shared.Exceptions;

namespace Verify.Api.Controllers;



[Route("api/[controller]")]
[ApiController]
public class DhtController : ControllerBase
{
    private readonly IServiceManager _serviceManager;

    public DhtController(IServiceManager serviceManager)
    {
        _serviceManager = serviceManager;
    }

    [HttpPost("fetchaccountinfo")]
    public async Task<ActionResult<AccountInfo>> FetchAccountData([FromBody] AccountRequest fetchAccountRequest)
    {
        var validator = new AccountRequestValidator();
        if (!validator.Validate(fetchAccountRequest).IsValid)
        {
            throw new ValidationException("Request Object is Invalid", errors: validator.Validate(fetchAccountRequest).Errors);
        }

        var serviceResponse = await _serviceManager.DhtService.FetchAccountData_(fetchAccountRequest);
        //var serviceResponse = await _serviceManager.DhtService.FetchAccountData(fetchAccountRequest);
        //var serviceResponse = await _serviceManager.DhtService._FetchAccountData(fetchAccountRequest);
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



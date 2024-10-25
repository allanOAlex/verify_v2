﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Web.BankOne.Api.Features.Account.Dtos;

namespace Web.BankOne.Api.Controllers;



[Route("api/[controller]")]
[ApiController]
public class AccountController : ControllerBase
{
    private static readonly List<AccountResponse> AccountData = new List<AccountResponse>
    {
        new AccountResponse
        {
            AccountId = "123",
            FirstName = "John",
            LastName = "Doe",
            OtherNames = "Sr.",
            AccountNumber = "2456345645"
        },
        new AccountResponse
        {
            AccountId = "124",
            FirstName = "John",
            LastName = "The",
            OtherNames = "Baptist",
            AccountNumber = "2456345646"
        },
    };

    public AccountController()
    {
            
    }

    [HttpGet("ping")]
    public async Task<ActionResult<bool>> Ping()
    {
        try
        {
            return Ok(true);
        }
        catch (Exception)
        {

            throw;
        }
    }

    [HttpPost("fetchaccountinfo")]
    public async Task<ActionResult<AccountResponse>> FetchAccountInfo([FromBody] AccountRequest accountRequest)
    {
        try
        {
            var accountInfo = AccountData.FirstOrDefault(a => a.AccountNumber == accountRequest.RecipientAccountNumber);
            if (accountInfo == null)
            {
                return NotFound("Account not found");
            }

            return Ok(accountInfo);
        }
        catch (Exception)
        {

            throw;
        }
    }

}

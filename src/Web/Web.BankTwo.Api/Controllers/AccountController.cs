﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Web.BankTwo.Api.Features.Account.Dtos;

namespace Web.BankTwo.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AccountController : ControllerBase
{

    private static readonly List<AccountResponse> AccountData = new List<AccountResponse>
    {
        new AccountResponse
        {
            AccountId = "125",
            FirstName = "Tressa",
            LastName = "Of",
            OtherNames = "Avila",
            AccountNumber = "2456345647"
        },
        new AccountResponse
        {
            AccountId = "456",
            FirstName = "Jane",
            LastName = "Smith",
            OtherNames = "Elizabeth",
            AccountNumber = "333444555"
        }
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

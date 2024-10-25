using Microsoft.AspNetCore.Http;
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
            OtherNames = "Middle",
            AccountNumber = "2456345645"
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

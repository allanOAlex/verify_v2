﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;

using Verify.Application.Dtos.Account;

namespace Verify.Application.Validations.Account.RequestValidators;
public class AccountRequestValidator : AbstractValidator<AccountRequest>
{
    public AccountRequestValidator()
    {
        RuleFor(x => x.InitiatorBIC)
            .NotEmpty().WithMessage("Initiator BIC is required.")
            .Length(8, 11).WithMessage("BIC must be between 8 and 11 characters.");

        RuleFor(x => x.RecipientBIC)
            .NotEmpty().WithMessage("Recipient BIC is required.")
            .Length(8, 11).WithMessage("BIC must be between 8 and 11 characters.");

        RuleFor(x => x.RecipientAccountNumber)
            .NotEmpty().WithMessage("Recipient account number is required.")
            .Length(8, 20).WithMessage("Account number must be between 8 and 20 characters.");
    }
}

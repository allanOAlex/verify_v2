using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Verify.Domain.Entities;

namespace Verify.Persistence.EntityConfigurations;
internal sealed class AccountEntityTypeConfiguration : IEntityTypeConfiguration<Account>
{
    public AccountEntityTypeConfiguration()
    {
            
    }

    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder
            .Property(e => e.AccountName)
            .IsRequired();

        builder
            .Property(e => e.AccountNumber)
            .IsRequired();

        builder
            .Property(e => e.AccountBIC)
            .IsRequired();

    }
}

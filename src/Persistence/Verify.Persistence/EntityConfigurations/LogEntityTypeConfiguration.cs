using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Verify.Domain.Entities;

namespace Verify.Persistence.EntityConfigurations;
internal sealed class LogEntityTypeConfiguration : IEntityTypeConfiguration<Log>
{
    public void Configure(EntityTypeBuilder<Log> builder)
    {
        builder
            .Property(e => e.LogLevel)
            .IsRequired();

        builder
            .Property(e => e.Message)
            .IsRequired();


    }
}

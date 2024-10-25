using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Verify.Domain.Entities;

namespace Verify.Persistence.DataContext;
public class DBContext : DbContext
{
    public DBContext(DbContextOptions<DBContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DBContext).Assembly);
    }

    public DbSet<Account> Accounts { get; set; }
    public DbSet<Log> Logs { get; set; }


}

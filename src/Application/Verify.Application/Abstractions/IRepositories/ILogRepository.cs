using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

using Verify.Domain.Entities;

namespace Verify.Application.Abstractions.IRepositories;
public interface ILogRepository : IBaseRepository<Log>
{
}

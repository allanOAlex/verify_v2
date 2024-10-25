using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verify.Application.Abstractions.IRepositories;

namespace Verify.Application.Abstractions.Interfaces;
public interface IUnitOfWork
{
    ILogRepository LogRepository { get; }


    Task<int> CompleteAsync();
}

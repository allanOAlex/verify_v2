using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Verify.Application.Abstractions.MessageQueuing;
public interface IMessageConsumer
{
    Task ConsumeAsync<T>(Func<T, Task> handler) where T : class;
}

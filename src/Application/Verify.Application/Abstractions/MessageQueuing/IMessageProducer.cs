using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Verify.Application.Abstractions.MessageQueuing;
public interface IMessageProducer
{
    Task ProduceAsync<T>(T message) where T : class;
}

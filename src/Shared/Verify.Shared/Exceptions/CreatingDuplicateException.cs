using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Verify.Shared.Exceptions;
public class CreatingDuplicateException : CustomException
{
    public CreatingDuplicateException(string message = null!) : base(message: message)
    {
    }
}
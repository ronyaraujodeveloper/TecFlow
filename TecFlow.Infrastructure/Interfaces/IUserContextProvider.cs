using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TecFlow.Infrastructure.Interfaces
{
    public interface IUserContextProvider
    {
        int? GetCurrentUserId();
    }
}

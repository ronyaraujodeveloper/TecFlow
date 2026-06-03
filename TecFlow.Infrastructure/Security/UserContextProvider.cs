using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using TecFlow.Infrastructure.Interfaces;

namespace TecFlow.Infrastructure.Security
{

    public class UserContextProvider : IUserContextProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserContextProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int? GetCurrentUserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var claim = user?.FindFirst(ClaimTypes.NameIdentifier);

            if (claim != null && int.TryParse(claim.Value, out int userId))
            {
                return userId;
            }

            return null;
        }
    }
}
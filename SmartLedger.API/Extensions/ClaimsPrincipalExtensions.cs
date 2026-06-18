using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SmartLedger.API.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static string? GetSmartLedgerUserId(this ClaimsPrincipal? user)
        {
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            return user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue("userId")
                ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        }
    }
}

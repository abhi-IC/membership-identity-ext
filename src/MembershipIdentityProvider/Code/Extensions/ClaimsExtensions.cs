using System.Security.Claims;

namespace MembershipIdentityProvider.Code.Extensions
{
    public static class ClaimsExtensions
    {
        public static List<string> GetUserRoles (this ClaimsPrincipal userClaimsPrincipal)
        {
            if (userClaimsPrincipal == null)
                return [];

            var userRoles = userClaimsPrincipal.Claims.Where(x => x.Type == ClaimTypes.Role).Select(x => x.Value.ToLower()).ToList();
            return userRoles;
        }
    }
}

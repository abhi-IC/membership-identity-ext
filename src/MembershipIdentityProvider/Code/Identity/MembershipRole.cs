using Microsoft.AspNetCore.Identity;

namespace MembershipIdentityProvider.Code.Identity
{
    public class MembershipRole : IdentityRole
    {
        public string RoleId { get; set; }
        public string RoleName { get; set; }
        public string LoweredRoleName { get; set; }
        public string Description { get; set; }
    }
}

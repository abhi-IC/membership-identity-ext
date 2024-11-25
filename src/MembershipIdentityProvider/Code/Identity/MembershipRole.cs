using Microsoft.AspNetCore.Identity;

namespace MembershipIdentityProvider.Code.Identity
{
    public class MembershipRole : IdentityRole<Guid>
    {
        public string Description { get; set; }
    }
}

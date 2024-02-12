using MembershipIdentityProvider.Code.Identity;
using Microsoft.AspNetCore.Identity;

namespace MembershipIdentityProvider.Code.Extensions
{
    public static class MembershipIdentityExtensions
    {
        /// <summary>
        /// Registers the Membership Identity services with the specified options.
        /// </summary>
        public static IServiceCollection AddMembershipIdentity<TUser, TRole>(this IServiceCollection services, Action<IdentityOptions> options = null)
            where TUser : MembershipUser
            where TRole : MembershipRole
        {
            if (options != null)
            {
                services.Configure(options);
                services.AddIdentity<TUser, TRole>(options)
                    .AddDefaultTokenProviders();
            }
            else
            {
                services.AddIdentity<TUser, TRole>()
                    .AddDefaultTokenProviders();
            }

            services.AddTransient<IPasswordHasher<TUser>, MembershipPasswordHasher<TUser>>();

            return services;
        }
    }
}

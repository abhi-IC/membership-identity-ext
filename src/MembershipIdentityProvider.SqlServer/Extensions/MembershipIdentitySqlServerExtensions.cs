using MembershipIdentityProvider.Code;
using MembershipIdentityProvider.Code.Extensions;
using MembershipIdentityProvider.Code.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace MembershipIdentityProvider.SqlServer.Extensions
{
    public static class MembershipIdentitySqlServerExtensions
    {
        public static IServiceCollection AddMembershipIdentitySqlServer<TUser, TRole>(this IServiceCollection services, string connectionString, MembershipSettings membershipSettings)
            where TUser : MembershipUser
            where TRole : MembershipRole
        {
            services.AddMembershipIdentity<TUser, TRole>();

            services.AddSingleton<IUserStore<TUser>>(o => new SqlServerMembershipUserStore<TUser>(connectionString, membershipSettings));
            services.AddSingleton<IRoleStore<TRole>>(o => new SqlServerMembershipRoleStore<TRole>(connectionString, membershipSettings));

            return services;
        }
    }
}

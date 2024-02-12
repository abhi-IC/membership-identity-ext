using MembershipIdentityProvider.Code.Extensions;
using MembershipIdentityProvider.Code.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace MembershipIdentityProvider.SqlServer.Extensions
{
    public static class MembershipIdentitySqlServerExtensions
    {
        public static IServiceCollection AddMembershipIdentitySqlServer<TUser, TRole>(this IServiceCollection services, string connectionString)
            where TUser : MembershipUser
            where TRole : MembershipRole
        {
            services.AddMembershipIdentity<TUser, TRole>();

            services.AddSingleton<IUserStore<TUser>>(o => new SqlServerMembershipUserStore<TUser>(connectionString));
            //services.AddSingleton<IUserRoleStore<TUser>>(o => new SqlServerMembershipUserRoleStore<TUser>(connectionString));
            services.AddSingleton<IRoleStore<TRole>>(o => new SqlServerMembershipRoleStore<TRole>(connectionString));

            return services;
        }
    }
}

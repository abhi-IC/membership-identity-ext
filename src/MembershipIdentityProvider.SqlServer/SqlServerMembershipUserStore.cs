using Dapper;
using MembershipIdentityProvider.Code.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;

namespace MembershipIdentityProvider.SqlServer
{
    public class SqlServerMembershipUserStore<TUser> : IUserStore<TUser>, IUserPasswordStore<TUser>, IUserRoleStore<TUser>
        where TUser : MembershipUser
    {
        private readonly string? _connectionString;

        public SqlServerMembershipUserStore(string? connectionString) 
        {
            _connectionString = connectionString;
        }

        #region IUserStore
        public Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            
        }

        public async Task<TUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            TUser user;
            using (var conn = new SqlConnection(_connectionString))
            {
                user = await conn.QueryFirstOrDefaultAsync<TUser>(
                    @"select top 1 mbm.UserId as Id, Username, lower(Username) as NormalizedUserName, Password as PasswordHash, PasswordSalt, Email, lower(Email) as NormalizedEmail, IsApproved, IsLockedOut, CreateDate, LastLoginDate, 
                      LastPasswordChangedDate, LastLockoutDate, FailedPasswordAttemptCount, FailedPasswordAttemptWindowStart, FailedPasswordAnswerAttemptCount, FailedPasswordAnswerAttemptWindowStart, Comment
                      from aspnet_users u inner join aspnet_membership mbm on u.UserId = mbm.UserId where u.UserId = @UserId",
                    param: new { UserId = Guid.Parse(userId) },
                    commandType: System.Data.CommandType.Text);
            }

            return user;
        }

        public async Task<TUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            TUser user;
            using (var conn = new SqlConnection(_connectionString))
            {
                user = await conn.QueryFirstOrDefaultAsync<TUser>(
                    @"select top 1 mbm.UserId as Id, Username, lower(Username) as NormalizedUserName, Password as PasswordHash, PasswordSalt, Email, lower(Email) as NormalizedEmail, IsApproved, IsLockedOut, CreateDate, LastLoginDate, LastPasswordChangedDate, 
                      LastLockoutDate, FailedPasswordAttemptCount, FailedPasswordAttemptWindowStart, FailedPasswordAnswerAttemptCount, FailedPasswordAnswerAttemptWindowStart, Comment
                      from aspnet_users u inner join aspnet_membership mbm on u.UserId = mbm.UserId where u.UserName = @UserName",
                    param: new { UserName = normalizedUserName.ToLower() },
                    commandType: System.Data.CommandType.Text);
            }

            return user;
        }

        public Task<string?> GetNormalizedUserNameAsync(TUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string?> GetPasswordHashAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.PasswordHash);
        }

        public Task<string> GetUserIdAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Id.ToString());
        }

        public Task<string?> GetUserNameAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.UserName?.ToString());
        }

        public Task SetUserNameAsync(TUser user, string? userName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region IUserPasswordStore
        public Task<bool> HasPasswordAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.PasswordHash != null);
        }

        public Task SetNormalizedUserNameAsync(TUser user, string? normalizedName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SetPasswordHashAsync(TUser user, string? passwordHash, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task AddToRoleAsync(TUser user, string roleName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task RemoveFromRoleAsync(TUser user, string roleName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<IList<string>> GetRolesAsync(TUser user, CancellationToken cancellationToken)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var list = await conn.QueryAsync<string>(@"select RoleName from aspnet_roles
                                               where RoleId in (select RoleId from aspnet_UsersInRoles where UserId = @UserId)",
                    param: new { UserId = user.Id },
                    commandType: System.Data.CommandType.Text);
                
                return list.ToList();
            }
        }

        public Task<bool> IsInRoleAsync(TUser user, string roleName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IList<TUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
        #endregion



    }
}

using System.Security.Claims;
using Dapper;
using MembershipIdentityProvider.Code.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;

namespace MembershipIdentityProvider.SqlServer
{
    public class SqlServerMembershipUserStore<TUser>(string? connectionString) 
        : IUserPasswordStore<TUser>, IUserRoleStore<TUser>, IUserClaimStore<TUser>
		where TUser : MembershipUser
    {

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
            Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			// Cleanup
		}

        ~SqlServerMembershipUserStore()
        {
            Dispose(false);
        }

		public async Task<TUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            TUser user;
            using (var conn = new SqlConnection(connectionString))
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
            using (var conn = new SqlConnection(connectionString))
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
            using (var conn = new SqlConnection(connectionString))
            {
                var list = await conn.QueryAsync<string>(@"select RoleName from aspnet_roles
                                               where RoleId in (select RoleId from aspnet_UsersInRoles where UserId = @UserId)",
                    param: new { UserId = user.Id },
                    commandType: System.Data.CommandType.Text);
                
                return list.ToList();
            }
        }

        public async Task<bool> IsInRoleAsync(TUser user, string roleName, CancellationToken cancellationToken)
        {
			using var conn = new SqlConnection(connectionString);
			var queryResult = await conn.ExecuteScalarAsync<int> (
                sql: @"select count(*) from aspnet_UsersInRoles uir 
                       inner join aspnet_roles r on r.RoleId = uir.RoleId 
                       where uir.UserId = @UserId and r.RoleName = @RoleName",
				commandType: System.Data.CommandType.Text,
				param: new { 
                UserId = user.Id,
                RoleName = roleName
            });
            
            return queryResult > 0;
		}

        public Task<IList<TUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

		public async Task<IList<Claim>> GetClaimsAsync(TUser user, CancellationToken cancellationToken)
		{
            var roles = await GetRolesAsync(user, cancellationToken);
            return roles.Select(x => new Claim(ClaimTypes.Role, x)).ToList();
		}

		public Task AddClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public Task ReplaceClaimAsync(TUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public Task RemoveClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public Task<IList<TUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}
		#endregion
	}
}

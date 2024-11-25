using System.Data;
using System.Data.SqlTypes;
using System.Security.Claims;
using Dapper;
using MembershipIdentityProvider.Code;
using MembershipIdentityProvider.Code.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;

namespace MembershipIdentityProvider.SqlServer
{
	public class SqlServerMembershipUserStore<TUser>(string? connectionString, MembershipSettings membershipSettings)
		: IUserPasswordStore<TUser>, IUserRoleStore<TUser>, IUserClaimStore<TUser>
		where TUser : MembershipUser
	{

		#region IUserStore
		public async Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(user);

			using var conn = new SqlConnection(connectionString);
			await conn.OpenAsync(cancellationToken);
			using var transaction = await conn.BeginTransactionAsync(cancellationToken);

			try
			{
				user.Id = Guid.NewGuid();
				user.PasswordSalt = MembershipPasswordHasher<TUser>.GenerateSalt();
				user.PasswordHash = MembershipPasswordHasher<TUser>.GetPassword(user, user.PasswordFormat, user.Password);

				// Insert into aspnet_users table
				var userInsertResult = await conn.ExecuteAsync(
					sql: @"insert into aspnet_users (ApplicationId, UserId, UserName, LoweredUserName, IsAnonymous, LastActivityDate, CreatedDate) 
						values (@ApplicationId, @UserId, @UserName, @LoweredUserName, @IsAnonymous, @LastActivityDate, @CreatedDate)",
					param: new
					{
						ApplicationId = membershipSettings.ApplicationId, // uses the injected applicationId
						UserId = user.Id,
						UserName = user.UserName,
						LoweredUserName = user.UserName?.ToLower(),
						IsAnonymous = false,
						LastActivityDate = DateTime.UtcNow,
						CreatedDate = DateTime.UtcNow
					},
					transaction: transaction,
					commandType: CommandType.Text);

				// Insert into aspnet_membership table
				var membershipInsertResult = await conn.ExecuteAsync(
					@"INSERT INTO dbo.aspnet_Membership (
						  ApplicationId,
						  UserId,
						  Password,
						  PasswordSalt,
						  PasswordFormat,
						  Email,
						  LoweredEmail,
						  PasswordQuestion,
						  PasswordAnswer,
						  IsApproved,
						  IsLockedOut,
						  CreateDate,
						  LastLoginDate,
						  LastPasswordChangedDate,
						  LastLockoutDate,
						  FailedPasswordAttemptCount,
						  FailedPasswordAttemptWindowStart,
						  FailedPasswordAnswerAttemptCount,
						  FailedPasswordAnswerAttemptWindowStart )
					  VALUES (
						  @ApplicationId,
						  @UserId,
						  @Password,
						  @PasswordSalt,
						  @PasswordFormat,
						  @Email,
						  LOWER(@Email),
						  @PasswordQuestion,
						  @PasswordAnswer,
						  @IsApproved,
						  @IsLockedOut,
						  @CreateDate,
						  @CreateDate,
						  @CreateDate,
						  @LastLockoutDate,
						  @FailedPasswordAttemptCount,
						  @FailedPasswordAttemptWindowStart,
						  @FailedPasswordAnswerAttemptCount,
						  @FailedPasswordAnswerAttemptWindowStart)",
					param: new
					{
						ApplicationId = membershipSettings.ApplicationId,
						UserId = user.Id,
						Password = user.PasswordHash, // uses the provided password (clear when format=0, hashed when format=1)
						PasswordSalt = user.PasswordSalt,
						PasswordFormat = user.PasswordFormat,
						Email = user.Email,
						PasswordQuestion = user.PasswordQuestion,
						PasswordAnswer = user.PasswordAnswer,
						IsApproved = true,
						IsLockedOut = false,
						CreateDate = DateTime.UtcNow,
						LastLockoutDate = SqlDateTime.MinValue.Value,
						FailedPasswordAttemptCount = 0,
						FailedPasswordAttemptWindowStart = SqlDateTime.MinValue.Value,
						FailedPasswordAnswerAttemptCount = 0,
						FailedPasswordAnswerAttemptWindowStart = SqlDateTime.MinValue.Value
					},
					transaction: transaction,
					commandType: CommandType.Text);

				// Commit transaction if both inserts succeed
				if (userInsertResult > 0 && membershipInsertResult > 0)
				{
					await transaction.CommitAsync(cancellationToken);
					return IdentityResult.Success;
				}
				else
				{
					await transaction.RollbackAsync(cancellationToken);
					return IdentityResult.Failed(new IdentityError { Description = "Failed to create user." });
				}
			}
			catch (Exception ex)
			{
				await transaction.RollbackAsync(cancellationToken);
				return IdentityResult.Failed(new IdentityError { Description = ex.Message });
			}
		}

		public async Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(user);

			using var conn = new SqlConnection(connectionString);
			await conn.OpenAsync(cancellationToken);

			using var transaction = await conn.BeginTransactionAsync(cancellationToken);

			try
			{
				// Delete user roles
				await conn.ExecuteAsync(@"delete from aspnet_UsersInRoles where UserId = @UserId",
					param: new { UserId = user.Id },
					transaction: transaction,
					commandType: CommandType.Text);

				// Delete membership record
				await conn.ExecuteAsync(@"delete from aspnet_membership where UserId = @UserId",
					param: new { UserId = user.Id },
					transaction: transaction,
					commandType: CommandType.Text);

				// Delete user record
				var rowsAffected = await conn.ExecuteAsync(@"delete from aspnet_users where UserId = @UserId",
					param: new { UserId = user.Id },
					transaction: transaction,
					commandType: CommandType.Text);

				if (rowsAffected == 0)
				{
					await transaction.RollbackAsync(cancellationToken);
					return IdentityResult.Failed(new IdentityError { Description = $"Failed to delete user with ID '{user.Id}'." });
				}

				// Commit transaction
				await transaction.CommitAsync(cancellationToken);
				return IdentityResult.Success;
			}
			catch (Exception ex)
			{
				await transaction.RollbackAsync(cancellationToken);
				return IdentityResult.Failed(new IdentityError { Description = ex.Message });
			}
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
			ArgumentNullException.ThrowIfNullOrEmpty(userId);

			TUser user;
			using var conn = new SqlConnection(connectionString);
			await conn.OpenAsync(cancellationToken);
			
			user = await conn.QueryFirstOrDefaultAsync<TUser>(
					@"select top 1 mbm.UserId as Id, Username, lower(Username) as NormalizedUserName, Password as PasswordHash, PasswordFormat, PasswordSalt, Email, lower(Email) as NormalizedEmail, IsApproved, IsLockedOut, CreateDate, LastLoginDate, 
                      LastPasswordChangedDate, LastLockoutDate, FailedPasswordAttemptCount, FailedPasswordAttemptWindowStart, FailedPasswordAnswerAttemptCount, FailedPasswordAnswerAttemptWindowStart, Comment
                      from aspnet_users u inner join aspnet_membership mbm on u.UserId = mbm.UserId where u.UserId = @UserId",
					param: new { UserId = Guid.Parse(userId) },
					commandType: CommandType.Text);


			return user;
		}

		public async Task<TUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNullOrEmpty(normalizedUserName);

			TUser user;
			using var conn = new SqlConnection(connectionString);
			await conn.OpenAsync(cancellationToken);

			user = await conn.QueryFirstOrDefaultAsync<TUser>(
					@"select top 1 mbm.UserId as Id, Username, lower(Username) as NormalizedUserName, Password as PasswordHash, PasswordFormat, PasswordSalt, Email, lower(Email) as NormalizedEmail, IsApproved, IsLockedOut, CreateDate, LastLoginDate, 
                      LastPasswordChangedDate, LastLockoutDate, FailedPasswordAttemptCount, FailedPasswordAttemptWindowStart, FailedPasswordAnswerAttemptCount, FailedPasswordAnswerAttemptWindowStart, Comment
                      from aspnet_users u inner join aspnet_membership mbm on u.UserId = mbm.UserId where u.UserName = @UserName",
					param: new { UserName = normalizedUserName.ToLower() },
					commandType: CommandType.Text);
			
			return user;
		}

		public async Task<string?> GetNormalizedUserNameAsync(TUser user, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(user);

			using var conn = new SqlConnection(connectionString);
			await conn.OpenAsync(cancellationToken);

			// Retrieve the normalized username from the database
			var normalizedUserName = await conn.QueryFirstOrDefaultAsync<string>(@"select upper(UserName) as NormalizedUserName from aspnet_users where UserId = @UserId",
				param: new { UserId = user.Id },
				commandType: CommandType.Text);

			return normalizedUserName;
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
			ArgumentNullException.ThrowIfNull(user);
			return Task.FromResult(user.UserName);
		}

		public Task SetUserNameAsync(TUser user, string? userName, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(user);
			ArgumentException.ThrowIfNullOrEmpty(userName);

			user.UserName = userName;
			return Task.CompletedTask;
		}

		public Task SetNormalizedUserNameAsync(TUser user, string? normalizedName, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(user);
			ArgumentException.ThrowIfNullOrEmpty(normalizedName);

			user.NormalizedUserName = normalizedName.ToUpper(); // Uses uppercase for normalized username (Identity default)
			return Task.CompletedTask;
		}

		public async Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(user);

			using var conn = new SqlConnection(connectionString);
			await conn.OpenAsync(cancellationToken);
			using var transaction = await conn.BeginTransactionAsync(cancellationToken);

			try
			{
				// Update aspnet_users table
				var userUpdateResult = await conn.ExecuteAsync(@"update aspnet_users set UserName = @UserName, LoweredUserName = @LoweredUserName where UserId = @UserId",
					param: new
					{
						UserName = user.UserName,
						LoweredUserName = user.UserName?.ToLower(),
						UserId = user.Id
					},
					transaction: transaction,
					commandType: CommandType.Text);

				// Update aspnet_membership table
				var membershipUpdateResult = await conn.ExecuteAsync(
					@"update aspnet_membership 
						set Email = @Email, 
						  LoweredEmail = @LoweredEmail, 
						  Password = @PasswordHash, 
						  IsApproved = @IsApproved, 
						  IsLockedOut = @IsLockedOut, 
						  LastLoginDate = @LastLoginDate, 
						  LastPasswordChangedDate = @LastPasswordChangedDate, 
						  LastLockoutDate = @LastLockoutDate, 
						  FailedPasswordAttemptCount = @FailedPasswordAttemptCount, 
						  FailedPasswordAttemptWindowStart = @FailedPasswordAttemptWindowStart, 
						  FailedPasswordAnswerAttemptCount = @FailedPasswordAnswerAttemptCount, 
						  FailedPasswordAnswerAttemptWindowStart = @FailedPasswordAnswerAttemptWindowStart, 
						  Comment = @Comment 
						where UserId = @UserId",
					param: new
					{
						Email = user.Email,
						LoweredEmail = user.Email?.ToLower(),
						PasswordHash = user.PasswordHash,
						IsApproved = user.IsApproved,
						IsLockedOut = user.IsLockedOut,
						LastLoginDate = user.LastLoginDate,
						LastPasswordChangedDate = user.LastPasswordChangedDate,
						LastLockoutDate = user.LastLockoutDate,
						FailedPasswordAttemptCount = user.FailedPasswordAttemptCount,
						FailedPasswordAttemptWindowStart = user.FailedPasswordAttemptWindowStart,
						FailedPasswordAnswerAttemptCount = user.FailedPasswordAnswerAttemptCount,
						FailedPasswordAnswerAttemptWindowStart = user.FailedPasswordAnswerAttemptWindowStart,
						Comment = user.Comment,
						UserId = user.Id
					},
					transaction: transaction,
					commandType: CommandType.Text);

				// Commit transaction if both updates succeed
				if (userUpdateResult > 0 && membershipUpdateResult > 0)
				{
					await transaction.CommitAsync(cancellationToken);
					return IdentityResult.Success;
				}
				else
				{
					await transaction.RollbackAsync(cancellationToken);
					return IdentityResult.Failed(new IdentityError { Description = "Failed to update user." });
				}
			}
			catch (Exception ex)
			{
				await transaction.RollbackAsync(cancellationToken);
				return IdentityResult.Failed(new IdentityError { Description = ex.Message });
			}
		}
		#endregion

		#region IUserPasswordStore
		public Task<bool> HasPasswordAsync(TUser user, CancellationToken cancellationToken)
		{
			return Task.FromResult(user.PasswordHash != null);
		}

		public Task SetPasswordHashAsync(TUser user, string? passwordHash, CancellationToken cancellationToken)
		{
			user.PasswordHash = passwordHash;
			return Task.CompletedTask;
		}

		public async Task AddToRoleAsync(TUser user, string roleName, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(user);
			ArgumentException.ThrowIfNullOrEmpty(roleName);

			using var conn = new SqlConnection(connectionString);
			await conn.OpenAsync(cancellationToken);

			// Role exists?
			var roleId = await conn.QueryFirstOrDefaultAsync<Guid>(@"select RoleId from aspnet_roles where RoleName = @RoleName",
				param: new { RoleName = roleName },
				commandType: CommandType.Text);

			if (roleId == Guid.Empty)
				throw new InvalidOperationException($"Role '{roleName}' does not exist.");

			// User already added to Role?
			var isInRole = await conn.ExecuteScalarAsync<int>(@"select count(*) from aspnet_UsersInRoles where UserId = @UserId and RoleId = @RoleId",
				param: new { UserId = user.Id, RoleId = roleId },
				commandType: CommandType.Text);

			if (isInRole > 0)
				throw new InvalidOperationException($"User is already in role '{roleName}'.");

			// Adds user to Role
			await conn.ExecuteAsync(@"insert into aspnet_UsersInRoles (UserId, RoleId) values (@UserId, @RoleId)",
				param: new { UserId = user.Id, RoleId = roleId },
				commandType: CommandType.Text);
		}

		public async Task RemoveFromRoleAsync(TUser user, string roleName, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(user);
			ArgumentException.ThrowIfNullOrEmpty(roleName);

			using var conn = new SqlConnection(connectionString);
			await conn.OpenAsync(cancellationToken);

			// Gets the RoleId
			var roleId = await conn.QueryFirstOrDefaultAsync<Guid>(@"select RoleId from aspnet_roles where RoleName = @RoleName",
				param: new { RoleName = roleName },
				commandType: CommandType.Text);

			if (roleId == Guid.Empty)
				throw new InvalidOperationException($"Role '{roleName}' does not exist.");

			// Remove the user from the role
			await conn.ExecuteAsync(@"delete from aspnet_UsersInRoles where UserId = @UserId and RoleId = @RoleId",
				param: new { UserId = user.Id, RoleId = roleId },
				commandType: CommandType.Text);
		}

		public async Task<IList<string>> GetRolesAsync(TUser user, CancellationToken cancellationToken)
		{
			using var conn = new SqlConnection(connectionString);
			await conn.OpenAsync(cancellationToken);

			var list = await conn.QueryAsync<string>(@"select RoleName from aspnet_roles where RoleId in (select RoleId from aspnet_UsersInRoles where UserId = @UserId)",
				param: new { UserId = user.Id },
				commandType: CommandType.Text);

			return [.. list];
		}

		public async Task<bool> IsInRoleAsync(TUser user, string roleName, CancellationToken cancellationToken)
		{
			using var conn = new SqlConnection(connectionString);
			await conn.OpenAsync(cancellationToken);

			var queryResult = await conn.ExecuteScalarAsync<int>(
				sql: @"select count(*) from aspnet_UsersInRoles uir 
                       inner join aspnet_roles r on r.RoleId = uir.RoleId 
                       where uir.UserId = @UserId and r.RoleName = @RoleName",
				commandType: CommandType.Text,
				param: new
				{
					UserId = user.Id,
					RoleName = roleName
				});

			return queryResult > 0;
		}

		public async Task<IList<TUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(roleName);

			using var conn = new SqlConnection(connectionString);
			await conn.OpenAsync(cancellationToken);

			// Get the RoleId for the given role name
			var roleId = await conn.QueryFirstOrDefaultAsync<Guid>(@"select RoleId from aspnet_roles where RoleName = @RoleName",
				param: new { RoleName = roleName },
				commandType: CommandType.Text);

			if (roleId == Guid.Empty)
				throw new InvalidOperationException($"Role '{roleName}' does not exist.");

			// Get the users in the specified role
			var users = await conn.QueryAsync<TUser>(@"select u.UserId as Id, u.UserName, lower(u.UserName) as NormalizedUserName, 
						m.Password as PasswordHash, m.PasswordFormat, m.PasswordSalt, 
						m.Email, lower(m.Email) as NormalizedEmail, m.IsApproved, m.IsLockedOut, 
						m.CreateDate, m.LastLoginDate, m.LastPasswordChangedDate, m.LastLockoutDate, 
						m.FailedPasswordAttemptCount, m.FailedPasswordAttemptWindowStart, 
						m.FailedPasswordAnswerAttemptCount, m.FailedPasswordAnswerAttemptWindowStart, m.Comment
					from aspnet_users u
					inner join aspnet_membership m on u.UserId = m.UserId
					inner join aspnet_UsersInRoles ur on u.UserId = ur.UserId
					where ur.RoleId = @RoleId",
				param: new { RoleId = roleId },
				commandType: CommandType.Text);

			return [.. users];
		}

		public async Task<IList<Claim>> GetClaimsAsync(TUser user, CancellationToken cancellationToken)
		{
			var roles = await GetRolesAsync(user, cancellationToken);
			return [.. roles.Select(x => new Claim(ClaimTypes.Role, x))];
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

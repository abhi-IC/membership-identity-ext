using System.Data;
using Dapper;
using MembershipIdentityProvider.Code;
using MembershipIdentityProvider.Code.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;

namespace MembershipIdentityProvider.SqlServer
{
	public class SqlServerMembershipRoleStore<TRole>(
		string? connectionString,
		MembershipSettings membershipSettings) : IRoleStore<TRole> where TRole : MembershipRole
	{
		public async Task<IdentityResult> CreateAsync(TRole role, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(role);

			try
			{
				role.Id = Guid.NewGuid();

				using var conn = new SqlConnection(connectionString);
				await conn.OpenAsync(cancellationToken);

				var rowsAffected = await conn.ExecuteAsync(
					@"insert into aspnet_roles (ApplicationId, RoleId, RoleName, LoweredRoleName, Description) 
                        values (@ApplicationId, @RoleId, @RoleName, @LoweredRoleName, @Description)",
					param: new
					{
						ApplicationId = membershipSettings.ApplicationId,
						RoleId = role.Id,
						RoleName = role.Name,
						LoweredRoleName = role.Name?.ToLower(),
						Description = role.Description
					},
					commandType: CommandType.Text);

				if (rowsAffected == 0)
					return IdentityResult.Failed(new IdentityError { Description = "Failed to create role." });

				return IdentityResult.Success;
			}
			catch (Exception ex)
			{
				return IdentityResult.Failed(new IdentityError { Description = ex.Message });
			}
		}

		public async Task<IdentityResult> DeleteAsync(TRole role, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(role);

			using var conn = new SqlConnection(connectionString);
			await conn.OpenAsync(cancellationToken);
			using var transaction = await conn.BeginTransactionAsync(cancellationToken);

			try
			{
				// Delete role associations in aspnet_UsersInRoles
				await conn.ExecuteAsync("delete from aspnet_UsersInRoles where RoleId = @RoleId",
					param: new { RoleId = role.Id },
					transaction: transaction,
					commandType: CommandType.Text);

				// Delete the role from aspnet_roles
				var rowsAffected = await conn.ExecuteAsync("delete from aspnet_roles where RoleId = @RoleId and ApplicationId = @ApplicationId",
					param: new
					{
						RoleId = role.Id,
						ApplicationId = membershipSettings.ApplicationId
					},
					transaction: transaction,
					commandType: CommandType.Text);

				if (rowsAffected == 0)
				{
					await transaction.RollbackAsync(cancellationToken);
					return IdentityResult.Failed(new IdentityError { Description = $"Failed to delete role with ID '{role.Id}'." });
				}

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
			GC.SuppressFinalize(this);
		}

		public async Task<TRole?> FindByIdAsync(string roleId, CancellationToken cancellationToken)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(roleId);

			try
			{
				using var conn = new SqlConnection(connectionString);
				await conn.OpenAsync(cancellationToken);

				// Query the aspnet_roles table to find the role by its ID
				var role = await conn.QueryFirstOrDefaultAsync<TRole>(
					@"select RoleId as Id, RoleName as Name, LoweredRoleName as NormalizedName 
					  from aspnet_roles 
					  where RoleId = @RoleId and ApplicationId = @ApplicationId",
					param: new
					{
						RoleId = Guid.Parse(roleId),
						ApplicationId = membershipSettings.ApplicationId
					},
					commandType: CommandType.Text);

				return role;
			}
			catch (Exception ex)
			{
				// Log or handle the exception as needed
				throw new InvalidOperationException($"An error occurred while finding the role with ID '{roleId}': {ex.Message}", ex);
			}
		}

		public async Task<TRole?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(normalizedRoleName);

			try
			{
				using var conn = new SqlConnection(connectionString);
				await conn.OpenAsync(cancellationToken);

				// Query the aspnet_roles table to find the role by its normalized name
				var role = await conn.QueryFirstOrDefaultAsync<TRole>(
					@"select RoleId as Id, RoleName as Name, upper(LoweredRoleName) as NormalizedName
					  from aspnet_roles 
					  where LoweredRoleName = @LoweredRoleName and ApplicationId = @ApplicationId",
					param: new
					{
						LoweredRoleName = normalizedRoleName.ToLower(),
						ApplicationId = membershipSettings.ApplicationId
					},
					commandType: CommandType.Text);

				return role;
			}
			catch (Exception ex)
			{
				// Log or handle the exception as needed
				throw new InvalidOperationException($"An error occurred while finding the role with normalized name '{normalizedRoleName}': {ex.Message}", ex);
			}
		}

		public Task<string?> GetNormalizedRoleNameAsync(TRole role, CancellationToken cancellationToken)
		{
			return Task.FromResult(role.NormalizedName);
		}

		public Task<string> GetRoleIdAsync(TRole role, CancellationToken cancellationToken)
		{
			return Task.FromResult(role.Id.ToString());
		}

		public Task<string?> GetRoleNameAsync(TRole role, CancellationToken cancellationToken)
		{
			return Task.FromResult(role.Name);
		}

		public Task SetNormalizedRoleNameAsync(TRole role, string? normalizedName, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(role);
			ArgumentException.ThrowIfNullOrWhiteSpace(normalizedName);
			role.NormalizedName = normalizedName.ToUpper();

			return Task.CompletedTask;
		}

		public Task SetRoleNameAsync(TRole role, string? roleName, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(role);
			ArgumentException.ThrowIfNullOrWhiteSpace(roleName);

			role.Name = roleName;
			return Task.CompletedTask;
		}

		public async Task<IdentityResult> UpdateAsync(TRole role, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(role);

			try
			{
				using var conn = new SqlConnection(connectionString);
				await conn.OpenAsync(cancellationToken);

				// Update the role in the aspnet_roles table
				var rowsAffected = await conn.ExecuteAsync(
					@"update aspnet_roles 
					  set RoleName = @RoleName, 
						  LoweredRoleName = @LoweredRoleName 
					  where RoleId = @RoleId and ApplicationId = @ApplicationId",
					param: new
					{
						ApplicationId = membershipSettings.ApplicationId,
						RoleId = role.Id,
						RoleName = role.Name,
						LoweredRoleName = role.Name?.ToLower()
					},
					commandType: CommandType.Text);

				if (rowsAffected == 0)
					return IdentityResult.Failed(new IdentityError { Description = $"Failed to update role with ID '{role.Id}'." });

				return IdentityResult.Success;
			}
			catch (Exception ex)
			{
				return IdentityResult.Failed(new IdentityError { Description = ex.Message });
			}
		}
	}
}

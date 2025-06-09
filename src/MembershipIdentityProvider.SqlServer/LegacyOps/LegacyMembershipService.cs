using Dapper;
using MembershipIdentityProvider.Code;
using MembershipIdentityProvider.Code.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MembershipIdentityProvider.SqlServer.LegacyOps
{
    public class LegacyMembershipService : ILegacyMembershipService
    {
        private readonly SqlServerMembershipUserStore<MembershipUser> _userStore;
        private readonly MembershipSettings _settings;
        private readonly string _connectionString;

        public MembershipSettings MembershipSettings
        {
            get { return _settings; }
        }

        public LegacyMembershipService(SqlServerMembershipUserStore<MembershipUser> userStore, MembershipSettings settings, string connectionString)
        {
            _userStore = userStore;
            _settings = settings;
            _connectionString = connectionString;
        }

        public async Task<bool> UnlockUserAsync(string username, CancellationToken cancellationToken = default)
        {
            var user = await _userStore.FindByNameAsync(username.ToLower(), cancellationToken);
            if (user is null) return false;

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            var result = await conn.ExecuteAsync(
                @"UPDATE aspnet_Membership 
                  SET 
                      IsLockedOut = 0,
                      FailedPasswordAttemptCount = 0,
                      FailedPasswordAttemptWindowStart = CONVERT(datetime, '17540101', 112),
                      FailedPasswordAnswerAttemptCount = 0,
                      FailedPasswordAnswerAttemptWindowStart = CONVERT(datetime, '17540101', 112),
                      LastLockoutDate = CONVERT(datetime, '17540101', 112)
                  WHERE UserId = @UserId",
                new
                {
                    UserId = user.Id
                });

            return result > 0;
        }

        public async Task<string?> ResetPasswordAsync(string username, string passwordAnswer, CancellationToken cancellationToken = default)
        {
            var user = await _userStore.FindByNameAsync(username.ToLower(), cancellationToken);
            if (user is null)
                return null;

            if (string.IsNullOrEmpty(user.PasswordAnswer) || !user.PasswordAnswer.Equals(passwordAnswer, StringComparison.OrdinalIgnoreCase))
                return null;

            // Generate a new temp password (you can use your own policy here)
            var newPassword = Guid.NewGuid().ToString("N")[..8]; // 8-char temp password

            var passwordSalt = MembershipPasswordHasher<MembershipUser>.GenerateSalt();
            var passwordHash = MembershipPasswordHasher<MembershipUser>.GetPassword(user, user.PasswordFormat, newPassword);

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            var result = await conn.ExecuteAsync(
                 @"UPDATE aspnet_Membership 
                   SET Password = @PasswordHash, PasswordSalt = @PasswordSalt, LastPasswordChangedDate = @Now 
                   WHERE UserId = @UserId",
                new
                {
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    Now = DateTime.UtcNow,
                    UserId = user.Id
                });

            return result > 0 ? newPassword : null;
        }       

        public async Task<bool> ChangePasswordAsync(string username, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
        {
            var user = await _userStore.FindByNameAsync(username.ToLower(), cancellationToken);
            if (user is null)
                return false;

            // Verify current password
            var currentHash = MembershipPasswordHasher<MembershipUser>.GetPassword(user, user.PasswordFormat, currentPassword);
            if (!string.Equals(currentHash, user.PasswordHash, StringComparison.Ordinal))
                return false;

            // Hash new password
            var passwordSalt = MembershipPasswordHasher<MembershipUser>.GenerateSalt();
            var passwordHash = MembershipPasswordHasher<MembershipUser>.GetPassword(user, user.PasswordFormat, newPassword);

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            var rows = await conn.ExecuteAsync(
                  @"UPDATE aspnet_Membership 
                    SET Password = @PasswordHash, PasswordSalt = @PasswordSalt, LastPasswordChangedDate = @Now
                    WHERE UserId = @UserId",
                new
                {
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    Now = DateTime.UtcNow,
                    UserId = user.Id
                });

            return rows > 0;
        }

        public async Task<bool> ValidateUserAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            // Load user from the membership store
            var user = await _userStore.FindByNameAsync(username.ToLower(), cancellationToken);
            if (user is null)
                return false;

            // Skip locked out or unapproved users if needed
            if (!user.IsApproved || user.IsLockedOut)
                return false;

            // Hash the incoming password using the same hasher
            var hashedInput = MembershipPasswordHasher<MembershipUser>.GetPassword(user, user.PasswordFormat, password);

            return string.Equals(hashedInput, user.PasswordHash, StringComparison.Ordinal);    
        }

        public async Task<string[]> GetRolesForUserAsync(string username, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(username))
                return Array.Empty<string>();

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);
           
             // Get roles
             var roles = await conn.QueryAsync<string>(
                                    @"SELECT r.RoleName
                              FROM aspnet_Users u
                              JOIN aspnet_UsersInRoles ur ON u.UserId = ur.UserId
                              JOIN aspnet_Roles r ON ur.RoleId = r.RoleId
                              WHERE LOWER(u.LoweredUserName) = LOWER(@UserName)
                                AND u.ApplicationId = @AppId
                                AND r.ApplicationId = @AppId
                              ORDER BY r.RoleName",
                new
                {
                    UserName = username,
                    AppId = _settings.ApplicationId
                });

            return roles.ToArray();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MembershipIdentityProvider.SqlServer.LegacyOps
{
    public interface ILegacyMembershipService
    {
        Task<bool> UnlockUserAsync(string username, CancellationToken cancellationToken = default);
        Task<string?> ResetPasswordAsync(string username, string passwordAnswer, CancellationToken cancellationToken = default);
        Task<bool> ChangePasswordAsync(string username, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
        Task<bool> ValidateUserAsync(string username, string password, CancellationToken cancellationToken = default);
    }
}

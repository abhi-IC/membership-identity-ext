using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;

namespace MembershipIdentityProvider.Code.Identity
{
	public class MembershipPasswordHasher<TUser> : IPasswordHasher<TUser>
		where TUser : MembershipUser
	{
		public string HashPassword(TUser user, string password)
		{
			var saltBytes = Convert.FromBase64String(user.PasswordSalt);
			var rfc2898DeriveBytes = new Rfc2898DeriveBytes(password, saltBytes, 10000, HashAlgorithmName.SHA256);
			var hashPassword = Convert.ToBase64String(rfc2898DeriveBytes.GetBytes(63));

			return hashPassword;
		}

		public PasswordVerificationResult VerifyHashedPassword(TUser user, string hashedPassword, string providedPassword)
			=> user.PasswordFormat 
			switch
			{
				0 => hashedPassword.Equals(providedPassword) ? PasswordVerificationResult.Success : PasswordVerificationResult.Failed,
				1 => hashedPassword.Equals(HashPassword(user, providedPassword)) ? PasswordVerificationResult.Success : PasswordVerificationResult.Failed,
				_ => PasswordVerificationResult.Failed
			};

		public static string GenerateSalt()
		{
			byte[] buf = new byte[16];
			new RNGCryptoServiceProvider().GetBytes(buf);

			return Convert.ToBase64String(buf);
		}

		public static string GetPassword(TUser user, int passwordFormat, string password)
		{
			return passwordFormat switch
			{
				0 => password,
				1 => new MembershipPasswordHasher<TUser>().HashPassword(user, password),
				_ => password
			};
		}
	}
}

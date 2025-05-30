using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using System.Text;

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

    /* --This may be more appropriate. Needs To be Tested---
     
    public class MembershipPasswordHasher<TUser> : IPasswordHasher<TUser>
        where TUser : MembershipUser
    {
        public string HashPassword(TUser user, string password)
        {
            // Legacy-compatible hash: SHA1(salt + Unicode(password))
            byte[] saltBytes = Convert.FromBase64String(user.PasswordSalt);
            byte[] passwordBytes = Encoding.Unicode.GetBytes(password); // Unicode = UTF-16LE

            byte[] combined = new byte[saltBytes.Length + passwordBytes.Length];
            Buffer.BlockCopy(saltBytes, 0, combined, 0, saltBytes.Length);
            Buffer.BlockCopy(passwordBytes, 0, combined, saltBytes.Length, passwordBytes.Length);

            using var sha1 = SHA1.Create();
            byte[] hashBytes = sha1.ComputeHash(combined);
            return Convert.ToBase64String(hashBytes);
        }

        public PasswordVerificationResult VerifyHashedPassword(TUser user, string hashedPassword, string providedPassword)
        {
            return user.PasswordFormat switch
            {
                0 => hashedPassword == providedPassword
                    ? PasswordVerificationResult.Success
                    : PasswordVerificationResult.Failed,

                1 => hashedPassword == HashPassword(user, providedPassword)
                    ? PasswordVerificationResult.Success
                    : PasswordVerificationResult.Failed,

                _ => PasswordVerificationResult.Failed
            };
        }

        public static string GenerateSalt()
        {
            byte[] salt = new byte[16]; // 128-bit salt
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(salt);
            return Convert.ToBase64String(salt);
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
    
    */

}

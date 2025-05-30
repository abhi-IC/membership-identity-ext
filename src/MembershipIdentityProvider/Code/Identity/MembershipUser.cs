﻿using Microsoft.AspNetCore.Identity;

namespace MembershipIdentityProvider.Code.Identity
{
    public class MembershipUser : IdentityUser<Guid>
    {
        public bool IsApproved { get; set; }
        public DateTime LastActivityDate { get; set; }
		
        public string Password { get; set; }
		public int PasswordFormat { get; set; }
		public string PasswordSalt { get; set; } = string.Empty;

		public string PasswordQuestion { get; set; } = string.Empty;
		public string PasswordAnswer { get; set; } = string.Empty;

		public DateTime CreateDate { get; set; }
        public DateTime LastLoginDate { get; set; }
        public DateTime LastPasswordChangedDate { get; set; }
        public DateTime LastLockoutDate { get; set; }
        public int FailedPasswordAttemptCount { get; set; }
        public DateTime FailedPasswordAttemptWindowStart { get; set; }
        public int FailedPasswordAnswerAttemptCount { get; set; }
        public DateTime FailedPasswordAnswerAttemptWindowStart { get; set; }
        public string Comment { get; set; } = string.Empty;
        public bool IsLockedOut { get; set; }

	}
}

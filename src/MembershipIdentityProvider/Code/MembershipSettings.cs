
namespace MembershipIdentityProvider.Code
{
	public class MembershipSettings
	{
		public Guid ApplicationId { get; set; }
		public int PasswordFormat { get; set; }
        public int MaxInvalidPasswordAttempts { get; set; } //added
    }
}

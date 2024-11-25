using System.ComponentModel.DataAnnotations;

namespace SampleWebsite.Code.Models
{
    public class RegisterViewModel
    {
		[Required]
		//[EmailAddress]
		public string UserLogin { get; set; }

		[Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

		[DataType(DataType.Text)]
		public string PasswordQuestion { get; set; }
		
		[DataType(DataType.Text)]
		public string PasswordAnswer { get; set; }


	}
}

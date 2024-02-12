using System.ComponentModel.DataAnnotations;

namespace SampleWebsite.Code.Models
{
    public class LoginViewModel
    {
        [Required]
        //[EmailAddress]
        public string UserLogin { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }

    }
}

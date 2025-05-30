using System.ComponentModel.DataAnnotations;

namespace SampleWebsite.Code.Models
{
    public class ChangePasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        public required string CurrentPassword { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public required string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword")]
        public required string ConfirmPassword { get; set; }
    }

}

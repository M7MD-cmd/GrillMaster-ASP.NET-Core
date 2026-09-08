using System.ComponentModel.DataAnnotations;

namespace GrillMaster.Models
{
    public class OrderFormViewModel
    {
        [Required(ErrorMessage = "Name is required")]
        [MinLength(3, ErrorMessage = "Name must be at least 3 characters")]
        public string fullname { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^[0-9]{10,11}$", ErrorMessage = "Invalid phone number")]
        public string phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required")]
        [MinLength(5, ErrorMessage = "Address must be at least 5 characters")]
        public string address { get; set; } = string.Empty;

        public List<string> meals { get; set; } = new();

        public Dictionary<string, int> quantities { get; set; } = new();
    }
}
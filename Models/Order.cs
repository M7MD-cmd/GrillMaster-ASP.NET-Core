using System.ComponentModel.DataAnnotations;

namespace GrillMaster.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [MinLength(3, ErrorMessage = "Name must be at least 3 characters")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^[0-9]{10,11}$", ErrorMessage = "Invalid phone number")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required")]
        [MinLength(5, ErrorMessage = "Address must be at least 5 characters")]
        public string Address { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string Status { get; set; } = "Pending";

        // User who placed the order
        public string? UserId { get; set; }

        public ApplicationUser? User { get; set; }

        public List<OrderItem> Items { get; set; } = new();

        public decimal TotalAmount =>
            Items.Sum(item => item.TotalPrice);
    }
}
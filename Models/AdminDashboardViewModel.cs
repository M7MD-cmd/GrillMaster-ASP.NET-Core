namespace GrillMaster.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int TotalMenuItems { get; set; }
        public int TotalCategories { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<Order> RecentOrders { get; set; } = new();
    }
}

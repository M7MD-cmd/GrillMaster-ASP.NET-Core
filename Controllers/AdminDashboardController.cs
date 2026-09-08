using GrillMaster.Data;
using GrillMaster.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GrillMaster.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly AppDbContext _context;

        public AdminDashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new AdminDashboardViewModel
            {
                TotalOrders = await _context.Orders.CountAsync(),
                PendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending"),
                TotalMenuItems = await _context.MenuItems.CountAsync(),
                TotalCategories = await _context.Categories.CountAsync(),
                TotalRevenue = await _context.Orders
                    .Where(o => o.Status != "Cancelled")
                    .SelectMany(o => o.Items)
                    .Select(i => (decimal?)i.UnitPrice * i.Quantity)
                    .SumAsync() ?? 0m,
                RecentOrders = await _context.Orders
                    .AsNoTracking()
                    .Include(o => o.Items)
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(8)
                    .ToListAsync()
            };

            return View(model);
        }
    }
}

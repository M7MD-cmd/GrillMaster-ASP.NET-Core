using GrillMaster.Data;
using GrillMaster.Models;
using Microsoft.EntityFrameworkCore;

namespace GrillMaster.Services
{
    public class OrderService : IOrderService
    {
        private readonly AppDbContext _context;
        private const int MaxQuantity = 50;

        public OrderService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<int> CreateOrderAsync(OrderFormViewModel formData, string? userId)
        {
            if (string.IsNullOrWhiteSpace(formData.fullname) ||
                string.IsNullOrWhiteSpace(formData.phone) ||
                string.IsNullOrWhiteSpace(formData.address))
            {
                throw new InvalidOperationException("Customer information is required.");
            }

            if (string.IsNullOrEmpty(userId))
            {
                throw new InvalidOperationException("You must be logged in to place an order.");
            }

            var requestedNames = formData.meals
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (requestedNames.Count == 0)
            {
                throw new InvalidOperationException("No valid menu items were selected.");
            }

            var menuItems = await _context.MenuItems
                .Where(m => requestedNames.Contains(m.Name))
                .ToListAsync();

            if (menuItems.Count == 0)
            {
                throw new InvalidOperationException("No valid menu items were selected.");
            }

            var order = new Order
            {
                CustomerName = formData.fullname.Trim(),
                Phone = formData.phone.Trim(),
                Address = formData.address.Trim(),
                Status = "Pending",
                UserId = userId,
                CreatedAt = DateTime.Now
            };

            foreach (var menuItem in menuItems)
            {
                var quantity = 1;

                if (formData.quantities.TryGetValue(menuItem.Name, out var selectedQuantity))
                {
                    quantity = Math.Clamp(selectedQuantity, 1, MaxQuantity);
                }

                order.Items.Add(new OrderItem
                {
                    MenuItemId = menuItem.Id,
                    Quantity = quantity,
                    // Always use the current server-side price.
                    UnitPrice = menuItem.Price
                });
            }

            if (!order.Items.Any())
            {
                throw new InvalidOperationException("No valid menu items were selected.");
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return order.Id;
        }

        public async Task<Order?> GetOrderByIdAsync(int id)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .ThenInclude(i => i.MenuItem)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<List<Order>> GetUserOrdersAsync(string userId)
        {
            return await _context.Orders
                .AsNoTracking()
                .Where(o => o.UserId == userId)
                .Include(o => o.Items)
                .ThenInclude(i => i.MenuItem)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .ThenInclude(i => i.MenuItem)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> UpdateStatusAsync(int id, string status)
        {
            var allowedStatuses = new[]
            {
                "Pending",
                "Confirmed",
                "Preparing",
                "OutForDelivery",
                "Delivered",
                "Cancelled"
            };

            var normalizedStatus = allowedStatuses.FirstOrDefault(
                x => string.Equals(x, status?.Trim(), StringComparison.OrdinalIgnoreCase));

            if (normalizedStatus == null)
            {
                return false;
            }

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return false;
            }

            order.Status = normalizedStatus;
            await _context.SaveChangesAsync();

            return true;
        }
    }
}

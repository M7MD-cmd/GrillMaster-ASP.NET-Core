using GrillMaster.Models;

namespace GrillMaster.Services
{
    public interface IOrderService
    {
        Task<int> CreateOrderAsync(
            OrderFormViewModel formData,
            string? userId
        );

        Task<Order?> GetOrderByIdAsync(int id);

        Task<List<Order>> GetAllOrdersAsync();

        Task<List<Order>> GetUserOrdersAsync(string userId);

        Task<bool> UpdateStatusAsync(int id, string status);
    }
}
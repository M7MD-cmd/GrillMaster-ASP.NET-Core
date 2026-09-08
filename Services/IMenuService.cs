using GrillMaster.Models;

namespace GrillMaster.Services
{
    public interface IMenuService
    {
        Task<List<Category>> GetMenuAsync();

        Task<List<MenuItem>> GetAllItemsAsync();
        Task<MenuItem?> GetItemByIdAsync(int id);
        Task AddItemAsync(MenuItem item);
        Task<bool> UpdateItemAsync(MenuItem item);
        Task<bool> DeleteItemAsync(int id);

        Task<List<Category>> GetAllCategoriesAsync();
        Task<Category?> GetCategoryByIdAsync(int id);
        Task AddCategoryAsync(Category category);
        Task<bool> UpdateCategoryAsync(Category category);
        Task<bool> DeleteCategoryAsync(int id);
    }
}
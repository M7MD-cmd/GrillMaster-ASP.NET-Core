using GrillMaster.Data;
using GrillMaster.Models;
using Microsoft.EntityFrameworkCore;

namespace GrillMaster.Services
{
    public class MenuService : IMenuService
    {
        private readonly AppDbContext _context;

        public MenuService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Category>> GetMenuAsync()
        {
            return await _context.Categories
                .AsNoTracking()
                .Include(c => c.MenuItems)
                .Where(c => c.Name != "Other")
                .OrderBy(c => c.Id)
                .ToListAsync();
        }

        public async Task<List<MenuItem>> GetAllItemsAsync()
        {
            return await _context.MenuItems
                .AsNoTracking()
                .Include(m => m.Category)
                .OrderBy(m => m.Id)
                .ToListAsync();
        }

        public async Task<MenuItem?> GetItemByIdAsync(int id)
        {
            return await _context.MenuItems
                .AsNoTracking()
                .Include(m => m.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task AddItemAsync(MenuItem item)
        {
            item.Name = item.Name.Trim();

            _context.MenuItems.Add(item);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UpdateItemAsync(MenuItem item)
        {
            var existingItem = await _context.MenuItems
                .FirstOrDefaultAsync(m => m.Id == item.Id);

            if (existingItem == null)
            {
                return false;
            }

            existingItem.Name = item.Name.Trim();
            existingItem.Description = item.Description?.Trim();
            existingItem.Price = item.Price;
            existingItem.ImageUrl = item.ImageUrl;
            existingItem.CategoryId = item.CategoryId;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteItemAsync(int id)
        {
            var item = await _context.MenuItems
                .FirstOrDefaultAsync(m => m.Id == id);

            if (item == null)
            {
                return false;
            }

            // Preserve historical orders. A menu item used by an order must not be deleted.
            var isUsedInOrders = await _context.OrderItems
                .AnyAsync(i => i.MenuItemId == id);

            if (isUsedInOrders)
            {
                return false;
            }

            _context.MenuItems.Remove(item);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            return await _context.Categories
                .AsNoTracking()
                .Include(c => c.MenuItems)
                .OrderBy(c => c.Id)
                .ToListAsync();
        }

        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            return await _context.Categories
                .AsNoTracking()
                .Include(c => c.MenuItems)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task AddCategoryAsync(Category category)
        {
            category.Name = category.Name.Trim();
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UpdateCategoryAsync(Category category)
        {
            var existingCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == category.Id);

            if (existingCategory == null)
            {
                return false;
            }

            existingCategory.Name = category.Name.Trim();
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            var category = await _context.Categories
                .Include(c => c.MenuItems)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null || category.MenuItems.Any())
            {
                return false;
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}

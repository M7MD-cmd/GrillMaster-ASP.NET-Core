using System.Text.Json;
using GrillMaster.Services;
using Microsoft.AspNetCore.Mvc;

namespace GrillMaster.Controllers
{
    public class MenuController : Controller
    {
        private const string SelectedItemsKey = "SelectedOrderItems";
        private const int MaxQuantity = 50;

        private readonly IMenuService _menuService;

        public MenuController(IMenuService menuService)
        {
            _menuService = menuService;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _menuService.GetMenuAsync();
            ViewBag.SelectedItems = GetSelectedItems();
            return View(categories);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToOrder(int menuItemId, int quantity = 1)
        {
            var menuItem = await _menuService.GetItemByIdAsync(menuItemId);

            if (menuItem == null)
            {
                return NotFound(new { success = false, message = "Menu item not found." });
            }

            quantity = Math.Clamp(quantity, 1, MaxQuantity);

            var selectedItems = GetSelectedItems();
            var existingItem = selectedItems.FirstOrDefault(x => x.MenuItemId == menuItemId);

            if (existingItem != null)
            {
                existingItem.Quantity = quantity;
            }
            else
            {
                selectedItems.Add(new SelectedOrderItem
                {
                    MenuItemId = menuItemId,
                    Quantity = quantity
                });
            }

            SaveSelectedItems(selectedItems);

            return Json(new
            {
                success = true,
                quantity = selectedItems.First(x => x.MenuItemId == menuItemId).Quantity,
                totalCount = selectedItems.Sum(x => x.Quantity)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int menuItemId, int quantity)
        {
            var menuItem = await _menuService.GetItemByIdAsync(menuItemId);

            if (menuItem == null)
            {
                return NotFound(new { success = false, message = "Menu item not found." });
            }

            var selectedItems = GetSelectedItems();
            var item = selectedItems.FirstOrDefault(x => x.MenuItemId == menuItemId);

            if (item == null)
            {
                return Json(new { success = false, message = "Item not found." });
            }

            item.Quantity = Math.Clamp(quantity, 1, MaxQuantity);
            SaveSelectedItems(selectedItems);

            return Json(new
            {
                success = true,
                quantity = item.Quantity,
                totalCount = selectedItems.Sum(x => x.Quantity)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveFromOrder(int menuItemId)
        {
            var selectedItems = GetSelectedItems();
            var removed = selectedItems.RemoveAll(x => x.MenuItemId == menuItemId);

            if (removed == 0)
            {
                return Json(new { success = false, message = "Item not found." });
            }

            SaveSelectedItems(selectedItems);

            return Json(new
            {
                success = true,
                totalCount = selectedItems.Sum(x => x.Quantity)
            });
        }

        private List<SelectedOrderItem> GetSelectedItems()
        {
            var json = HttpContext.Session.GetString(SelectedItemsKey);

            if (string.IsNullOrEmpty(json))
            {
                return new List<SelectedOrderItem>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<SelectedOrderItem>>(json)
                       ?? new List<SelectedOrderItem>();
            }
            catch
            {
                return new List<SelectedOrderItem>();
            }
        }

        private void SaveSelectedItems(List<SelectedOrderItem> selectedItems)
        {
            var json = JsonSerializer.Serialize(selectedItems);
            HttpContext.Session.SetString(SelectedItemsKey, json);
        }
    }

    public class SelectedOrderItem
    {
        public int MenuItemId { get; set; }
        public int Quantity { get; set; }
    }
}

using GrillMaster.Data;
using GrillMaster.Models;
using GrillMaster.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GrillMaster.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminMenuController : Controller
    {
        private const long MaxImageSize = 5 * 1024 * 1024;

        private readonly IMenuService _menuService;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public AdminMenuController(
            IMenuService menuService,
            AppDbContext context,
            IWebHostEnvironment environment)
        {
            _menuService = menuService;
            _context = context;
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var items = await _menuService.GetAllItemsAsync();
            return View(items);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadCategoriesAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MenuItem item, IFormFile? imageFile)
        {
            item.Name = item.Name?.Trim() ?? string.Empty;
            item.Description = item.Description?.Trim();

            if (await _context.MenuItems.AnyAsync(
                    m => m.Name.ToLower() == item.Name.ToLower()))
            {
                ModelState.AddModelError("Name", "A menu item with this name already exists.");
            }

            if (!await _context.Categories.AnyAsync(c => c.Id == item.CategoryId))
            {
                ModelState.AddModelError("CategoryId", "Please select a valid category.");
            }

            string? imagePath = null;

            if (imageFile != null && imageFile.Length > 0)
            {
                imagePath = await SaveImageAsync(imageFile);

                if (imagePath == null)
                {
                    ModelState.AddModelError("ImageUrl", "Invalid image. Use JPG, JPEG, PNG, or WEBP up to 5 MB.");
                }
            }

            if (!ModelState.IsValid)
            {
                DeleteLocalImage(imagePath);
                await LoadCategoriesAsync();
                return View(item);
            }

            item.ImageUrl = imagePath;
            await _menuService.AddItemAsync(item);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _menuService.GetItemByIdAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            await LoadCategoriesAsync();
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            MenuItem item,
            IFormFile? imageFile,
            bool removeImage = false)
        {
            item.Name = item.Name?.Trim() ?? string.Empty;
            item.Description = item.Description?.Trim();

            if (await _context.MenuItems.AnyAsync(
                    m => m.Id != item.Id &&
                         m.Name.ToLower() == item.Name.ToLower()))
            {
                ModelState.AddModelError("Name", "A menu item with this name already exists.");
            }

            if (!await _context.Categories.AnyAsync(c => c.Id == item.CategoryId))
            {
                ModelState.AddModelError("CategoryId", "Please select a valid category.");
            }

            var existingItem = await _menuService.GetItemByIdAsync(item.Id);

            if (existingItem == null)
            {
                return NotFound();
            }

            var newImagePath = existingItem.ImageUrl;

            if (removeImage)
            {
                newImagePath = null;
            }

            if (imageFile != null && imageFile.Length > 0)
            {
                newImagePath = await SaveImageAsync(imageFile);

                if (newImagePath == null)
                {
                    ModelState.AddModelError("ImageUrl", "Invalid image. Use JPG, JPEG, PNG, or WEBP up to 5 MB.");
                }
            }

            if (!ModelState.IsValid)
            {
                if (newImagePath != existingItem.ImageUrl)
                {
                    DeleteLocalImage(newImagePath);
                }

                item.ImageUrl = existingItem.ImageUrl;
                await LoadCategoriesAsync();
                return View(item);
            }

            item.ImageUrl = newImagePath;

            var updated = await _menuService.UpdateItemAsync(item);

            if (!updated)
            {
                return NotFound();
            }

            if (newImagePath != existingItem.ImageUrl)
            {
                DeleteLocalImage(existingItem.ImageUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _menuService.GetItemByIdAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _menuService.GetItemByIdAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            var deleted = await _menuService.DeleteItemAsync(id);

            if (!deleted)
            {
                TempData["DeleteError"] =
                    "This menu item cannot be deleted because it is already used by an order.";

                return RedirectToAction(nameof(Delete), new { id });
            }

            DeleteLocalImage(item.ImageUrl);

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadCategoriesAsync()
        {
            ViewBag.Categories = await _context.Categories
                .AsNoTracking()
                .Where(c => c.Name != "Other")
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        private async Task<string?> SaveImageAsync(IFormFile imageFile)
        {
            if (imageFile.Length <= 0 || imageFile.Length > MaxImageSize)
            {
                return null;
            }

            var detectedExtension = await DetectImageExtensionAsync(imageFile);

            if (detectedExtension == null)
            {
                return null;
            }

            var imageDirectory = Path.Combine(
                _environment.WebRootPath,
                "images",
                "menu");

            Directory.CreateDirectory(imageDirectory);

            var fileName = $"{Guid.NewGuid():N}{detectedExtension}";
            var filePath = Path.Combine(imageDirectory, fileName);

            await using var stream = new FileStream(
                filePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None);

            await imageFile.CopyToAsync(stream);

            return $"/images/menu/{fileName}";
        }

        private static async Task<string?> DetectImageExtensionAsync(IFormFile imageFile)
        {
            await using var stream = imageFile.OpenReadStream();

            var header = new byte[12];
            var bytesRead = await stream.ReadAsync(header.AsMemory(0, header.Length));

            if (bytesRead >= 3 &&
                header[0] == 0xFF &&
                header[1] == 0xD8 &&
                header[2] == 0xFF)
            {
                return ".jpg";
            }

            if (bytesRead >= 8 &&
                header[0] == 0x89 &&
                header[1] == 0x50 &&
                header[2] == 0x4E &&
                header[3] == 0x47 &&
                header[4] == 0x0D &&
                header[5] == 0x0A &&
                header[6] == 0x1A &&
                header[7] == 0x0A)
            {
                return ".png";
            }

            if (bytesRead >= 12 &&
                header[0] == (byte)'R' &&
                header[1] == (byte)'I' &&
                header[2] == (byte)'F' &&
                header[3] == (byte)'F' &&
                header[8] == (byte)'W' &&
                header[9] == (byte)'E' &&
                header[10] == (byte)'B' &&
                header[11] == (byte)'P')
            {
                return ".webp";
            }

            return null;
        }

        private void DeleteLocalImage(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath) ||
                !imagePath.StartsWith("/images/menu/", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var fileName = Path.GetFileName(imagePath);

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }

            var imageDirectory = Path.Combine(
                _environment.WebRootPath,
                "images",
                "menu");

            var fullPath = Path.Combine(imageDirectory, fileName);

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
    }
}

using GrillMaster.Data;
using GrillMaster.Models;
using GrillMaster.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GrillMaster.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminCategoryController : Controller
    {
        private readonly IMenuService _menuService;
        private readonly AppDbContext _context;

        public AdminCategoryController(IMenuService menuService, AppDbContext context)
        {
            _menuService = menuService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var categories = await _menuService.GetAllCategoriesAsync();
            return View(categories);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            category.Name = category.Name?.Trim() ?? string.Empty;

            if (await _context.Categories.AnyAsync(
                    c => c.Name.ToLower() == category.Name.ToLower()))
            {
                ModelState.AddModelError("Name", "A category with this name already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(category);
            }

            await _menuService.AddCategoryAsync(category);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _menuService.GetCategoryByIdAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Category category)
        {
            category.Name = category.Name?.Trim() ?? string.Empty;

            if (await _context.Categories.AnyAsync(
                    c => c.Id != category.Id &&
                         c.Name.ToLower() == category.Name.ToLower()))
            {
                ModelState.AddModelError("Name", "A category with this name already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(category);
            }

            var updated = await _menuService.UpdateCategoryAsync(category);

            if (!updated)
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _menuService.GetCategoryByIdAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var deleted = await _menuService.DeleteCategoryAsync(id);

            if (!deleted)
            {
                TempData["DeleteError"] =
                    "This category cannot be deleted because it contains menu items or does not exist.";

                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

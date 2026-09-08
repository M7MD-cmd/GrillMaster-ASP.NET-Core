using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.Json;
using GrillMaster.Models;
using GrillMaster.Services;
using Microsoft.AspNetCore.Mvc;

namespace GrillMaster.Controllers
{
    public class OrderController : Controller
    {
        private const string SelectedItemsKey = "SelectedOrderItems";
        private const string PendingOrderKey = "PendingOrder";
        private const int MaxQuantity = 50;

        private static readonly ConcurrentDictionary<string, SemaphoreSlim> CheckoutLocks = new();

        private readonly IOrderService _orderService;
        private readonly IMenuService _menuService;

        public OrderController(
            IOrderService orderService,
            IMenuService menuService)
        {
            _orderService = orderService;
            _menuService = menuService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var selectedItems = GetSelectedItems();

            if (!selectedItems.Any())
            {
                return RedirectToAction("Index", "Menu");
            }

            await PrepareOrderView();

            return View(new OrderFormViewModel());
        }

        // Current Order page used by the navbar.
        [HttpGet]
        public async Task<IActionResult> ViewOrder()
        {
            var selectedItems = GetSelectedItems();

            if (!selectedItems.Any())
            {
                return RedirectToAction("Index", "Menu");
            }

            await PrepareOrderView();

            return View("Index", new OrderFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(OrderFormViewModel formData)
        {
            var selectedItems = GetSelectedItems();

            if (!selectedItems.Any())
            {
                ModelState.AddModelError(
                    "meals",
                    "Please select at least one meal.");

                await PrepareOrderView(formData);

                return View(formData);
            }

            if (!ModelState.IsValid)
            {
                await PrepareOrderView(formData);

                return View(formData);
            }

            // The cart is stored server-side. Never trust meals or quantities from the browser.
            var trustedForm =
                await BuildTrustedOrderFormAsync(
                    formData,
                    selectedItems);

            if (trustedForm == null)
            {
                ModelState.AddModelError(
                    "meals",
                    "One or more selected menu items are no longer available.");

                await PrepareOrderView(formData);

                return View(formData);
            }

            if (User.Identity?.IsAuthenticated != true)
            {
                HttpContext.Session.SetString(
                    PendingOrderKey,
                    JsonSerializer.Serialize(trustedForm));

                return RedirectToAction(
                    "Login",
                    "Account",
                    new
                    {
                        returnUrl = Url.Action(
                            nameof(CompletePendingOrder),
                            "Order")
                    });
            }

            var userId =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var checkoutLock = GetCheckoutLock();

            if (!await checkoutLock.WaitAsync(0))
            {
                return RedirectToAction(
                    nameof(Index),
                    "Order");
            }

            try
            {
                var orderId =
                    await _orderService.CreateOrderAsync(
                        trustedForm,
                        userId);

                HttpContext.Session.Remove(SelectedItemsKey);
                HttpContext.Session.Remove(PendingOrderKey);

                return RedirectToAction(
                    nameof(Confirmation),
                    new { id = orderId });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(
                    "meals",
                    ex.Message);

                await PrepareOrderView(formData);

                return View(formData);
            }
            finally
            {
                checkoutLock.Release();
            }
        }

        [HttpGet]
        public async Task<IActionResult> CompletePendingOrder()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var pendingOrderJson =
                HttpContext.Session.GetString(
                    PendingOrderKey);

            if (string.IsNullOrEmpty(pendingOrderJson))
            {
                return RedirectToAction(
                    "Index",
                    "Menu");
            }

            OrderFormViewModel? pendingForm;

            try
            {
                pendingForm =
                    JsonSerializer.Deserialize<OrderFormViewModel>(
                        pendingOrderJson);
            }
            catch (JsonException)
            {
                pendingForm = null;
            }

            if (pendingForm == null)
            {
                HttpContext.Session.Remove(
                    PendingOrderKey);

                return RedirectToAction(
                    "Index",
                    "Menu");
            }

            var selectedItems = GetSelectedItems();

            if (!selectedItems.Any())
            {
                HttpContext.Session.Remove(
                    PendingOrderKey);

                return RedirectToAction(
                    "Index",
                    "Menu");
            }

            var trustedForm =
                await BuildTrustedOrderFormAsync(
                    pendingForm,
                    selectedItems);

            if (trustedForm == null)
            {
                HttpContext.Session.Remove(
                    PendingOrderKey);

                HttpContext.Session.Remove(
                    SelectedItemsKey);

                return RedirectToAction(
                    "Index",
                    "Menu");
            }

            var userId =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var checkoutLock = GetCheckoutLock();

            if (!await checkoutLock.WaitAsync(0))
            {
                return RedirectToAction(
                    nameof(Index),
                    "Order");
            }

            try
            {
                var orderId =
                    await _orderService.CreateOrderAsync(
                        trustedForm,
                        userId);

                HttpContext.Session.Remove(
                    PendingOrderKey);

                HttpContext.Session.Remove(
                    SelectedItemsKey);

                return RedirectToAction(
                    nameof(Confirmation),
                    new { id = orderId });
            }
            catch (InvalidOperationException)
            {
                HttpContext.Session.Remove(
                    PendingOrderKey);

                return RedirectToAction(
                    "Index",
                    "Menu");
            }
            finally
            {
                checkoutLock.Release();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Confirmation(int id)
        {
            var order =
                await _orderService.GetOrderByIdAsync(id);

            if (order == null)
            {
                return NotFound();
            }

            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction(
                    "Login",
                    "Account",
                    new
                    {
                        returnUrl = Url.Action(
                            nameof(Confirmation),
                            "Order",
                            new { id })
                    });
            }

            var currentUserId =
                User.FindFirst(
                    ClaimTypes.NameIdentifier)?.Value;

            var isAdmin =
                User.IsInRole("Admin");

            if (!isAdmin &&
                (string.IsNullOrEmpty(currentUserId) ||
                 order.UserId != currentUserId))
            {
                return Forbid();
            }

            return View(order);
        }

        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction(
                    "Login",
                    "Account",
                    new
                    {
                        returnUrl = Url.Action(
                            nameof(MyOrders),
                            "Order")
                    });
            }

            var userId =
                User.FindFirst(
                    ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var orders =
                await _orderService.GetUserOrdersAsync(
                    userId);

            return View(orders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(
            int menuItemId,
            int quantity)
        {
            var menuItem =
                await _menuService.GetItemByIdAsync(
                    menuItemId);

            if (menuItem == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Menu item not found."
                });
            }

            var selectedItems =
                GetSelectedItems();

            var item =
                selectedItems.FirstOrDefault(
                    x => x.MenuItemId == menuItemId);

            if (item == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Item not found."
                });
            }

            item.Quantity =
                Math.Clamp(
                    quantity,
                    1,
                    MaxQuantity);

            SaveSelectedItems(
                selectedItems);

            return Json(new
            {
                success = true,
                quantity = item.Quantity,
                totalCount =
                    selectedItems.Sum(
                        x => x.Quantity)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveFromOrder(
            int menuItemId)
        {
            var selectedItems =
                GetSelectedItems();

            var removed =
                selectedItems.RemoveAll(
                    x => x.MenuItemId == menuItemId);

            if (removed == 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Item not found."
                });
            }

            SaveSelectedItems(
                selectedItems);

            return Json(new
            {
                success = true,
                totalCount =
                    selectedItems.Sum(
                        x => x.Quantity)
            });
        }

        private async Task PrepareOrderView(
            OrderFormViewModel? formData = null)
        {
            var selectedItems =
                GetSelectedItems();

            var menuItems =
                await _menuService.GetAllItemsAsync();

            var selectedMenuItems =
                menuItems
                    .Where(item =>
                        selectedItems.Any(
                            x => x.MenuItemId == item.Id))
                    .ToList();

            var selectedQuantities =
                selectedItems
                    .Where(x =>
                        selectedMenuItems.Any(
                            item =>
                                item.Id == x.MenuItemId))
                    .ToDictionary(
                        x => x.MenuItemId,
                        x => Math.Clamp(
                            x.Quantity,
                            1,
                            MaxQuantity));

            ViewBag.SelectedMenuItems =
                selectedMenuItems;

            ViewBag.SelectedQuantities =
                selectedQuantities;
        }

        private async Task<OrderFormViewModel?>
            BuildTrustedOrderFormAsync(
                OrderFormViewModel formData,
                List<SelectedOrderItem> selectedItems)
        {
            var menuItems =
                await _menuService.GetAllItemsAsync();

            var validItems =
                menuItems
                    .Where(item =>
                        selectedItems.Any(
                            x => x.MenuItemId == item.Id))
                    .ToList();

            if (validItems.Count == 0)
            {
                return null;
            }

            var quantities =
                new Dictionary<string, int>(
                    StringComparer.OrdinalIgnoreCase);

            var meals =
                new List<string>();

            foreach (var item in validItems)
            {
                var cartItem =
                    selectedItems.First(
                        x => x.MenuItemId == item.Id);

                var quantity =
                    Math.Clamp(
                        cartItem.Quantity,
                        1,
                        MaxQuantity);

                meals.Add(item.Name);

                quantities[item.Name] =
                    quantity;
            }

            return new OrderFormViewModel
            {
                fullname =
                    formData.fullname.Trim(),

                phone =
                    formData.phone.Trim(),

                address =
                    formData.address.Trim(),

                meals = meals,

                quantities = quantities
            };
        }

        private SemaphoreSlim GetCheckoutLock()
        {
            return CheckoutLocks.GetOrAdd(
                HttpContext.Session.Id,
                _ => new SemaphoreSlim(1, 1));
        }

        private List<SelectedOrderItem>
            GetSelectedItems()
        {
            var json =
                HttpContext.Session.GetString(
                    SelectedItemsKey);

            if (string.IsNullOrEmpty(json))
            {
                return new List<SelectedOrderItem>();
            }

            try
            {
                var items =
                    JsonSerializer.Deserialize<
                        List<SelectedOrderItem>>(
                            json)
                    ?? new List<SelectedOrderItem>();

                return items
                    .Where(x =>
                        x.MenuItemId > 0)
                    .Select(x =>
                        new SelectedOrderItem
                        {
                            MenuItemId =
                                x.MenuItemId,

                            Quantity =
                                Math.Clamp(
                                    x.Quantity,
                                    1,
                                    MaxQuantity)
                        })
                    .GroupBy(
                        x => x.MenuItemId)
                    .Select(
                        g => new SelectedOrderItem
                        {
                            MenuItemId =
                                g.Key,

                            Quantity =
                                g.First().Quantity
                        })
                    .ToList();
            }
            catch
            {
                return new List<SelectedOrderItem>();
            }
        }

        private void SaveSelectedItems(
            List<SelectedOrderItem> selectedItems)
        {
            HttpContext.Session.SetString(
                SelectedItemsKey,
                JsonSerializer.Serialize(
                    selectedItems));
        }

        public class SelectedOrderItem
        {
            public int MenuItemId { get; set; }

            public int Quantity { get; set; }
        }
    }
}
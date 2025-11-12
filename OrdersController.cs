using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BawolShopApi.Data;
using BawolShopApi.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json;

namespace BawolShopApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Toutes les routes protégées
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/orders - Commandes de l'utilisateur connecté
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderResponse>>> GetUserOrders()
        {
            var userName = User.Identity.Name;

            var orders = await _context.Orders
                .Where(o => o.UserId == userName)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // ✅ CORRECTION : Utiliser le DTO pour éviter les cycles
            var orderResponses = orders.Select(order => new OrderResponse
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                PaymentStatus = order.PaymentStatus,
                CustomerFullName = order.CustomerFullName,
                CustomerPhone = order.CustomerPhone,
                CustomerAddress = order.CustomerAddress,
                OrderDate = order.OrderDate,
                Items = order.OrderItems.Select(oi => new OrderItemResponse
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.ProductName,
                    UnitPrice = oi.UnitPrice,
                    Quantity = oi.Quantity,
                    TotalPrice = oi.UnitPrice * oi.Quantity
                }).ToList()
            }).ToList();

            return orderResponses;
        }

        // GET: api/orders/5 - Détail d'une commande
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderResponse>> GetOrder(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
             User.FindFirstValue(ClaimTypes.Name) ??
             User.FindFirstValue("UserId");

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

            // ✅ CORRECTION : Utiliser le DTO
            var orderResponse = new OrderResponse
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                PaymentStatus = order.PaymentStatus,
                CustomerFullName = order.CustomerFullName,
                CustomerPhone = order.CustomerPhone,
                CustomerAddress = order.CustomerAddress,
                OrderDate = order.OrderDate,
                Items = order.OrderItems.Select(oi => new OrderItemResponse
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.ProductName,
                    UnitPrice = oi.UnitPrice,
                    Quantity = oi.Quantity,
                    TotalPrice = oi.UnitPrice * oi.Quantity
                }).ToList()
            };

            return orderResponse;
        }

        // GET: api/orders/admin - Toutes les commandes (Admin seulement)
        [Authorize(Roles = "Admin")]
        [HttpGet("admin")]
        public async Task<ActionResult<IEnumerable<OrderResponse>>> GetAllOrders()
        {
            try
            {
                var orders = await _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .Include(o => o.User)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                // ✅ CORRECTION : Utiliser le DTO pour éviter les cycles
                var orderResponses = orders.Select(order => new OrderResponse
                {
                    Id = order.Id,
                    OrderNumber = order.OrderNumber,
                    TotalAmount = order.TotalAmount,
                    Status = order.Status,
                    PaymentStatus = order.PaymentStatus,
                    CustomerFullName = order.CustomerFullName,
                    CustomerPhone = order.CustomerPhone,
                    CustomerAddress = order.CustomerAddress,
                    OrderDate = order.OrderDate,
                    Items = order.OrderItems.Select(oi => new OrderItemResponse
                    {
                        ProductId = oi.ProductId,
                        ProductName = oi.ProductName,
                        UnitPrice = oi.UnitPrice,
                        Quantity = oi.Quantity,
                        TotalPrice = oi.UnitPrice * oi.Quantity
                    }).ToList()
                }).ToList();

                return orderResponses;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔴 ERREUR dans GetAllOrders: {ex.Message}");
                Console.WriteLine($"🔴 StackTrace: {ex.StackTrace}");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        // POST: api/orders - Créer une commande
        [HttpPost]
        public async Task<ActionResult<OrderResponse>> CreateOrder([FromBody] CreateOrderModel model)
        {
            try
            {
                Console.WriteLine($"🔍 CreateOrder appelé - User: {User.Identity.Name}");
                Console.WriteLine($"🔍 Model reçu: {JsonSerializer.Serialize(model)}");

                var userName = User.Identity.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);

                Console.WriteLine($"🔍 UserName: {userName}");
                Console.WriteLine($"🔍 User trouvé: {user != null}");

                if (user == null)
                {
                    Console.WriteLine("🔴 User non trouvé en base!");
                    return Unauthorized();
                }

                // Générer un numéro de commande unique
                var orderNumber = GenerateOrderNumber();
                Console.WriteLine($"🔍 OrderNumber généré: {orderNumber}");

                // Calculer le total et vérifier le stock
                decimal totalAmount = 0;
                var orderItems = new List<OrderItem>();

                Console.WriteLine($"🔍 Vérification de {model.Items.Count} produits...");

                foreach (var item in model.Items)
                {
                    Console.WriteLine($"🔍 Traitement produit {item.ProductId}, quantité {item.Quantity}");

                    var product = await _context.Products.FindAsync(item.ProductId);
                    Console.WriteLine($"🔍 Produit trouvé: {product != null}");

                    if (product == null || !product.IsActive)
                    {
                        Console.WriteLine($"🔴 Produit {item.ProductId} non disponible");
                        return BadRequest(new { message = $"Produit {item.ProductId} non disponible" });
                    }

                    if (product.Stock < item.Quantity)
                    {
                        Console.WriteLine($"🔴 Stock insuffisant: {product.Stock} < {item.Quantity}");
                        return BadRequest(new { message = $"Stock insuffisant pour {product.Name}" });
                    }

                    // Mettre à jour le stock
                    product.Stock -= item.Quantity;
                    product.UpdatedAt = DateTime.UtcNow;

                    var orderItem = new OrderItem
                    {
                        ProductId = item.ProductId,
                        ProductName = product.Name,
                        UnitPrice = product.Price,
                        Quantity = item.Quantity
                    };

                    orderItems.Add(orderItem);
                    totalAmount += product.Price * item.Quantity;

                    Console.WriteLine($"🔍 Produit {product.Name} ajouté - Prix: {product.Price}");
                }

                Console.WriteLine($"🔍 Total calculé: {totalAmount}");

                // Créer la commande
                var order = new Order
                {
                    OrderNumber = orderNumber,
                    UserId = user.Id,
                    TotalAmount = totalAmount,
                    Status = "En attente",
                    PaymentStatus = "En attente",
                    CustomerFullName = $"{user.FirstName} {user.LastName}",
                    CustomerPhone = user.FullPhoneNumber,
                    CustomerAddress = model.ShippingAddress,
                    OrderDate = DateTime.UtcNow,
                    OrderItems = orderItems
                };

                Console.WriteLine($"🔍 Commande créée - Ajout au contexte...");
                _context.Orders.Add(order);

                Console.WriteLine($"🔍 Sauvegarde en base...");
                await _context.SaveChangesAsync();

                Console.WriteLine($"✅ Commande créée avec succès - ID: {order.Id}");

                // ✅ CORRECTION : Retourner un DTO au lieu de l'entité complète
                var response = new OrderResponse
                {
                    Id = order.Id,
                    OrderNumber = order.OrderNumber,
                    TotalAmount = order.TotalAmount,
                    Status = order.Status,
                    PaymentStatus = order.PaymentStatus,
                    CustomerFullName = order.CustomerFullName,
                    CustomerPhone = order.CustomerPhone,
                    CustomerAddress = order.CustomerAddress,
                    OrderDate = order.OrderDate,
                    Items = order.OrderItems.Select(oi => new OrderItemResponse
                    {
                        ProductId = oi.ProductId,
                        ProductName = oi.ProductName,
                        UnitPrice = oi.UnitPrice,
                        Quantity = oi.Quantity,
                        TotalPrice = oi.UnitPrice * oi.Quantity
                    }).ToList()
                };

                return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔴 ERREUR dans CreateOrder: {ex.Message}");
                Console.WriteLine($"🔴 StackTrace: {ex.StackTrace}");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        // PUT: api/orders/5/status - Mettre à jour le statut (Admin seulement)
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusModel model)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = model.Status;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/orders/5/payment-status - Mettre à jour le statut de paiement
        [HttpPut("{id}/payment-status")]
        public async Task<IActionResult> UpdatePaymentStatus(int id, [FromBody] UpdatePaymentStatusModel model)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.PaymentStatus = model.PaymentStatus;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        private string GenerateOrderNumber()
        {
            return $"BAWOL-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        }
    }

    public class CreateOrderModel
    {
        public List<OrderItemModel> Items { get; set; } = new List<OrderItemModel>();
        public string ShippingAddress { get; set; } = string.Empty;
    }

    public class OrderItemModel
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class UpdateOrderStatusModel
    {
        public string Status { get; set; } = string.Empty;
    }

    public class UpdatePaymentStatusModel
    {
        public string PaymentStatus { get; set; } = string.Empty;
    }

    // ✅ DTOs pour éviter les cycles de référence
    public class OrderResponse
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string CustomerFullName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public List<OrderItemResponse> Items { get; set; } = new List<OrderItemResponse>();
    }

    public class OrderItemResponse
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
    }
}

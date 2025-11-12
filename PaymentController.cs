using BawolShopApi.Data;
using BawolShopApi.Models;
using BawolShopApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BawolShopApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IFusionPayService _fusionPayService;
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public PaymentController(IFusionPayService fusionPayService, AppDbContext context, IConfiguration configuration)
        {
            _fusionPayService = fusionPayService;
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("initiate/{orderId}")]
        public async Task<IActionResult> InitiatePayment(int orderId)
        {
            var userName = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);

            if (user == null)
            {
                return Unauthorized();
            }

            // Récupérer la commande
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == user.Id);

            if (order == null)
            {
                return NotFound(new { message = "Commande non trouvée" });
            }

            if (order.PaymentStatus == "Payée")
            {
                return BadRequest(new { message = "Cette commande a déjà été payée" });
            }

            try
            {
                // Préparer les articles pour FusionPay
                var articles = order.OrderItems.Select(oi => new Article
                {
                    nom = oi.ProductName,
                    montant = oi.UnitPrice * oi.Quantity
                }).ToList();

                // Préparer la requête FusionPay selon leur format exact
                var fusionPayRequest = new FusionPayRequest
                {
                    totalPrice = order.TotalAmount,
                    article = articles,
                    personal_Info = new List<PersonalInfo>
                    {
                        new PersonalInfo
                        {
                            userId = user.Id,
                            orderId = order.Id.ToString()
                        }
                    },
                    numeroSend = user.PhoneNumber,
                    nomclient = $"{user.FirstName} {user.LastName}",
                    return_url = "https://senbib.duckdns.org/merci", // Votre URL de retour
                    webhook_url = $"{_configuration["BaseUrl"]}/api/payment/webhook"
                };

                // Initier le paiement
                var fusionPayResponse = await _fusionPayService.InitiatePaymentAsync(fusionPayRequest);

                if (fusionPayResponse.statut)
                {
                    // Mettre à jour la commande avec le token de paiement
                    order.PaymentStatus = "En attente de paiement";
                    order.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        success = true,
                        paymentUrl = fusionPayResponse.url,
                        message = "Paiement initié avec succès",
                        token = fusionPayResponse.token
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = fusionPayResponse.message
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Erreur lors de l'initiation du paiement",
                    error = ex.Message
                });
            }
        }

        [AllowAnonymous]
        [HttpPost("webhook")]
        public async Task<IActionResult> HandleWebhook([FromBody] FusionPayWebhook webhookData)
        {
            try
            {
                Console.WriteLine($"[FusionPay] Webhook reçu – Token: {webhookData.tokenPay}, Statut: {webhookData.statut}");

                if (webhookData.personal_Info == null || webhookData.personal_Info.Length == 0)
                {
                    return BadRequest(new { Message = "Données invalides." });
                }

                var orderId = webhookData.personal_Info[0].orderId;

                if (!int.TryParse(orderId, out var orderIdInt))
                {
                    return BadRequest(new { Message = "OrderId invalide." });
                }

                var order = await _context.Orders.FindAsync(orderIdInt);

                if (order == null)
                {
                    return NotFound(new { Message = "Commande non trouvée." });
                }

                // Vérifier si déjà traité
                if (order.PaymentStatus == "Payée")
                {
                    return Ok(new { Message = "Déjà traité" });
                }

                if (webhookData.statut == "paid")
                {
                    // Paiement réussi
                    order.PaymentStatus = "Payée";
                    order.Status = "Confirmée";
                    order.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    // TODO: Envoyer un email de confirmation
                    // TODO: Mettre à jour les stocks

                    return Ok(new { Message = "Paiement confirmé avec succès." });
                }
                else
                {
                    // Paiement échoué
                    order.PaymentStatus = "Échouée";
                    order.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    return BadRequest(new { Message = "Paiement non accepté", Status = webhookData.statut });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FusionPay] Erreur webhook: {ex.Message}");
                return StatusCode(500, new { Message = "Erreur interne du serveur" });
            }
        }

        [HttpGet("success")]
        public IActionResult PaymentSuccess([FromQuery] int orderId)
        {
            return Ok(new { message = "Paiement effectué avec succès!", orderId });
        }
    }
}

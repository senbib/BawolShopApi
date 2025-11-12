using System.Text;
using System.Text.Json;
using BawolShopApi.Models;

namespace BawolShopApi.Services
{
    public interface IFusionPayService
    {
        Task<FusionPayResponse> InitiatePaymentAsync(FusionPayRequest request);
    }

    public class FusionPayService : IFusionPayService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FusionPayService> _logger;

        public FusionPayService(HttpClient httpClient, IConfiguration configuration, ILogger<FusionPayService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            // Configuration de l'URL FusionPay depuis appsettings.json
            var fusionPayUrl = _configuration["FusionPay:ApiUrl"] ?? "https://www.pay.moneyfusion.net/Senbib/c2af5ab0ca89c711/pay/";
            _httpClient.BaseAddress = new Uri(fusionPayUrl);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public async Task<FusionPayResponse> InitiatePaymentAsync(FusionPayRequest request)
        {
            try
            {
                // CORRECTION : Utiliser personal_Info au lieu de PersonalInfo
                var firstPersonalInfo = request.personal_Info.FirstOrDefault();
                _logger.LogInformation("🔄 Initiation du paiement FusionPay pour la commande {OrderId}",
                    firstPersonalInfo?.orderId);

                // Sérialiser la requête avec les noms exacts attendus par FusionPay
                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("📤 Envoi requête FusionPay: {Url}", _httpClient.BaseAddress);
                _logger.LogInformation("📦 Données envoyées: {Data}", json);

                // Appeler l'API FusionPay
                var response = await _httpClient.PostAsync("", content);

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("📥 Réponse FusionPay: {StatusCode} - {Content}", response.StatusCode, responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("❌ Erreur FusionPay: {StatusCode} - {Content}", response.StatusCode, responseContent);
                    throw new HttpRequestException($"Erreur FusionPay: {response.StatusCode} - {responseContent}");
                }

                var fusionPayResponse = JsonSerializer.Deserialize<FusionPayResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (fusionPayResponse == null)
                {
                    throw new InvalidOperationException("Réponse FusionPay invalide");
                }

                // CORRECTION : Utiliser url au lieu de Url
                _logger.LogInformation("✅ Paiement FusionPay initié avec succès. URL: {Url}", fusionPayResponse.url);
                return fusionPayResponse;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de l'initiation du paiement FusionPay");
                throw;
            }
        }
    }
}

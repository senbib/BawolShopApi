using System.Text.Json.Serialization;

namespace BawolShopApi.Models
{
    public class FusionPayRequest
    {
        [JsonPropertyName("totalPrice")]
        public decimal totalPrice { get; set; }

        [JsonPropertyName("article")]
        public List<Article> article { get; set; } = new List<Article>();

        [JsonPropertyName("personal_Info")]
        public List<PersonalInfo> personal_Info { get; set; } = new List<PersonalInfo>();

        [JsonPropertyName("numeroSend")]
        public string? numeroSend { get; set; }

        [JsonPropertyName("nomclient")]
        public string? nomclient { get; set; }

        [JsonPropertyName("return_url")]
        public string return_url { get; set; } = string.Empty;

        [JsonPropertyName("webhook_url")]
        public string webhook_url { get; set; } = string.Empty;
    }

    public class Article
    {
        [JsonPropertyName("nom")]
        public string nom { get; set; } = string.Empty;

        [JsonPropertyName("montant")]
        public decimal montant { get; set; }
    }

    public class PersonalInfo
    {
        [JsonPropertyName("userId")]
        public string userId { get; set; } = string.Empty;

        [JsonPropertyName("orderId")]
        public string orderId { get; set; } = string.Empty;
    }

    public class FusionPayResponse
    {
        [JsonPropertyName("statut")]
        public bool statut { get; set; }

        [JsonPropertyName("token")]
        public string token { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string message { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string url { get; set; } = string.Empty;
    }

    public class FusionPayWebhook
    {
        [JsonPropertyName("tokenPay")]
        public string tokenPay { get; set; } = string.Empty;

        [JsonPropertyName("statut")]
        public string statut { get; set; } = string.Empty;

        [JsonPropertyName("personal_Info")]
        public PersonalInfo[] personal_Info { get; set; } = Array.Empty<PersonalInfo>();

        [JsonPropertyName("numeroSend")]
        public string numeroSend { get; set; } = string.Empty;

        [JsonPropertyName("nomclient")]
        public string nomclient { get; set; } = string.Empty;

        [JsonPropertyName("numeroTransaction")]
        public string numeroTransaction { get; set; } = string.Empty;

        [JsonPropertyName("Montant")]
        public decimal Montant { get; set; }

        [JsonPropertyName("frais")]
        public decimal frais { get; set; }

        [JsonPropertyName("moyen")]
        public string moyen { get; set; } = string.Empty;
    }
}

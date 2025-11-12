using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BawolShopApi.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Role { get; set; } // Client ou Admin

        // ✅ REMPLACER Email par Phone comme identifiant principal
        [Required]
        [Phone]
        public override string? PhoneNumber { get; set; }

        // ✅ On garde Email mais optionnel
        public override string? Email { get; set; }

        // ✅ Champ pour l'indicatif pays
        public string CountryCode { get; set; } = "+221";

        // ✅ Propriété calculée pour le numéro complet
        [NotMapped] // Ne pas stocker en base
        public string FullPhoneNumber => $"{CountryCode}{PhoneNumber}";
    }
}

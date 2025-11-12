using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BawolShopApi.Models;

namespace BawolShopApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            // Nettoyer le numéro de téléphone
            var cleanedPhoneNumber = CleanPhoneNumber(model.PhoneNumber);

            // Vérifier si le numéro existe déjà
            var existingUser = await _userManager.FindByNameAsync(cleanedPhoneNumber);
            if (existingUser != null)
            {
                return BadRequest(new { message = "Ce numéro de téléphone est déjà utilisé." });
            }

            var user = new ApplicationUser
            {
                UserName = cleanedPhoneNumber,
                PhoneNumber = cleanedPhoneNumber,
                CountryCode = model.CountryCode,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                Role = model.Role ?? "Client"
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Ajouter l'utilisateur au rôle
                if (!string.IsNullOrEmpty(user.Role))
                {
                    await _userManager.AddToRoleAsync(user, user.Role);
                }

                // ✅ GÉNÉRER LE TOKEN JWT IMMÉDIATEMENT
                var token = GenerateJwtToken(user);

                // ✅ RETOURNER LA MÊME RÉPONSE QUE LE LOGIN
                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    expiration = token.ValidTo,
                    user = new
                    {
                        user.Id,
                        user.FullPhoneNumber,
                        user.FirstName,
                        user.LastName,
                        user.Role,
                        user.CountryCode
                    },
                    message = "Compte créé et connecté avec succès"
                });
            }

            return BadRequest(new { errors = result.Errors });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            // Nettoyer le numéro de téléphone
            var cleanedPhoneNumber = CleanPhoneNumber(model.PhoneNumber);

            // Trouver l'utilisateur par le numéro de téléphone
            var user = await _userManager.FindByNameAsync(cleanedPhoneNumber);
            if (user == null)
            {
                return Unauthorized(new { message = "Numéro de téléphone ou mot de passe incorrect." });
            }

            // Vérifier le mot de passe
            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);

            if (result.Succeeded)
            {
                var token = GenerateJwtToken(user);

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    expiration = token.ValidTo,
                    user = new
                    {
                        user.Id,
                        user.FullPhoneNumber,
                        user.FirstName,
                        user.LastName,
                        user.Role,
                        user.CountryCode
                    }
                });
            }

            return Unauthorized(new { message = "Numéro de téléphone ou mot de passe incorrect." });
        }

        // Méthode pour nettoyer le numéro de téléphone
        private string CleanPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return phoneNumber;

            // Supprimer les espaces, tirets, etc.
            var cleaned = new string(phoneNumber.Where(char.IsDigit).ToArray());

            // Si le numéro commence par l'indicatif Sénégal, le retirer
            if (cleaned.StartsWith("221"))
            {
                cleaned = cleaned.Substring(3);
            }

            // S'assurer que c'est un numéro à 9 chiffres
            if (cleaned.Length == 9)
            {
                return cleaned;
            }

            return phoneNumber;
        }

        private JwtSecurityToken GenerateJwtToken(ApplicationUser user)
        {
            // ✅ CORRECTION : Utiliser UserName comme NameIdentifier (comme ASP.NET Identity l'attend)
            var claims = new[]
            {
        new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(ClaimTypes.NameIdentifier, user.UserName), // ← CHANGEMENT CRITIQUE ICI
        new Claim(ClaimTypes.Role, user.Role ?? "Client"),
        new Claim("PhoneNumber", user.FullPhoneNumber),
        new Claim("UserId", user.Id) // ← Ajouter UserId séparément si besoin
    };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            return new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(3),
                signingCredentials: creds);
        }
    }

    public class RegisterModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string CountryCode { get; set; } = "+221";
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Password { get; set; } = string.Empty;
        public string? Role { get; set; }
    }

    public class LoginModel
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}

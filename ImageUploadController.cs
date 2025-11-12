using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using BawolShopApi.Data;
using BawolShopApi.Models;

namespace BawolShopApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Seuls les utilisateurs connectés peuvent uploader
    public class ImageUploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly AppDbContext _context;

        public ImageUploadController(IWebHostEnvironment environment, AppDbContext context)
        {
            _environment = environment;
            _context = context;
        }

        // POST: api/imageupload/product
        [HttpPost("product")]
        [Authorize(Roles = "Admin")] // Seuls les admins peuvent uploader des images produits
        public async Task<ActionResult<ImageUploadResponse>> UploadProductImage(IFormFile file)
        {
            try
            {
                Console.WriteLine("🔼 Début de l'upload d'image...");

                // 1. Vérifier si un fichier a été envoyé
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "Aucun fichier sélectionné" });
                }

                // 2. Vérifier la taille du fichier (max 5MB)
                if (file.Length > 5 * 1024 * 1024) // 5MB en bytes
                {
                    return BadRequest(new { message = "L'image ne doit pas dépasser 5MB" });
                }

                // 3. Vérifier le type de fichier
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new { message = "Format d'image non supporté. Utilisez JPG, PNG, GIF ou WebP" });
                }

                // 4. Créer le dossier uploads s'il n'existe pas
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "products");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                    Console.WriteLine($"✅ Dossier créé: {uploadsFolder}");
                }

                // 5. Générer un nom de fichier unique
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                Console.WriteLine($"📁 Sauvegarde vers: {filePath}");

                // 6. Sauvegarder le fichier sur le serveur
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // 7. Générer l'URL accessible depuis le web
                var imageUrl = $"/uploads/products/{fileName}";

                Console.WriteLine($"✅ Image uploadée avec succès: {imageUrl}");

                return Ok(new ImageUploadResponse
                {
                    Success = true,
                    ImageUrl = imageUrl,
                    Message = "Image uploadée avec succès"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur lors de l'upload: {ex.Message}");
                Console.WriteLine($"❌ StackTrace: {ex.StackTrace}");

                return StatusCode(500, new ImageUploadResponse
                {
                    Success = false,
                    Message = "Erreur lors de l'upload de l'image"
                });
            }
        }

        // DELETE: api/imageupload/product
        [HttpDelete("product")]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteProductImage([FromBody] DeleteImageRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ImageUrl))
                {
                    return BadRequest(new { message = "URL d'image manquante" });
                }

                // Extraire le nom de fichier de l'URL
                var fileName = Path.GetFileName(request.ImageUrl);
                var filePath = Path.Combine(_environment.WebRootPath, "uploads", "products", fileName);

                Console.WriteLine($"🗑️ Tentative de suppression: {filePath}");

                // Vérifier que le fichier existe et est dans le dossier uploads (sécurité)
                if (System.IO.File.Exists(filePath) && filePath.StartsWith(Path.Combine(_environment.WebRootPath, "uploads")))
                {
                    System.IO.File.Delete(filePath);
                    Console.WriteLine($"✅ Image supprimée: {fileName}");
                    return Ok(new { message = "Image supprimée avec succès" });
                }

                Console.WriteLine($"⚠️ Image non trouvée: {fileName}");
                return NotFound(new { message = "Image non trouvée" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur lors de la suppression: {ex.Message}");
                return StatusCode(500, new { message = "Erreur lors de la suppression de l'image" });
            }
        }

        // GET: api/imageupload/images - Récupérer la liste des images uploadées
        [HttpGet("images")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetUploadedImages()
        {
            try
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "products");

                if (!Directory.Exists(uploadsFolder))
                {
                    return Ok(new List<GalleryImage>());
                }

                var images = Directory.GetFiles(uploadsFolder)
                    .Where(file =>
                        file.ToLower().EndsWith(".jpg") ||
                        file.ToLower().EndsWith(".jpeg") ||
                        file.ToLower().EndsWith(".png") ||
                        file.ToLower().EndsWith(".gif") ||
                        file.ToLower().EndsWith(".webp"))
                    .Select(file => new GalleryImage
                    {
                        Name = Path.GetFileName(file),
                        Url = $"/uploads/products/{Path.GetFileName(file)}",
                        UploadDate = System.IO.File.GetLastWriteTime(file)
                    })
                    .OrderByDescending(img => img.UploadDate)
                    .ToList();

                return Ok(images);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur récupération images: {ex.Message}");
                return StatusCode(500, new { message = "Erreur lors de la récupération des images" });
            }
        }
    }

    // Classes pour structurer les réponses
    public class ImageUploadResponse
    {
        public bool Success { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class DeleteImageRequest
    {
        public string ImageUrl { get; set; } = string.Empty;
    }

    public class GalleryImage
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; }
    }
}

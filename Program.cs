using BawolShopApi.Data;
using BawolShopApi.Models;
using BawolShopApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuration SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Configuration Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 4;
    options.User.RequireUniqueEmail = false;
    options.User.AllowedUserNameCharacters = "0123456789";
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// 3. Services
builder.Services.AddHttpClient<IFusionPayService, FusionPayService>();
builder.Services.AddScoped<IFusionPayService, FusionPayService>();

// 4. Configuration JWT
var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!);
var issuer = builder.Configuration["Jwt:Issuer"];
var audience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        NameClaimType = ClaimTypes.NameIdentifier,
        RoleClaimType = ClaimTypes.Role
    };
});

// 5. Contrôleurs + CORS
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 6. Swagger (seulement en développement)
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Bawol Shop API",
        Version = "v1",
        Description = "API e-commerce Bawol Shop"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// 7. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .SetPreflightMaxAge(TimeSpan.FromHours(1));
    });
});

var app = builder.Build();

// ✅ CONFIGURATION PRODUCTION/DEVELOPPEMENT
if (app.Environment.IsDevelopment())
{
    // 🛠 MODE DÉVELOPPEMENT - On active tout
    Console.WriteLine("🚀 Mode Développement Activé");
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BawolShop API V1");
        c.RoutePrefix = "swagger";
    });
}
else
{
    // 🏭 MODE PRODUCTION - On désactive Swagger pour la sécurité
    Console.WriteLine("🏭 Mode Production Activé");

    // ❌ Swagger DÉSACTIVÉ en production
    // (on laisse les lignes commentées = pas de Swagger)
}

app.UseHttpsRedirection();

// ✅ SERVIR LES FICHIERS STATIQUES
app.UseDefaultFiles();
app.UseStaticFiles();

// ✅ SERVIR LE DOSSIER UPLOADS
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads")),
    RequestPath = "/uploads"
});

// ✅ FORCER app.html COMME PAGE PAR DÉFAUT EN PRODUCTION
// ✅ REDIRECTION POUR SOUS-DOSSIER /bawol/
if (!app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        // Si on accède à /bawol/, rediriger vers /bawol/app.html
        if (context.Request.Path == "/bawol/" || context.Request.Path == "/bawol")
        {
            context.Response.Redirect("/bawol/app.html");
            return;
        }
        await next();
    });
}


// ✅ MIDDLEWARE DANS LE BON ORDRE
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ✅ INITIALISATION DES DONNÉES
using (var scope = app.Services.CreateScope())
{
    await SeedData.Initialize(scope.ServiceProvider);
}

Console.WriteLine("✅ BawolShop API Démarrée avec Succès!");
app.Run();

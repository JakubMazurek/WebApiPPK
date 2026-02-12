using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using System.Text;
using WebApiPPK.Data;
using WebApiPPK.Models;
using WebApiPPK.Services;

var builder = WebApplication.CreateBuilder(args);

// --------------------
// 1) EF Core + baza
// --------------------
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// --------------------
// 2) Identity (lokalne konta w DB)
// --------------------
builder.Services
    .AddIdentityCore<ApplicationUser>(opt =>
    {
        // Minimalna konfiguracja haseł pod projekt (możesz zaostrzyć)
        opt.User.RequireUniqueEmail = true;
        opt.Password.RequiredLength = 6;
        opt.Password.RequireNonAlphanumeric = false;
        opt.Password.RequireUppercase = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// Serwis do tworzenia JWT
builder.Services.AddScoped<TokenService>();

// --------------------
// 3) JWT Bearer Auth
// --------------------
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"]!;
var jwtIssuer = jwtSection["Issuer"]!;
var jwtAudience = jwtSection["Audience"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

            // Mały margines na różnice w zegarach
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// Kontrolery (REST)
builder.Services.AddControllers();

// --------------------
// 4) Swagger + JWT w UI
// --------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "WebApiPPK - Project & Task Management API",
        Version = "v1",
        Description = "REST API dla zarządzania projektami i zadaniami z autoryzacją JWT"
    });

    // Definicja schematu bezpieczeństwa Bearer
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Autoryzacja JWT przy użyciu schematu Bearer. Wpisz 'Bearer' [spacja] i token w polu poniżej.\n\nPrzykład: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    // Wymaganie globalne - wszystkie endpointy wymagają tokena JWT
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

// Middleware dla Swaggera - zawsze dostępny w dev i prod
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "WebApiPPK API v1");
    c.RoutePrefix = "swagger"; // Dostępny pod /swagger
});

app.UseHttpsRedirection();

// Kolejność ma znaczenie: najpierw auth, potem authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
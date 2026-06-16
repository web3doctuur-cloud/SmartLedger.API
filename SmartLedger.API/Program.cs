using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SmartLedger.API.Data;
using SmartLedger.API.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

// Determine environment
var isDevelopment = builder.Environment.IsDevelopment();
var isProduction = builder.Environment.IsProduction();

// Configure Kestrel to use PORT environment variable for Render
var port = Environment.GetEnvironmentVariable("PORT") ?? "5173";
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(int.Parse(port));
});

// CORS CONFIGURATION
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[]
{
    "http://localhost:3000",
    "http://localhost:5173",
    "https://localhost:5173",
    "https://smartledger.vercel.app",
    "https://smartledger-frontend.vercel.app"
};

builder.Services.AddCors(options =>
{
    options.AddPolicy("VercelFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// DATABASE (PostgreSQL)

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string not found");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(3);
        npgsqlOptions.CommandTimeout(30);
    }));

// ============================================================
// IDENTITY & AUTHENTICATION
// ============================================================

builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// JWT Settings - MUST come from environment variables in production
var jwtSecret = builder.Configuration["JwtSettings:Secret"];
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"];
var jwtAudience = builder.Configuration["JwtSettings:Audience"];
var jwtExpiryMinutes = int.TryParse(builder.Configuration["JwtSettings:ExpiryMinutes"], out var expiry) ? expiry : 60;

// Validate JWT secret in production
if (isProduction && (string.IsNullOrEmpty(jwtSecret) || jwtSecret.Length < 32))
{
    throw new InvalidOperationException("JWT Secret must be at least 32 characters in production. Set JwtSettings__Secret environment variable.");
}

var key = Encoding.ASCII.GetBytes(jwtSecret ?? "DevSecretKeyForLocalOnly1234567890123456");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = isProduction;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = !string.IsNullOrEmpty(jwtIssuer),
        ValidIssuer = jwtIssuer,
        ValidateAudience = !string.IsNullOrEmpty(jwtAudience),
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// ============================================================
// SERVICES
// ============================================================

builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<ILedgerService, LedgerService>();

// ============================================================
// API & SWAGGER
// ============================================================

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ============================================================
// MIDDLEWARE PIPELINE
// ============================================================

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartLedger API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseCors("VercelFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    environment = builder.Environment.EnvironmentName,
    timestamp = DateTime.UtcNow
}));

// Simple public test endpoint
app.MapGet("/test", () => "Hello from SmartLedger API! This is a public endpoint!");


// DATABASE MIGRATIONS & ROLE SEEDING

if (isProduction)
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            // Run migrations
            var dbContext = services.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.MigrateAsync();
            Console.WriteLine("Database migrations applied successfully.");

            // Seed roles - ONLY "User" role (no Admin)
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

            // Only create the "User" role
            string roleName = "User";
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
                Console.WriteLine($"Created role: {roleName}");
            }

            Console.WriteLine("Role seeding completed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error applying migrations or seeding roles: {ex.Message}");
        }
    }
}
else // Development environment
{
    // In development, we don't auto-run migrations - let EF Core handle it
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var services = scope.ServiceProvider;
            var dbContext = services.GetRequiredService<ApplicationDbContext>();

            // Just check if tables exist, create if needed
            await dbContext.Database.EnsureCreatedAsync();
            Console.WriteLine("Development database ensured.");

            // Still need roles for local testing
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

            string roleName = "User";
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
                Console.WriteLine($"Created role: {roleName}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in development seeding: {ex.Message}");
        }
    }
}

app.Run();
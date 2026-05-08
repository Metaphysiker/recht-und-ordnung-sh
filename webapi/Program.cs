using System.Text;
using Amazon.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using webapi.Data;
using webapi.Models;
using webapi.Models.ModelsImpl;
using webapi.Services;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddScoped<IEmailService, EmailService>();

// Add AWS S3
var awsRegion = builder.Configuration["AWS_REGION"] ?? builder.Configuration["AWS_DEFAULT_REGION"] ?? "eu-central-1";
builder.Services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(
    new AmazonS3Config { RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(awsRegion) }
));
builder.Services.AddSingleton<IS3Service, S3Service>();
builder.Services.AddScoped<webapi.Pdf.ProblemPdfGenerator>();

// Add PostgreSQL DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;

    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Add JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");

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
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Apply migrations automatically with retry logic for database startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    var retryCount = 0;
    var maxRetries = 10;
    var delaySeconds = 3;

    while (retryCount < maxRetries)
    {
        try
        {
            logger.LogInformation("Applying database migrations... (Attempt {Attempt}/{MaxRetries})", retryCount + 1, maxRetries);
            db.Database.Migrate();
            logger.LogInformation("Database migrations applied successfully");
            break;
        }
        catch (Exception ex) when (retryCount < maxRetries - 1)
        {
            retryCount++;
            logger.LogWarning(ex, "Failed to connect to database. Retrying in {Delay} seconds... (Attempt {Attempt}/{MaxRetries})", delaySeconds, retryCount + 1, maxRetries);
            Thread.Sleep(delaySeconds * 1000);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating the database after {MaxRetries} attempts.", maxRetries);
            throw;
        }
    }

    // Seed admin user
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    await DatabaseSeeder.SeedAdminUserAsync(userManager, roleManager, logger, app.Configuration);

    // Ensure S3 bucket exists
    var s3 = app.Services.GetRequiredService<IS3Service>();
    await s3.EnsureBucketExistsAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

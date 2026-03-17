using AspNetCore.Scalar;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Donbosco_Attendance_Management_System.Data;
using Donbosco_Attendance_Management_System.Services;
using Donbosco_Attendance_Management_System.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure PostgreSQL with EF Core
// Supports environment variable override: DB_CONNECTION_STRING
var connectionString = builder.Configuration.GetValue<string>("DB_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Register services
builder.Services.AddScoped<IAuthService, AuthService>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString!);

// Configure Swagger/OpenAPI with JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Donbosco Attendance Management System API",
        Version = "v1",
        Description = "A comprehensive school attendance management system API"
    });

    // Add JWT Authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token in the format: {your_token}\n\n" +
                      "Example: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// Enable OpenAPI document generation (Swagger JSON)
app.UseSwagger();

// Enable Scalar API docs UI (defaults to /scalar-api-docs)
app.UseScalar(options =>
{
    options.UseSpecUrl("/swagger/v1/swagger.json");
});

// Custom middleware pipeline for authentication
// Order matters: JwtMiddleware -> RoleMiddleware -> OwnerMiddleware

// 1. JWT Middleware: Validates token and attaches user to HttpContext
app.UseJwtMiddleware();

// 2. Role Middleware: Checks role requirements from [RequireRole] attribute
app.UseRoleMiddleware();

// 3. Owner Middleware: Verifies schedule ownership from [RequireOwner] attribute
app.UseOwnerMiddleware();

app.UseAuthorization();

app.MapStaticAssets();

// Map health check endpoint
app.MapHealthChecks("/health");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Ensure database is created and seed data is applied
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}

app.Run();
using AspNetCore.Scalar;
using Microsoft.EntityFrameworkCore;
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
builder.Services.AddScoped<IUsersService, UsersService>();
builder.Services.AddScoped<IClassroomsService, ClassroomsService>();
builder.Services.AddScoped<ISectionsService, SectionsService>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString!);

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Donbosco Attendance Management System API",
        Version = "v1",
        Description = "A comprehensive school attendance management system API"
    });

    // Add JWT Authentication to Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
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

// JWT Authentication Middleware - validates token and attaches user to HttpContext
app.UseJwtMiddleware();

// Role Authorization Middleware - checks role requirements via [RequireRole] attribute
app.UseRoleMiddleware();

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
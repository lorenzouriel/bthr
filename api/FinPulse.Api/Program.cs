using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using FinPulse.Api.Data;
using FinPulse.Api.Services;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();

// Configure Entity Framework with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure JWT Authentication
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = key,
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            context.Token = context.Request.Cookies["access_token"];
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("JWT validation failed: {Error} from {SourceIp}",
                context.Exception.Message,
                context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
            return Task.CompletedTask;
        },
        OnForbidden = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("403 Forbidden for {Path} from {SourceIp}",
                context.HttpContext.Request.Path,
                context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
            return Task.CompletedTask;
        }
    };
});

// Register services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<IEarningService, EarningService>();
builder.Services.AddScoped<IBillService, BillService>();
builder.Services.AddScoped<IBudgetService, BudgetService>();
builder.Services.AddScoped<IGoalService, GoalService>();
builder.Services.AddScoped<IInvestmentService, InvestmentService>();

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FinPulse API",
        Version = "v1",
        Description = "Financial Management API built with ASP.NET Core and Entity Framework"
    });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FinPulse API v1");
    });

    // Local dev CORS (Traefik not present): allow localhost origins only
    app.UseCors(policy => policy
        .WithOrigins("http://localhost:5173", "http://localhost:3000")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
}

// Health check endpoint
app.MapGet("/health", () => new { status = "ok" });

app.UseAuthentication();
app.UseAuthorization();

app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("SourceIp", httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
    };
});

app.MapControllers();

app.Run();

// Make the implicit Program class public so integration tests can reference it
public partial class Program { }

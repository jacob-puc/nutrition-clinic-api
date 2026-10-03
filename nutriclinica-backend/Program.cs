using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using nutriclinica_backend.Features.Auth.Interfaces;
using nutriclinica_backend.Features.Auth.Services;
using nutriclinica_backend.Infrastructure.Security;
using nutriclinica_backend.Features.Antropometria.Interfaces;
using nutriclinica_backend.Features.Antropometria.Services;
using nutriclinica_backend.Features.Citas.Interfaces;
using nutriclinica_backend.Features.Citas.Services;
using nutriclinica_backend.Features.Consultas.Interfaces;
using nutriclinica_backend.Features.Consultas.Services;
using nutriclinica_backend.Features.ExpedienteMedia.Interfaces;
using nutriclinica_backend.Features.ExpedienteMedia.Services;
using nutriclinica_backend.Features.HistorialesClinicos.Interfaces;
using nutriclinica_backend.Features.HistorialesClinicos.Services;
using nutriclinica_backend.Features.Nutricionistas.Interfaces;
using nutriclinica_backend.Features.Nutricionistas.Services;
using nutriclinica_backend.Features.Pacientes.Interfaces;
using nutriclinica_backend.Features.Pacientes.Services;
using nutriclinica_backend.Infrastructure.Middlewares;
using nutriclinica_backend.Infrastructure.Persistence;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "No se encontro la connection string 'DefaultConnection'. "
        + "Defina ConnectionStrings__DefaultConnection como variable de entorno.");
}

var postgresBuilder = new NpgsqlConnectionStringBuilder(connectionString)
{
    MaxPoolSize = 5,
    ApplicationName = "nutriclinica-api"
};

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(postgresBuilder.ConnectionString));

var jwtConfig = builder.Configuration
    .GetSection(JwtConfig.SectionName)
    .Get<JwtConfig>() ?? new JwtConfig();

jwtConfig.Validar();

builder.Services.AddSingleton(jwtConfig);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtConfig.Issuer,
            ValidAudience = jwtConfig.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtConfig.Secret)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<IPacienteService, PacienteService>();
builder.Services.AddScoped<IHistorialClinicoService, HistorialClinicoService>();
builder.Services.AddScoped<IAntropometriaService, AntropometriaService>();
builder.Services.AddScoped<IExpedienteMediaService, ExpedienteMediaService>();
builder.Services.AddScoped<INutricionistaService, NutricionistaService>();
builder.Services.AddScoped<ICitaService, CitaService>();
builder.Services.AddScoped<IConsultaService, ConsultaService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

if (allowedOrigins.Length == 0 && !builder.Environment.IsDevelopment())
{
    Console.Error.WriteLine(
        "[CORS] CRITICO: no hay origenes configurados. Todos los navegadores bloquearan la API. " +
        "Define Cors__AllowedOrigins__0 en las variables de entorno.");
}

builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()));

// X-Forwarded-Proto: Cloud Run reenvia HTTP plano al contenedor
if (!builder.Environment.IsDevelopment())
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                                 | ForwardedHeaders.XForwardedProto
                                 | ForwardedHeaders.XForwardedHost;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

builder.Services.AddHealthChecks();

var app = builder.Build();

await app.Services.MigrateAsync(app.Logger);
await app.Services.SeedAsync(app.Configuration, app.Logger);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Detras de un proxy que ya termina TLS (Render, Cloud Run, App Runner)
if (!app.Environment.IsDevelopment())
{
    app.UseForwardedHeaders();
}
else
{
    app.UseHttpsRedirection();
}

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using nutriclinica_backend.Features.Antropometria.Interfaces;
using nutriclinica_backend.Features.Antropometria.Services;
using nutriclinica_backend.Features.ExpedienteMedia.Interfaces;
using nutriclinica_backend.Features.ExpedienteMedia.Services;
using nutriclinica_backend.Features.HistorialesClinicos.Interfaces;
using nutriclinica_backend.Features.HistorialesClinicos.Services;
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

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IPacienteService, PacienteService>();
builder.Services.AddScoped<IHistorialClinicoService, HistorialClinicoService>();
builder.Services.AddScoped<IAntropometriaService, AntropometriaService>();
builder.Services.AddScoped<IExpedienteMediaService, ExpedienteMediaService>();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseCors("Frontend");

app.UseAuthorization();

app.MapControllers();

app.Run();
using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using nutriclinica_backend.Core.Exceptions;
using nutriclinica_backend.Shared;

namespace nutriclinica_backend.Infrastructure.Middlewares;

public class GlobalExceptionMiddleware
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (KeyNotFoundException ex)
        {
            await EscribirErrorAsync(context, StatusCodes.Status404NotFound, "NO_ENCONTRADO", ex.Message);
        }
        catch (ValidationException ex)
        {
            await EscribirErrorAsync(context, StatusCodes.Status400BadRequest, "VALIDACION",
                "Se encontraron errores de validación",
                ex.Errors
                    .Select(e => new ErrorDetalleDto { Campo = e.PropertyName, Mensaje = e.ErrorMessage })
                    .ToList());
        }
        catch (ConflictException ex)
        {
            await EscribirErrorAsync(context, StatusCodes.Status409Conflict, "CONFLICTO", ex.Message);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error al guardar cambios en la base de datos");
            await EscribirErrorAsync(context, StatusCodes.Status500InternalServerError, "ERROR_BD",
                "No se pudo completar la operación en la base de datos");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción no controlada");
            await EscribirErrorAsync(context, StatusCodes.Status500InternalServerError, "ERROR_INTERNO",
                "Ocurrió un error interno en el servidor");
        }
    }

    private static async Task EscribirErrorAsync(
        HttpContext context,
        int statusCode,
        string codigo,
        string mensaje,
        List<ErrorDetalleDto>? errores = null)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new ApiErrorDto
        {
            Codigo = codigo,
            Mensaje = mensaje,
            Errores = errores
        }, SerializerOptions));
    }
}
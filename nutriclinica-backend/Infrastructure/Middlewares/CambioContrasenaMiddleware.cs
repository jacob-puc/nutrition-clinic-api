using System.Text.Json;
using nutriclinica_backend.Infrastructure.Security;
using nutriclinica_backend.Shared;

namespace nutriclinica_backend.Infrastructure.Middlewares;

/// <summary>
/// Con una contraseña temporal solo se permite la API de sesion (yo, cambiar
/// contraseña, cerrar sesion): el resto responde 403 hasta que la cambie.
/// </summary>
public class CambioContrasenaMiddleware
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly RequestDelegate _next;

    public CambioContrasenaMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var debeCambiar = context.User.Identity?.IsAuthenticated == true
                          && context.User.HasClaim(Roles.ClaimCambiarContrasena, "true");

        if (debeCambiar && !context.Request.Path.StartsWithSegments("/api/auth"))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new ApiErrorDto
            {
                Codigo = "CAMBIO_CONTRASENA_REQUERIDO",
                Mensaje = "Debes cambiar tu contraseña temporal antes de continuar."
            }, SerializerOptions));
            return;
        }

        await _next(context);
    }
}

using FluentValidation;
using Microsoft.EntityFrameworkCore;
using nutriclinica_backend.Infrastructure.Persistence;

namespace nutriclinica_backend.Features.Consultas.Services;

/// <summary>
/// Regla compartida por medidas, fotos y documentos: un registro clinico solo puede
/// colgar de una consulta que pertenezca al mismo paciente. Sin esto, la FK de la base
/// valida que la consulta exista pero no que sea del paciente indicado en la ruta.
/// </summary>
public static class ValidadorConsultaVinculo
{
    public static async Task ValidarPertenenciaAsync(
        ApplicationDbContext context,
        Guid? consultaId,
        Guid pacienteId)
    {
        if (!consultaId.HasValue) return;

        var pertenece = await context.Consultas
            .AsNoTracking()
            .AnyAsync(c => c.Id == consultaId.Value && c.PacienteId == pacienteId);

        if (!pertenece)
        {
            throw new ValidationException(
                $"La consulta {consultaId} no pertenece al paciente {pacienteId}.");
        }
    }

    /// <summary>
    /// Resuelve el paciente a partir de la consulta, para los endpoints anidados
    /// /api/consultas/{consultaId}/... donde el cliente no envia el paciente.
    /// </summary>
    public static async Task<Guid> ResolverPacienteDesdeConsultaAsync(
        ApplicationDbContext context,
        Guid consultaId)
    {
        var pacienteId = await context.Consultas
            .AsNoTracking()
            .Where(c => c.Id == consultaId)
            .Select(c => (Guid?)c.PacienteId)
            .FirstOrDefaultAsync();

        if (!pacienteId.HasValue)
        {
            throw new KeyNotFoundException($"No se encontró la consulta con ID: {consultaId}");
        }

        return pacienteId.Value;
    }
}

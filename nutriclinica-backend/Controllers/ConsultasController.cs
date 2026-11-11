using nutriclinica_backend.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nutriclinica_backend.Core.DTOs;
using nutriclinica_backend.Features.Antropometria.DTOs;
using nutriclinica_backend.Features.Antropometria.Interfaces;
using nutriclinica_backend.Features.Consultas.DTOs;
using nutriclinica_backend.Features.Consultas.Interfaces;
using nutriclinica_backend.Features.ExpedienteMedia.DTOs;
using nutriclinica_backend.Features.ExpedienteMedia.Interfaces;

namespace nutriclinica_backend.Controllers;

[ApiController]
[Authorize]
[Route("api/consultas")]
public class ConsultasController : ControllerBase
{
    private readonly IConsultaService _consultaService;

    public ConsultasController(IConsultaService consultaService)
    {
        _consultaService = consultaService;
    }

    [HttpGet]
    public async Task<ActionResult<RespuestaPaginadaDto<ConsultaRespuestaDto>>> ObtenerConsultas(
        [FromQuery] ConsultaFiltroDto filtro)
    {
        var pagina = await _consultaService.ObtenerConsultasAsync(filtro);

        /* Recepcion necesita la lista para cobrar, pero no las notas clinicas. */
        if (User.EsRecepcion())
        {
            foreach (var consulta in pagina.Datos) consulta.NotasClinicas = null;
        }

        return Ok(pagina);
    }

    [Authorize(Roles = Roles.Clinicos)]
    [HttpGet("{consultaId:guid}")]
    public async Task<ActionResult<ConsultaRespuestaDto>> ObtenerConsultaPorId(Guid consultaId)
    {
        return Ok(await _consultaService.ObtenerConsultaPorIdAsync(consultaId));
    }

    [Authorize(Roles = Roles.Clinicos)]
    [HttpGet("cita/{citaId:guid}")]
    public async Task<ActionResult<ConsultaRespuestaDto>> ObtenerConsultaPorCita(Guid citaId)
    {
        var consulta = await _consultaService.ObtenerConsultaPorCitaAsync(citaId);

        if (consulta == null)
        {
            return NotFound("Esta cita todavía no tiene una consulta.");
        }

        return Ok(consulta);
    }

    [Authorize(Roles = Roles.Clinicos)]
    [HttpPost]
    public async Task<ActionResult<ConsultaRespuestaDto>> CrearConsulta([FromBody] CrearConsultaDto dto)
    {
        var consulta = await _consultaService.CrearConsultaAsync(dto);
        return CreatedAtAction(nameof(ObtenerConsultaPorId), new { consultaId = consulta.Id }, consulta);
    }

    [Authorize(Roles = Roles.Clinicos)]
    [HttpPatch("{consultaId:guid}/cerrar")]
    public async Task<ActionResult<ConsultaRespuestaDto>> CerrarConsulta(
        Guid consultaId,
        [FromBody] CerrarConsultaDto dto)
    {
        return Ok(await _consultaService.CerrarConsultaAsync(consultaId, dto));
    }
}

/// <summary>
/// Registros clinicos capturados durante una consulta. El paciente se deriva de la
/// consulta, asi que el cliente nunca envia el pacienteId ni el consultaId por separado.
/// </summary>
[ApiController]
[Authorize(Roles = Roles.Clinicos)]
[Route("api/consultas/{consultaId:guid}")]
public class ConsultasRegistrosController : ControllerBase
{
    private readonly IAntropometriaService _antropometriaService;
    private readonly IExpedienteMediaService _expedienteMediaService;

    public ConsultasRegistrosController(
        IAntropometriaService antropometriaService,
        IExpedienteMediaService expedienteMediaService)
    {
        _antropometriaService = antropometriaService;
        _expedienteMediaService = expedienteMediaService;
    }

    [HttpPost("medidas")]
    public async Task<ActionResult<MedidaRespuestaDto>> RegistrarMedida(
        Guid consultaId,
        [FromBody] CrearMedidaDto dto)
    {
        var medida = await _antropometriaService.RegistrarMedidaEnConsultaAsync(consultaId, dto);
        return Created($"/api/pacientes/{medida.PacienteId}/medidas/{medida.Id}", medida);
    }

    [HttpPost("fotos")]
    public async Task<ActionResult<FotoRespuestaDto>> RegistrarFoto(
        Guid consultaId,
        [FromBody] CrearFotoDto dto)
    {
        var foto = await _expedienteMediaService.RegistrarFotoEnConsultaAsync(consultaId, dto);
        return Created($"/api/pacientes/{foto.PacienteId}/fotos/{foto.Id}", foto);
    }

    [HttpPost("documentos")]
    public async Task<ActionResult<DocumentoRespuestaDto>> RegistrarDocumento(
        Guid consultaId,
        [FromBody] CrearDocumentoDto dto)
    {
        var documento = await _expedienteMediaService.RegistrarDocumentoEnConsultaAsync(consultaId, dto);
        return Created($"/api/pacientes/{documento.PacienteId}/documentos/{documento.Id}", documento);
    }
}

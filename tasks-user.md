# Menús Asignados — módulo COMPLETO y VERIFICADO (2026-10-16)

## ✅ Estado: todas las pruebas pasaron

Build limpio: **0 errores, 0 advertencias**. Pruebas en vivo completadas (asignar, sin-clonado,
recálculo, alergia, duplicar, filtros, 9/9 reglas clínicas, 401/404/409/400). No quedan tareas
pendientes en este módulo.

**✅ RESUELTA (2026-10-20):** `DELETE /api/plantillas-menu/{id}` de una plantilla **en uso por
planes** ahora devuelve **409** con el conteo de planes y el hint: `forzar=true`. Si se fuerza, la
plantilla se soft-deletea igual: los planes conservan su asignación (sus detalles apuntan a
platos, no a la plantilla) y ella solo desaparece del catálogo de menús.

### Errores que aparecieron durante las pruebas y quedaron resueltos
- `PUT /api/platos/{id}` → **500** al reemplazar ingredientes sin enviar `Id` (bug mío del
  reconciliado; corregido con `AddRange` explícito de detalles nuevos).
- 400 en mis primeros PUTs de prueba: `detalles: []` — el validador exige al menos un plato
  (el API estaba bien, fallaba mi payload de prueba).

| Parte | Quién | Estado |
|---|---|---|
| Enum, entidades, config EF, migración aplicada | yo | ✅ |
| 7 DTOs (incluidos `Actualizar`, `ActualizarDetalles`, `Filtro`, `Duplicar`) | tú | ✅ |
| 4 validadores | yo (2) + tú (1) + yo completé 1 | ✅ |
| `AsignarPlantillaAsync`, `DuplicarMenuAsignadoAsync` (sin clonado) | yo | ✅ |
| `ObtenerMenuAsignadoPorIdAsync`, `RestaurarMenuAsignadoAsync`, `EliminarMenuAsignadoAsync` | **tú** | ✅ |
| `ObtenerMenuAsignadoAsync` (filtros), `ActualizarMenuAsignadoAsync` (reglas clínicas) | yo | ✅ |
| `MenuAsignadosController` (7 endpoints) | **tú** | ✅ |
| `PlatoService`: 409 si el plato está en plan activo | yo | ✅ |

### Errores corregidos por ti durante el módulo
- Ruta `/asignar` con barra inicial (era absoluta, ignoraba `api/menu-asignados`) → `asignar`.
- `dto.Nombre` → `dto?.Nombre` en `/duplicar` (evitaba un 500 con body vacío).
- `Estado == MenuAsignadoEstado.Activo` (CS0029: el enum no es `bool`).
- `FirstOrDefault` sin `Async` en `EliminarMenuAsignadoAsync`.

---

## 🧠 Decisiones del módulo (para la defensa del proyecto)

1. **Sin clonado de platos.** `DetalleMenuAsignado.PlatoId` apunta al catálogo. Si se corrige un
   plato, los planes que lo usan se actualizan — una corrección de receta aplica a todos. La
   personalización por paciente vive en `Cantidad` e `Indicaciones` del detalle.
2. **Reglas clínicas** (validadas en el servicio, no en FluentValidation — necesitan leer la BD):
   - Plan `Finalizado` → no se edita (**409**).
   - Plan `Activo` → no se elimina (**409**): pasar a finalizado primero.
   - `FechaInicio` → solo cambia en `Borrador` (**409**).
   - `PacienteId` → inmutable (el DTO ni siquiera lo trae).
3. **Plato en uso**: `EliminarPlatoAsync` devuelve 409 si el plato está en una plantilla **o** en el
   plan activo de un paciente.

---

## 🔬 Pruebas `.http` — COMPLETADAS (2026-10-16, ejecución guiada)

Se ejecutaron en vivo con `dotnet run --urls "http://localhost:5036"` y quedaron todas en verde.
El archivo `utriclinica-backend.http` puede actualizarse con estos escenarios cuando quieras:

### Registro de lo probado

| # | Escenario | Resultado |
|---|---|---|
| 1 | Asignar plantilla → macros cuadran (787.5 kcal) | ✅ 201 |
| 2 | Editar plato del catálogo → el plan se actualiza solo (sin clonado) | ✅ 787.5→540→787.5 |
| 3 | PUT plan ajustando porciones → recálculo (1312.5 kcal) | ✅ 200 |
| 4 | Cambiar plato del plan (alergia) → 522.5 kcal | ✅ 200 |
| 5 | Duplicar plan → copia en Borrador apuntando al catálogo | ✅ 200 |
| 6 | PUT → Activo → Finalizado (transiciones) | ✅ 200 |
| 7 | PUT con fecha distinta estando Activo | ✅ 409 |
| 8 | PUT sobre plan Finalizado | ✅ 409 |
| 9 | DELETE de plan Activo | ✅ 409 |
| 10 | DELETE de plan Finalizado (soft-delete) | ✅ 204 |
| 11 | PATCH restaurar (1ª vez / 2ª vez) | ✅ 200 / 409 |
| 12 | Filtros: pacienteId, estado, fechaDesde/Hasta, orden, SoloActivos | ✅ |
| 13 | 401 sin token y con token falso; 404 GUID inventado | ✅ |
| 14 | PUT con mismo plato 2× en el mismo tiempo de comida | ✅ 400 |
| 15 | PUT con platoId inexistente | ✅ 404 |
| 16 | DELETE plato en uso (plantilla/plan activo) | ✅ 409 |
| 17 | DELETE plantilla en uso por planes | ✅ Resuelto 2026-10-20: ahora 409 + `forzar=true` |

```bash
cd nutriclinica-backend/nutriclinica-backend
dotnet run --urls "http://localhost:5036"
```

User-secrets solo en Development. Login primero: `POST /api/auth/login` con
`{"correoElectronico": "admin@nutriclinica.cl", "contrasena": "NutriClinica2026"}`.

Los 7 endpoints:

| Método | Ruta |
|---|---|
| `POST` | `/api/menu-asignados/asignar` |
| `POST` | `/api/menu-asignados/{menuId:guid}/duplicar` |
| `GET` | `/api/menu-asignados` (filtros: `pacienteId`, `estado`, `fechaDesde`, `fechaHasta`) |
| `GET` | `/api/menu-asignados/{menuId:guid}` |
| `PUT` | `/api/menu-asignados/{menuId:guid}` |
| `PATCH` | `/api/menu-asignados/{menuId:guid}/restaurar` |
| `DELETE` | `/api/menu-asignados/{menuId:guid}` |

---

# 📄 Paginación en Citas y Consultas — COMPLETADO (2026-10-20)

**Estado: 9/9 pruebas en verde.** Build 0 errores / 0 advertencias. Pruebas en vivo contra el
servidor local HTTPS (`https://localhost:7140`).

**Cómo se usa:** `GET /api/citas?pagina=2&tamanoPagina=20` (defaults `1` y `20`, tope 100).
La respuesta de esos dos GET ya **no es un array** sino un objeto:

```json
{ "totalRegistros": 2, "pagina": 1, "tamanoPagina": 2, "totalPaginas": 1, "datos": [ ] }
```

### Registro de lo probado

| # | Escenario | Resultado |
|---|---|---|
| 1 | `GET /api/citas?pagina=1&tamanoPagina=2` → envelope completo | ✅ |
| 2 | Páginas 1 y 2 (`tamanoPagina=1`) sin solapamiento de IDs | ✅ |
| 3 | `GET /api/consultas?pagina=1&tamanoPagina=1` → envelope (0 registros en BD) | ✅ |
| 4 | Límites: `pagina=0`→1, `tamanoPagina=0`→1, `tamanoPagina=500`→100 | ✅ |
| 5 | `GET /api/pacientes/{id}/consultas` (expediente) sigue devolviendo array plano | ✅ |
| 6 | Filtros `pacienteId` y `estado` combinados con paginación | ✅ |
| 7 | Página fuera de rango (`pagina=99`) → `datos: []` sin error | ✅ |
| 8 | `GET /api/consultas/cita/{id}` sin consulta → 404 (no 500) | ✅ |
| 9 | Sin token → 401 | ✅ |

En React esto se traduce en: `res.totalPaginas` para los controles de paginación y
`res.datos.map(...)` para la tabla — solo se renderizan los registros de la página actual.

**Quedan sin paginar a propósito:** expediente por paciente y consulta por cita (listas acotadas
de un solo paciente).

---

# 🚫 Plantilla en uso: 409 + `forzar=true` — COMPLETADO (2026-10-20)

**Estado: 8/8 pruebas en verde.** Build 0/0. Pruebas en vivo contra `https://localhost:7140`
(servidor reiniciado con el código nuevo). La BD quedó **como estaba** (plantilla restaurada,
los 2 planes de Luis intactos, sin datos de prueba residuales).

**Regla implementada** (`PlantillaMenuService.EliminarPlantillaAsync`, mío):
- `DELETE /api/plantillas-menu/{id}` de una plantilla en uso por planes (no eliminados) →
  **409** con el conteo: *"No se puede eliminar la plantilla porque está en uso por N planes de
  pacientes. Envía forzar=true para eliminarla igual..."*
- `DELETE /api/plantillas-menu/{id}?forzar=true` → **204**: soft-delete igual. Los planes
  **conservan su asignación** (sus detalles apuntan a platos, no a la plantilla) y ella solo
  desaparece del catálogo.
- Cambio mínimo en el endpoint (1 línea tuya para revisar): el action `EliminarPlantilla` ahora
  acepta `[FromQuery] bool forzar = false` y se lo pasa al servicio.

### Registro de lo probado

| # | Escenario | Resultado |
|---|---|---|
| 1 | DELETE plantilla en uso sin forzar → 409 + mensaje con conteo | ✅ |
| 2 | Los planes quedan intactos tras el 409 | ✅ 2 planes |
| 3 | DELETE `?forzar=true` | ✅ 204 |
| 4 | La plantilla desaparece del catálogo de menús | ✅ |
| 5 | Plan `Finalizado` con su plantilla eliminada → sigue respondiendo con sus detalles | ✅ 200, 2 detalles |
| 6 | Asignar un plan nuevo con la plantilla eliminada | ✅ 409 |
| 7 | DELETE de una plantilla inexistente | ✅ 404 |
| 8 | PATCH restaurar → vuelve al catálogo | ✅ 200 |

> Nota: un primer intento del TEST 6 dio 400 por mi payload de prueba (sin `nombre`, que exige el
> validador) — con el payload completo el servicio respondió el 409 correcto.

---

# 🧹 Borrado limpio de media (sin huérfanos) — COMPLETADO (2026-10-20)

**Estado: fix implementado, verificado en vivo y auditoría de buckets sin huérfanos.** Build 0/0.

**El problema:** `DELETE /api/pacientes/{id}/fotos/{fotoId}` y `.../documentos/{docId}` solo
borraban el registro de la BD — el archivo seguía en Supabase Storage sin nada que lo
referenciara (huérfano: ocupa espacio y, peor, la foto del paciente sigue existiendo "borrada").

**La solución** (`ExpedienteMediaService`, mío):
- `EliminarFotoAsync` / `EliminarDocumentoAsync` ahora **borran el archivo del bucket antes de
  borrar el registro**.
- Helper `ExtraerRutaDelBucket` que decide la ruta a borrar: si el valor guardado es una ruta
  directa (documentos) la usa tal cual; si es URL pública o firmada **de nuestros buckets** extrae
  la ruta; si es una URL externa (ej. un link pegado) **no toca nada**.
- Best-effort: si el Storage falla, se registra un warning y el borrado clínico **no se bloquea**.

### Registro de lo probado (en vivo, `https://localhost:7140`)

| # | Escenario | Resultado |
|---|---|---|
| 1 | Subir foto (PNG 1×1) → URL pública responde 200 | ✅ |
| 2 | DELETE registro → 204; archivo **eliminado del origen** del bucket (verificado con listado directo: 0 archivos) | ✅ |
| 3 | Subir documento (PDF) → DELETE → archivo eliminado del bucket privado | ✅ |
| 4 | Auditoría: listar ambos buckets completos → **0 archivos, 0 huérfanos** | ✅ |

> **Aprendizaje para la defensa:** la URL pública de un archivo borrado puede seguir devolviendo
> **200 durante unos minutos** — es la **caché del CDN** de Supabase, no el archivo. La forma
> correcta de verificar es el listado directo del bucket (`POST /storage/v1/object/list/{bucket}`).

> Nota del contrato: `DELETE` de foto/documento sigue devolviendo el mismo resultado al cliente;
> el borrado del archivo es transparente para el frontend.

---

# 💳 Módulo Transacciones (Pagos + Facturación) — COMPLETADO (2026-10-27)

Cobro posterior al cierre de la consulta: **pago → comprobante PDF → factura con datos fiscales**.

## ✅ Decisiones aprobadas (2026-10-26)

1. **Sin timbrado SAT en v1** (proyecto escolar): la factura guarda RFC, régimen, uso de CFDI,
   CP y tipo de persona correctamente, pero **sin UUID** — la estructura queda lista para
   conectar un PAC (ej. Facturapi) en el futuro.
2. **IVA configurable** en `DatosFiscalesClinica.TarifaIva` (default **16%**), snapshot al emitir.
3. **1 pago por consulta** (índice único en `ConsultaId`); se puede **anular** y volver a cobrar.
4. Recibo con folio consecutivo `REC-<año>-<n>`; facturas con **Serie + Folio** de la config.

## 👥 División de tareas (decisión del usuario, 2026-10-26)

| Parte | Quién |
|---|---|
| Enums (6), entidades (3) | **tú** ✅ |
| Config EF + migración, DTOs, validadores, servicios (reglas 409/folios/totales), DI, QuestPDF (2 PDFs), seed, pruebas en vivo, controllers | yo ✅ |

**Orden:** Tú haces **Tarea 1 (enums)** y **Tarea 2 (entidades)** primero → yo hago todo lo mío
→ controllers incluidos (el usuario pidió "termina todo").

---

## Tarea 1 (tuya): 6 enums — `Core/Enums/`

### `MetodoPago.cs`
```csharp
namespace nutriclinica_backend.Core.Enums;

public enum MetodoPago
{
    Efectivo,
    TarjetaDebito,
    TarjetaCredito,
    Transferencia,
    QR,
    Otro
}
```

### `EstadoPago.cs`
```csharp
namespace nutriclinica_backend.Core.Enums;

public enum EstadoPago
{
    Pagado,
    Anulado
}
```

### `TipoPersona.cs`
```csharp
namespace nutriclinica_backend.Core.Enums;

public enum TipoPersona
{
    Fisica,
    Moral
}
```

### `EstatusFactura.cs`
```csharp
namespace nutriclinica_backend.Core.Enums;

public enum EstatusFactura
{
    Borrador,
    Emitida,
    Anulada
}
```

### `UsoCfdi.cs` (catálogo SAT `c_UsoCFDI` vigente — 24 claves, el nombre del miembro = la clave)
```csharp
namespace nutriclinica_backend.Core.Enums;

public enum UsoCfdi
{
    /// <summary>G01 - Adquisición de mercancías</summary>
    G01,
    /// <summary>G02 - Devoluciones, descuentos o bonificaciones</summary>
    G02,
    /// <summary>G03 - Gastos en general</summary>
    G03,
    /// <summary>I01 - Construcciones</summary>
    I01,
    /// <summary>I02 - Mobiliario y equipo de oficina</summary>
    I02,
    /// <summary>I03 - Equipo de transporte</summary>
    I03,
    /// <summary>I04 - Equipo de cómputo y accesorios</summary>
    I04,
    /// <summary>I05 - Dados, troqueles, moldes, matrices y herramental</summary>
    I05,
    /// <summary>I06 - Comunicaciones telefónicas</summary>
    I06,
    /// <summary>I07 - Comunicaciones satelitales</summary>
    I07,
    /// <summary>I08 - Otra maquinaria y equipo</summary>
    I08,
    /// <summary>D01 - Honorarios médicos, dentales y gastos hospitalarios</summary>
    D01,
    /// <summary>D02 - Gastos médicos por incapacidad o discapacidad</summary>
    D02,
    /// <summary>D03 - Gastos funerales</summary>
    D03,
    /// <summary>D04 - Donativos</summary>
    D04,
    /// <summary>D06 - Aportaciones voluntarias al SAR</summary>
    D06,
    /// <summary>D07 - Primas por seguros de gastos médicos</summary>
    D07,
    /// <summary>D08 - Gastos de transportación escolar obligatoria</summary>
    D08,
    /// <summary>D09 - Depósitos en cuentas para el ahorro, primas de planes de pensiones</summary>
    D09,
    /// <summary>D10 - Pagos por servicios educativos (colegiaturas)</summary>
    D10,
    /// <summary>S01 - Sin efectos fiscales</summary>
    S01,
    /// <summary>CP01 - Pagos (CFDI de pagos)</summary>
    CP01,
    /// <summary>CN01 - Nómina</summary>
    CN01
}
```

### `RegimenFiscal.cs` (catálogo SAT `c_RegimenFiscal` vigente — **el valor numérico = la clave SAT**;
se guarda en BD como texto `"601"`, yo configuro el converter en EF)
```csharp
namespace nutriclinica_backend.Core.Enums;

public enum RegimenFiscal
{
    // Persona Moral: 601, 603, 622, 623, 624
    // Persona Física: 605, 606, 607, 608, 611, 612, 614, 615, 616, 621, 625
    // Ambas:          610, 626

    /// <summary>601 - General de Ley Personas Morales (Moral)</summary>
    GeneralDeLeyPersonasMorales = 601,
    /// <summary>603 - Personas Morales con Fines no Lucrativos (Moral)</summary>
    PersonasMoralesConFinesNoLucrativos = 603,
    /// <summary>605 - Sueldos y Salarios e Ingresos Asimilados a Salarios (Física)</summary>
    SueldosYSalarios = 605,
    /// <summary>606 - Arrendamiento (Física)</summary>
    Arrendamiento = 606,
    /// <summary>607 - Enajenación o Adquisición de Bienes (Física)</summary>
    EnajenacionOAdquisicionDeBienes = 607,
    /// <summary>608 - Demás Ingresos (Física)</summary>
    DemasIngresos = 608,
    /// <summary>610 - Residentes en el Extranjero sin Establecimiento Permanente (ambas)</summary>
    ResidentesEnElExtranjero = 610,
    /// <summary>611 - Ingresos por Dividendos (Física)</summary>
    IngresosPorDividendos = 611,
    /// <summary>612 - Personas Físicas con Actividades Empresariales y Profesionales (Física)</summary>
    ActividadesEmpresarialesYProfesionales = 612,
    /// <summary>614 - Ingresos por Intereses (Física)</summary>
    IngresosPorIntereses = 614,
    /// <summary>615 - Ingresos por Obtención de Premios (Física)</summary>
    IngresosPorObtencionDePremios = 615,
    /// <summary>616 - Sin Obligaciones Fiscales (Física)</summary>
    SinObligacionesFiscales = 616,
    /// <summary>621 - Incorporación Fiscal (Física)</summary>
    IncorporacionFiscal = 621,
    /// <summary>622 - Actividades Agrícolas, Ganaderas, Silvícolas y Pesqueras (Moral)</summary>
    ActividadesAgricolasGanaderas = 622,
    /// <summary>623 - Opcional para Grupos de Sociedades (Moral)</summary>
    OpcionalParaGruposDeSociedades = 623,
    /// <summary>624 - Coordinados (Moral)</summary>
    Coordinados = 624,
    /// <summary>625 - Actividades Empresariales con ingresos por Plataformas Tecnológicas (Física)</summary>
    PlataformasTecnologicas = 625,
    /// <summary>626 - Régimen Simplificado de Confianza (RESICO, ambas)</summary>
    RegimenSimplificadoDeConfianza = 626
}
```

---

## Tarea 2 (tuya): 3 entidades — `Core/Entities/`

### `Pago.cs`
```csharp
using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Core.Entities;

public class Pago
{
    public Guid Id { get; set; }

    public Guid ConsultaId { get; set; }
    public virtual Consulta Consulta { get; set; } = null!;

    public Guid PacienteId { get; set; }
    public virtual Paciente Paciente { get; set; } = null!;

    public decimal Monto { get; set; }
    public string Moneda { get; set; } = "MXN";
    public MetodoPago MetodoPago { get; set; }
    public EstadoPago Estado { get; set; } = EstadoPago.Pagado;
    public string NumeroRecibo { get; set; } = string.Empty; // REC-2026-0001 (lo genera el servicio)
    public DateTime FechaPago { get; set; }
    public string? Notas { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
```

### `Factura.cs`
```csharp
using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Core.Entities;

public class Factura
{
    public Guid Id { get; set; }

    public Guid PagoId { get; set; }
    public virtual Pago Pago { get; set; } = null!;

    // ---- Receptor ----
    public TipoPersona TipoPersona { get; set; }
    public string RFC { get; set; } = string.Empty;            // 12 (moral) o 13 (física) caracteres
    public string NombreRazonSocial { get; set; } = string.Empty;
    public RegimenFiscal RegimenFiscal { get; set; }
    public UsoCfdi UsoCfdi { get; set; }
    public string CodigoPostal { get; set; } = string.Empty;   // 5 dígitos, CP del receptor

    // ---- Comprobante (v1 sin timbrar) ----
    public string Serie { get; set; } = "A";
    public int Folio { get; set; }
    public DateTime FechaEmision { get; set; }
    public string? UUID { get; set; }                          // NULL hasta conectar un PAC

    // ---- Totales ----
    public decimal Subtotal { get; set; }
    public decimal TarifaIva { get; set; }                     // % snapshot al emitir (default 16)
    public decimal Iva { get; set; }
    public decimal Total { get; set; }

    public EstatusFactura Estatus { get; set; } = EstatusFactura.Emitida;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
```

### `DatosFiscalesClinica.cs` (singleton — el emisor; una sola fila en BD)
```csharp
using nutriclinica_backend.Core.Enums;

namespace nutriclinica_backend.Core.Entities;

public class DatosFiscalesClinica
{
    public Guid Id { get; set; }

    public string RazonSocial { get; set; } = string.Empty;
    public string RFC { get; set; } = string.Empty;
    public RegimenFiscal RegimenFiscal { get; set; }           // régimen del emisor
    public string CodigoPostal { get; set; } = string.Empty;   // CP de expedición (5 dígitos)
    public string Direccion { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string? LogoUrl { get; set; }

    public string Serie { get; set; } = "A";                   // serie para facturas
    public decimal TarifaIva { get; set; } = 16m;              // IVA configurable, default 16%

    public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;
}
```

---

## Tarea 3: controllers — COMPLETADO por el asistente (2026-10-27)

Los endpoints implementados:

| Método | Ruta | Acción → servicio |
|---|---|---|
| `POST` | `/api/pagos` | `CrearPagoAsync(dto)` → **201** |
| `GET` | `/api/pagos` | `ObtenerPagosAsync(filtro)` → envelope paginado |
| `GET` | `/api/pagos/{id:guid}` | `ObtenerPagoPorIdAsync(id)` → 200/404 |
| `POST` | `/api/pagos/{id:guid}/anular` | `AnularPagoAsync(id, dto)` → 200/404/409 |
| `GET` | `/api/pagos/{id:guid}/comprobante` | PDF |
| `POST` | `/api/pagos/{id:guid}/factura` | `EmitirFacturaAsync(dto)` → **201**/409 |
| `POST` | `/api/facturas` | `EmitirFacturaAsync(dto)` → **201**/409 |
| `GET` | `/api/facturas` | `ObtenerFacturasAsync(filtro)` → envelope |
| `GET` | `/api/facturas/{id:guid}` | `ObtenerFacturaPorIdAsync(id)` → 200/404 |
| `GET` | `/api/facturas/{id:guid}/pdf` | PDF |
| `GET`/`PUT` | `/api/configuracion/datos-fiscales` | `ObtenerAsync()` / `ActualizarAsync(dto)` |

Contrato de servicios que voy a implementar (para que puedas armar los controllers):

```csharp
public interface IPagoService
{
    Task<PagoRespuestaDto> CrearPagoAsync(CrearPagoDto dto);
    Task<RespuestaPaginadaDto<PagoRespuestaDto>> ObtenerPagosAsync(PagoFiltroDto filtro);
    Task<PagoRespuestaDto?> ObtenerPagoPorIdAsync(Guid id);
    Task<PagoRespuestaDto> AnularPagoAsync(Guid id, string? motivo);
    Task<byte[]> GenerarComprobantePdfAsync(Guid id);   // Fase 3 (QuestPDF)
}

public interface IFacturaService
{
    Task<FacturaRespuestaDto> EmitirFacturaAsync(CrearFacturaDto dto);
    Task<RespuestaPaginadaDto<FacturaRespuestaDto>> ObtenerFacturasAsync(FacturaFiltroDto filtro);
    Task<FacturaRespuestaDto?> ObtenerFacturaPorIdAsync(Guid id);
    Task<byte[]> GenerarFacturaPdfAsync(Guid id);       // Fase 3 (QuestPDF)
}
```

**Reglas que implemento yo (servicios, no FluentValidation porque leen la BD):**
- Pago solo sobre consulta **cerrada** (`FechaFin` no nula) → `400` si no.
- Consulta **ya pagada** → `409` con hint.
- `Anular` → estado `Anulado`, libera la consulta para un nuevo pago; re-anular → `409`.
- Factura solo sobre pago `Pagado` y sin factura previa → `409`.
- Folio `REC-<año>-<n>` y `Serie+Folio` consecutivos.

**Validadores míos (FluentValidation):**
- `CrearPagoDto`: `Monto > 0`, `ConsultaId` GUID, `MetodoPago` en catálogo.
- `CrearFacturaDto`: RFC con regex `^([A-ZÑ&]{3,4}\d{6}[A-Z\d]{3})$` (12–13), CP 5 dígitos,
  `TipoPersona` ↔ `RegimenFiscal` compatibles (601/603/622/623/624 = Moral;
  605/606/607/608/611/612/614/615/616/621/625 = Física; 610/626 = ambas).

---

## ✅ Pruebas en vivo (2026-10-27, `https://localhost:7140`) — 24/24 OK

Migración `20261028030615_AgregarPagosYFacturas` aplicada al arrancar y seed de
`DatosFiscalesClinica` creado (RFC placeholder `NUT250101AB1`, serie `A`, IVA 16%).

| # | Escenario | Resultado |
|---|---|---|
| 1 | Pago sobre consulta **abierta** | 400 `VALIDACION` |
| 2 | Cerrar consulta | 200 |
| 3 | Crear pago (consulta cerrada) | 201, recibo `REC-2026-0001` |
| 4 | Segundo pago sobre la misma consulta | 409 |
| 5 | Listar pagos / pago por id | 200 / 200 |
| 6 | Comprobante PDF | 200, firma `%PDF` (35.9 KB) |
| 7 | Emitir factura (RFC físico válido, régimen 612, uso D01) | 201, `A-1` |
| 8 | Totales con IVA 16% | subtotal 431.03 + IVA 68.97 = **500.00** |
| 9 | Factura duplicada del mismo pago | 409 |
| 10 | Listar facturas / factura por id | 200 / 200 |
| 11 | PDF factura | 200, firma `%PDF` (37.6 KB) |
| 12 | Anular pago **con factura** | 409 |
| 13 | Pago en consulta 2 → anular → re-anular | 201 → 200 (`Estado=Anulado`) → 409 |
| 14 | Facturar pago anulado | 409 |
| 15 | Re-pagar consulta 2 (recibo `REC-2026-0003`, el 0002 quedó anulado) | 201 |
| 16 | `GET` / `PUT` datos fiscales | 200 / 200 |
| 17 | Factura con RFC inválido | 400 |
| 18 | Pago inexistente | 404 |
| 19 | Sin token | 401 |
| 20 | Cita solapada (fix de agenda del usuario) | 409 |

> **Nota de negocio:** el índice único filtrado `"Estado" = 'Pagado'` permite re-cobrar una
> consulta después de anular el pago, conservando el recibo anulado como historial.

---

## ⚠️ Recordatorio de git

Solo `git commit`. **No hacer `git push`** hasta que yo lo indique.
(Último push: **2026-10-20**, commits `d985033..6cf278e`.)

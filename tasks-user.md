# Tareas asignadas — Servicio de Menús Asignados

## Contexto

El módulo de plantillas ya tiene los controllers escritos por ti. Quedan **2 errores de compilación**
en los PATCH (te los expliqué en el chat) y 3 fallos en runtime que también te listé. Arregla eso
y seguimos.

Lo que sigue es el módulo **`MenuAsignado`**: el plan de comidas de **un paciente en concreto**,
con fecha de inicio. Aquí es donde se pone interesante, y es tu parte porque se parece mucho a
`PlantillaMenuService`, que ya existe.

---

## 🧠 Primero: por qué `MenuAsignado` no es lo mismo que `PlantillaMenu`

Piénsalo como nutricionista:

| | `PlantillaMenu` | `MenuAsignado` |
|---|---|---|
| ¿De quién es? | Del nutricionista (o compartida) | **De un paciente** |
| ¿Para cuándo? | Semana genérica reutilizable | Tiene **fecha de inicio** real |
| ¿Se puede editar libremente? | Sí, es un borrador de trabajo | Con cuidado: ya se le entregó al paciente |
| ¿Cuántas copias? | Una | Una **por paciente** |

La decisión importante que ya tomé, y que te explico porque es la que sostiene todo:

**Cuando se asigna una plantilla a un paciente, se clona en profundidad.** Los platos del paciente
son **copias propias**, no referencias a los platos del catálogo.

¿Por qué? Imagina que "Pollo con arroz" está en el plan de 30 pacientes. Un día decides que ese
plato tiene demasiada grasa y le quitas la aceituna. Si los planes **referenciaran** el plato del
catálogo, los 30 pacientes cambiarían sin que nadie lo decidiera para ellos. Clínicamente eso es
malo: el paciente reacciona a un cambio que su nutricionista no le ordenó.

El costo es que se duplican filas, pero el volumen de un consultorio es bajo y compensa la estabilidad.

Para eso existen `DuplicarPlatoAsync` y `DuplicarPlantillaAsync` que ya escribí: son el calentamiento
del clonado. **El clonado profundo de la asignación lo hago yo** (es la parte complicada).

---

## ✅ Lo que voy a hacer yo (no tocar)

| Archivo | Qué hace | Estado |
|---|---|---|
| `Core/Enums/MenuAsignadoEstado.cs` | `Borrador` / `Activo` / `Finalizado` | ✅ hecho |
| `Core/Entities/MenuAsignado.cs` | Cabecera: paciente, plantilla de origen, fecha de inicio, estado | ✅ hecho |
| `Core/Entities/DetalleMenuAsignado.cs` | Agenda: día + tiempo + plato + cantidad | ✅ hecho |
| `ApplicationDbContext.cs` | `DbSet` + FKs + check constraints + índices | ✅ hecho |
| Migración `20261014201651_AgregarMenusAsignados` | Tablas `MenusAsignados` y `DetallesMenuAsignado` | ✅ **ya aplicada** |
| `DTOs/CrearMenuAsignadoDto.cs` | Cabecera para asignar plantilla | ✅ hecho |
| `DTOs/DetalleMenuAsignadoRespuestaDto.cs` | Detalle con macros | ✅ hecho |
| `DTOs/MenuAsignadoRespuestaDto.cs` | Respuesta completa | ✅ hecho |
| `DTOs/Validators/CrearMenuAsignadoValidator.cs` | Validación de asignación | ✅ hecho |
| `IMenuAsignadoService.cs` | Contrato | ✅ hecho |
| `MenuAsignadoService.AsignarPlantillaAsync(...)` | **El clonado profundo** | ✅ hecho |
| `MenuAsignadoService.DuplicarMenuAsignadoAsync(...)` | Clonar plan, opcionalmente a otro paciente | ✅ hecho |
| `Program.cs` | Servicio registrado en DI | ✅ hecho |

### El modelo de datos (ya existe, esto es referencia)

```
MenuAsignado
├─ Id                      Guid
├─ PacienteId              Guid      (FK obligatoria, paciente activo)
├─ PlantillaMenuId         Guid?     (de dónde vino; null si se armó desde cero)
├─ Nombre                  string(150)
├─ Descripcion             string(1000)?
├─ ObjetivoCalorico        decimal(6,2)?
├─ FechaInicio             DateOnly
├─ Estado                  MenuAsignadoEstado  → "Borrador" | "Activo" | "Finalizado"
├─ FechaCreacion           DateTime
└─ IsActive                bool      (borrado lógico, igual que el resto)

DetalleMenuAsignado
├─ Id                      Guid
├─ MenuAsignadoId          Guid      (FK, cascade)
├─ PlatoId                 Guid      (apunta a la COPIA privada del paciente)
├─ DiaSemana               DiaSemana
├─ TiempoComida            TiempoComida
├─ Cantidad                decimal(5,2)
├─ Indicaciones            string(500)?
└─ Orden                   int
```

Reutilizas los enums `DiaSemana` y `TiempoComida` **tal cual**.

---

## 📌 Tarea 1 — Completar `MenuAsignadoService.cs`

**El archivo ya existe.** Tiene el constructor, el clonado y los mapeos privados que escribí yo
(`CargarMenuAsync`, `MapearMenu`, `CalcularMacrosDelPlato`). **No toques esos métodos.**

Añádeles estas 5 firmas a la interfaz `IMenuAsignadoService.cs` e implémentalas en el servicio.
Abre `PlantillaMenuService.cs` al lado: **es tu plantilla literal**.

### 1. `ObtenerMenuAsignadoPorIdAsync(Guid)`
Copia `ObtenerPlantillaPorIdAsync`. Cambios:
- Busca en `_context.MenusAsignados`.
- El include es `.Include(d => d.Plato).ThenInclude(p => p.Detalles).ThenInclude(d => d.Alimento)`
  sobre `DetallesMenuAsignado` — **igual que el de plantillas**, porque los macros se calculan igual.
- Trae también el nombre del paciente y el `Estado` en la respuesta.

### 2. `ObtenerMenusAsignadosAsync(MenuAsignadoFiltroDto)`
Copia `ObtenerPlantillasAsync`. Cambios en los filtros:
- **`PacienteId`** (obligatorio en el filtro real, pero déjalo `Guid?` y valida tú): es el filtro más
  importante, un nutricionista siempre ve los planes de *un* paciente.
- **`Estado`** (`MenuAsignadoEstado?`): `Borrador` / `Activo` / `Finalizado`.
- **`FechaDesde` / `FechaHasta`** (`DateOnly?`): filtran por `FechaInicio`. Es un rango, igual que
  `CaloriasMinimo/Maximo` en `AlimentoFiltroDto`.
- Quitar `ObjetivoCaloricoMinimo/Maximo` si quieres simplificar, o déjalos.
- El orden dinámico: `nombre`, `fechainicio`, `fechacreacion`.

### 3. `ActualizarMenuAsignadoAsync(Guid, ActualizarMenuAsignadoDto)`
Copia `ActualizarPlantillaAsync` (el reconciliado de detalles por `Id`, añadir/conservar/borrar).
**Tres reglas clínicas adicionales, y son las que más cuidan:**

- **Si `Estado == Finalizado`, rechaza con `ConflictException`** ("un plan finalizado no se modifica").
  Un plan que ya se ejecutó es historia clínica, no borrador.
- **No dejes cambiar `PacienteId`** en la actualización. Cambiar el paciente de un plan existente es
  un error catastrófico de expediente. Si el DTO no trae `PacienteId`, esto ya viene resuelto.
- **No dejes cambiar `FechaInicio` si `Estado != Borrador`**. Solo un borrador puede reprogramarse.

### 4. `RestaurarMenuAsignadoAsync(Guid)`
Copia `RestaurarPlatoAsync` exacto: buscar, `ConflictException` si ya está activo, `IsActive = true`.

### 5. `EliminarMenuAsignadoAsync(Guid)`
Copia `EliminarPlatoAsync`: buscar, `ConflictException` si ya está eliminado, `IsActive = false`.

**Regla extra que sí te toca resolver tú:** un plan **`Activo` no se puede eliminar**, hay que
pasarlo a `Finalizado` primero. Devuelve `ConflictException` con un mensaje que le diga al usuario
eso mismo. (`Borrador` y `Finalizado` sí se pueden eliminar lógicamente.)

---

## 📌 Tarea 2 — `MenuAsignadosController.cs`

**Archivo a crear:** `Controllers/MenuAsignadosController.cs`

```csharp
[ApiController]
[Authorize]
[Route("api/menu-asignados")]
```

**6 endpoints.** El de creación **ya existe en mi servicio**, solo lo expones tú:

| Método | Ruta | Método del servicio |
|---|---|---|
| `POST` | `/api/menu-asignados/asignar` | `AsignarPlantillaAsync([FromBody] CrearMenuAsignadoDto)` → 201 |
| `POST` | `/api/menu-asignados/{menuId:guid}/duplicar` | `DuplicarMenuAsignadoAsync(menuId, dto?.Nombre, dto?.PacienteId)` |
| `GET` | `/api/menu-asignados` | `ObtenerMenusAsignadosAsync([FromQuery] MenuAsignadoFiltroDto)` |
| `GET` | `/api/menu-asignados/{menuId:guid}` | `ObtenerMenuAsignadoPorIdAsync(menuId)` |
| `PUT` | `/api/menu-asignados/{menuId:guid}` | `ActualizarMenuAsignadoAsync(menuId, dto)` |
| `PATCH` | `/api/menu-asignados/{menuId:guid}/restaurar` | `RestaurarMenuAsignadoAsync(menuId)` → `await` + `Ok()` |
| `DELETE` | `/api/menu-asignados/{menuId:guid}` | `EliminarMenuAsignadoAsync(menuId)` → `NoContent()` |

> Para `/duplicar` crea `DuplicarMenuAsignadoDto` con `Nombre?` y `PacienteId?` (mismo molde que
> `DuplicarPlatoDto`). `PacienteId` permite pasar el mismo plan a otro paciente.

**Lo que ya aprendiste y aplica aquí** (los 3 errores que tuviste antes):

- `await servicio.XAsync(id);` + `return Ok();` en el PATCH — el `Task` sin genérico no tiene
  valor que pasar a `Ok(...)`.
- Cierra bien el `}` de las constraints de ruta.
- `[FromBody]` en los DTO que vienen del body, y `dto?.Nombre` donde el DTO es nullable.
- En el `POST`, `CreatedAtAction` necesita el **nombre del parámetro de la ruta** (`menuId`),
  no `id` — si te equivocas devuelve 500.

---

## 📌 Tarea 3 — Los DTOs que faltan

Ya hice `CrearMenuAsignadoDto`, `DetalleMenuAsignadoRespuestaDto` y `MenuAsignadoRespuestaDto`.
Te faltan estos dos, con `PlantillaMenuFiltroDto` y `ActualizarPlantillaMenuDto` como molde:

- `ActualizarMenuAsignadoDto` — copia de `ActualizarPlantillaMenuDto` (nombre, descripción,
  objetivo calórico, detalles), **más `FechaInicio` y `Estado`**. Recuerda: `Estado` no puede ser
  `Finalizado` si el plan ya está finalizado, y `FechaInicio` solo cambia en `Borrador`.
- `MenuAsignadoFiltroDto` — `PacienteId`, `Estado`, `FechaDesde`, `FechaHasta`, `SoloActivos`,
  `OrdenarPor`, `Descendente`.
- `ActualizarDetalleMenuAsignadoDto` — copia de `CrearDetallePlantillaMenuDto`, **con `Id` nullable**.

Validadores en `Validators/`, uno por DTO, copiando los de plantillas. No olvides el de filtro con
el `Must` que valida que el mínimo no supere al máximo.

---

## ❓ Manejo de errores

`GlobalExceptionMiddleware` ya lo convierte a HTTP. **No** pongas `try/catch`:

| Excepción | Status |
|---|---|
| `ValidationException` | 400 |
| `KeyNotFoundException` | 404 |
| `ConflictException` | 409 |

---

## 🔬 Las pruebas del `.http` — para que las entiendas, no como tarea

Antes te las dejaba como tarea. Mejor: te explico **qué prueba cada cosa y por qué importa**, y
cuando el backend compile las corremos juntos.

```bash
cd nutriclinica-backend/nutriclinica-backend
dotnet run --urls "http://localhost:5036"
```

Los user-secrets **solo cargan en Development**, así que no pongas `--environment Production` en local.
Primero `POST /api/auth/login` (el token dura 1 h).

Lo que hay que cubrir, en orden de importancia clínica:

1. **Asignar una plantilla a un paciente** y verificar que los macros cuadren. Aquí se prueba el
   clonado: si cambias el plato del catálogo después, el plan del paciente **no debe cambiar**.
2. **Editar porciones de un plan** (`PUT`) — el nutricionista ajusta cantidad de un plato porque el
   paciente no bajó de peso. Verifica que `CaloriasTotales` se recalcule.
3. **Cambiar un plato por otro** en el mismo día/tiempo — simula una alergia detectada en consulta.
4. **`Estado = Finalizado` y luego intentar editar** → debe responder `409`. Esta es la prueba de
   seguridad del expediente clínico.
5. **Eliminar un plan `Activo`** → `409` con mensaje de "pásalo a finalizado primero".
6. **Filtro por paciente** — que solo salgan los planes de ese paciente, nunca los de otro.
7. **Filtro por rango de fechas** — `?fechaDesde=2026-10-14&fechaHasta=2026-10-20`.
8. **Borrar y restaurar** — el borrado es lógico, el plan debe volver con `PATCH /restaurar`.
9. **401 sin token** y **404 con un GUID inventado** — que nadie lea expedientes ajenos.

Cuando quieras, arrancamos el backend y lo vas ejecutando tú mientras yo te voy diciendo qué
esperas ver en cada respuesta. Eso te deja claro el módulo sin que tengas que copiar pruebas a mano.

---

## ⚠️ Recordatorio de git

Solo `git commit`. **No hacer `git push`** hasta que yo lo indique.

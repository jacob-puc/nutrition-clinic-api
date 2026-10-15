# Tareas asignadas — Terminar el módulo de Menús Asignados

## Contexto — qué estado tiene el módulo

El módulo **`MenuAsignado`** es el plan de comidas de **un paciente en concreto**, con fecha de
inicio. Aquí está el 80 % hecho. Tu tarea ya **no es construir el servicio**, es **terminarlo**.

### ✅ Ya está hecho (no tocar)

| Parte | Quién | Estado |
|---|---|---|
| Enum `MenuAsignadoEstado`, entidades, config EF, migración aplicada | yo | ✅ |
| DTOs: `Crear`, `Actualizar`, `Detalle` (respuesta y de actualización), `Filtro`, `Duplicar` | **tú** | ✅ |
| Validadores: `Crear`, `Actualizar`, `ActualizarDetalles`, `Filtro` | yo (2) + tú (1) | ✅ |
| `AsignarPlantillaAsync` y `DuplicarMenuAsignadoAsync` | yo | ✅ |
| `ObtenerMenuAsignadoPorIdAsync` | tú | ✅ |
| `ObtenerMenuAsignadoAsync` (filtros + orden) | yo | ✅ |
| `ActualizarMenuAsignadoAsync` (reconciliado de detalles + reglas clínicas) | yo | ✅ |

### ⚠️ Decisión de dominio CAMBIADA: ya NO hay clonado en profundidad

Antes al asignar una plantilla se copiaban los platos del catálogo. **Se quitó** porque las copias
no tenían ninguna marca en `Plato` y se mezclaban con el catálogo en `GET /api/platos`, y porque
`DuplicarPlantillaAsync` nunca copió platos — eran dos filosofías distintas sin motivo.

**Ahora `DetalleMenuAsignado.PlatoId` apunta al plato del catálogo, directo.** Las consecuencias:

- Si el nutricionista corrige un plato del catálogo, **todos los planes que lo usan se actualizan**.
  Eso está bien: corregir una receta errónea es una decisión clínica que aplica a todos.
- La personalización por paciente **no se pierde**: `Cantidad` (porciones) e `Indicaciones`
  ("sin sal") viven en `DetalleMenuAsignado`, que es **por paciente**.
- Si un paciente necesita una receta distinta, se crea otro plato variante en el catálogo.
- Agregué bloqueo en `PlatoService.EliminarPlatoAsync`: un plato en el plan activo de un paciente
  devuelve **409**.

---

## 📌 Tarea 1 — Terminar `MenuAsignadoService.cs` (2 métodos)

**El servicio está al 90 %.** El compilador falla con **exactamente 2 errores CS0535**, y son los 2
métodos que te faltan. Cuando `dotnet build` pase a 0 errores, terminaste.

**No toques** los métodos existentes (`AsignarPlantillaAsync`, `DuplicarMenuAsignadoAsync`,
`ObtenerMenuAsignadoAsync`, `ActualizarMenuAsignadoAsync`, `CargarMenuAsync`, `MapearMenu` ni los
privados `Validar*`).

Abre `PlantillaMenuService.cs` al lado: **es tu plantilla literal**.

### 1. `RestaurarMenuAsignadoAsync(Guid menuId)`

Copia literal de `RestaurarPlantillaAsync` (línea 275). Tres cambios:

- Busca en `_context.MenusAsignados`.
- Mensaje: `"El menú asignado ya está activo."`
- `Task` sin retorno: `await _context.SaveChangesAsync();` y nada más.

### 2. `EliminarMenuAsignadoAsync(Guid menuId)`

Copia de `EliminarPlantillaAsync` (línea 290), **más una regla clínica** que sí te toca resolver:

```csharp
// después de cargar `menu` y de verificar que ya no esté eliminado:
if (menu.Estado == MenuAsignadoEstado.Activo)
{
    throw new ConflictException(
        "No se puede eliminar un plan activo. Pásalo a Finalizado primero.");
}
```

Luego `menu.IsActive = false;` + `SaveChangesAsync`.

> `Borrador` y `Finalizado` sí se pueden eliminar lógicamente. Solo `Activo` está bloqueado.

---

## 📌 Tarea 2 — `MenuAsignadosController.cs`

**Archivo a crear:** `Controllers/MenuAsignadosController.cs`

```csharp
[ApiController]
[Authorize]
[Route("api/menu-asignados")]
```

**7 endpoints.** Los 5 primeros ya existen en el servicio, solo los expones tú; los 2 últimos son
los métodos de la Tarea 1:

| Método | Ruta | Método del servicio | Respuesta |
|---|---|---|---|
| `POST` | `/api/menu-asignados/asignar` | `AsignarPlantillaAsync([FromBody] CrearMenuAsignadoDto)` | 201 + `CreatedAtAction` |
| `POST` | `/{menuId:guid}/duplicar` | `DuplicarMenuAsignadoAsync(menuId, dto?.Nombre, dto?.PacienteId)` | 201 |
| `GET` | `/api/menu-asignados` | `ObtenerMenuAsignadoAsync([FromQuery] MenuAsignadoFiltroDto)` | 200 |
| `GET` | `/{menuId:guid}` | `ObtenerMenuAsignadoPorIdAsync(menuId)` | 200 |
| `PUT` | `/{menuId:guid}` | `ActualizarMenuAsignadoAsync(menuId, dto)` | 200 |
| `PATCH` | `/{menuId:guid}/restaurar` | `RestaurarMenuAsignadoAsync(menuId)` → `await` + `Ok()` | 200 |
| `DELETE` | `/{menuId:guid}` | `EliminarMenuAsignadoAsync(menuId)` | 204 |

Molde literal: `Controllers/PlantillasMenuController.cs`.

**Los 4 errores que ya aprendiste y aplican aquí:**

- `await servicio.XAsync(id);` + `return Ok();` en el PATCH — el `Task` sin genérico no tiene valor.
- Cierra bien el `}` de los constraints de ruta.
- `[FromBody]` en los DTO del body, `dto?.Nombre` donde el DTO es nullable.
- `CreatedAtAction` necesita el **nombre del parámetro de la ruta** (`menuId`), no `id`.

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

Cuando compile con 0 errores, arrancamos juntos:

```bash
cd nutriclinica-backend/nutriclinica-backend
dotnet run --urls "http://localhost:5036"
```

Los user-secrets **solo cargan en Development**. Primero `POST /api/auth/login` (el token dura 1 h;
el campo es `correoElectronico`).

Lo que hay que cubrir, en orden de importancia clínica:

1. **Asignar una plantilla a un paciente** y verificar que los macros cuadren. Y ahora, con el
   cambio de decisión: si cambias un plato del catálogo, **sí debe actualizarse** en el plan del
   paciente (es lo esperado, al revés que antes).
2. **Editar porciones de un plan** (`PUT`) — verificar que `CaloriasTotales` se recalcule.
3. **Cambiar un plato por otro** en el mismo día/tiempo — simula una alergia detectada.
4. **`Estado = Finalizado` y luego intentar editar** → debe responder `409`.
5. **Eliminar un plan `Activo`** → `409` con mensaje de "pásalo a finalizado primero".
6. **Filtro por paciente** — solo los planes de ese paciente.
7. **Filtro por rango de fechas** — `?fechaDesde=2026-10-14&fechaHasta=2026-10-20`.
8. **Borrar y restaurar** — borrado lógico, `PATCH /restaurar`.
9. **401 sin token** y **404 con un GUID inventado**.

Tú lo vas ejecutando, yo te digo qué esperas ver en cada respuesta.

---

## ⚠️ Recordatorio de git

Solo `git commit`. **No hacer `git push`** hasta que yo lo indique.

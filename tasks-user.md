# Tareas asignadas — Módulo de Alimentos

## Contexto

El módulo `ALIMENTOS` (catálogo de alimentos) ya tiene la base lista y compilando.
Mi parte (arquitectura y lógica) está hecha. Esta es **tu parte**: el controller
y las pruebas manuales.

---

## ✅ Lo que ya quedó listo (no tocar)

| Archivo | Qué hace |
|---|---|
| `Core/Entities/Alimento.cs` | Entidad con auditoría (`FechaCreacion`, `IsActive`) |
| `Infrastructure/Persistence/ApplicationDbContext.cs` | DbSet + configuración y `NombreNormalizado` |
| `Migrations/20261009174116_AgregarCatalogoAlimentos.cs` | Tabla `Alimentos`, **ya aplicada** |
| `Features/Alimentos/DTOs/*.cs` | `CrearAlimentoDto`, `ActualizarAlimentoDto`, `AlimentoRespuestaDto`, `AlimentoFiltroDto` |
| `Features/Alimentos/DTOs/Validators/*.cs` | Validaciones de FluentValidation |
| `Features/Alimentos/Interfaces/IAlimentoService.cs` | Contrato del servicio |
| `Features/Alimentos/Services/AlimentoService.cs` | Toda la lógica (abajo detallo) |
| `Program.cs` | Registro de `IAlimentoService` en DI |

**Decisiones ya tomadas — respétalas:**

- Los macros (`Calorias`, `Proteinas`, `Carbohidratos`, `Grasas`) van **por porción de 100 g**.
- `Nombre` se guarda tal cual, pero se calcula `NombreNormalizado` (sin acentos, minúsculas)
  para poder detectar duplicados. **No agregues esa columna al DTO de respuesta.**
- El borrado es **lógico**: `EliminarAlimentoAsync` pone `IsActive = false`, nunca borra la fila.
  Por eso existe `RestaurarAlimentoAsync`.
- `AlimentoRespuestaDto` trae dos campos calculados que **no se envían en requests**:
  `CaloriasEstimadas` y `DiferenciaCalorica` (detallados abajo).

---

## 📌 Tarea 1 — Crear `AlimentosController.cs`

**Archivo a crear:**
`nutriclinica-backend/Controllers/AlimentosController.cs`

**Qué hacer:** CRUD completo inyectando `IAlimentoService`.
Copia la estructura de `Controllers/CitasController.cs` (misma forma, mismos atributos).

```csharp
[ApiController]
[Authorize]
[Route("api/alimentos")]
```

**Endpoints a exponer (6):**

| Método | Ruta | Método del servicio |
|---|---|---|
| `GET` | `/api/alimentos` | `ObtenerAlimentosAsync([FromQuery] AlimentoFiltroDto filtro)` |
| `GET` | `/api/alimentos/{alimentoId:guid}` | `ObtenerAlimentoPorIdAsync(alimentoId)` |
| `POST` | `/api/alimentos` | `CrearAlimentoAsync([FromBody] CrearAlimentoDto dto)` |
| `PUT` | `/api/alimentos/{alimentoId:guid}` | `ActualizarAlimentoAsync(alimentoId, dto)` |
| `PATCH` | `/api/alimentos/{alimentoId:guid}/restaurar` | `RestaurarAlimentoAsync(alimentoId)` |
| `DELETE` | `/api/alimentos/{alimentoId:guid}` | `EliminarAlimentoAsync(alimentoId)` |

**Detalles que no olvides:**

- `POST` debe devolver `CreatedAtAction(nameof(ObtenerAlimentoPorId), new { alimentoId = alimento.Id }, alimento)`
  igual que hace `CitasController.CrearCita`.
- `DELETE` y `PATCH` devuelven `NoContent()` / `Ok(...)` respectivamente.
- `GET` sin parámetros ya funciona: el filtro llega vacío y el servicio usa valores por defecto.
- No necesitas mapear nada a mano: **el servicio ya devuelve DTOs**, el controller solo delega.

---

## 📌 Tarea 2 — Agregar pruebas al `.http`

**Archivo a editar:** `nutriclinica-backend/nutriclinica-backend.http`

Agrega un bloque con estas peticiones (usa `{{token}}` que ya existe en el archivo):

1. `POST /api/alimentos` — crea un alimento válido (ej. pollo a la plancha).
2. `GET /api/alimentos` — lista, debe devolver el recién creado.
3. `GET /api/alimentos?buscar=pollo` — búsqueda por texto.
4. `GET /api/alimentos?ordenarPor=calorias&descendente=true` — orden descendente.
5. `GET /api/alimentos/{id}` — detalle.
6. `POST /api/alimentos` con **el mismo nombre** — debe responder `409`.
7. `POST /api/alimentos` con `calorias = -5` — debe responder `400`.
8. `DELETE /api/alimentos/{id}` y luego `GET /api/alimentos?buscar=pollo` — ya no aparece.
9. `PATCH /api/alimentos/{id}/restaurar` y luego `GET` — vuelve a aparecer.

---

## 📌 Tarea 3 (opcional, si te sobra tiempo)

Conecta el campo `ImagenUrl` al bucket de Supabase Storage que ya existe
(`nutriclinica-media`, público, 5 MB):

- Nuevo endpoint: `POST /api/alimentos/{alimentoId:guid}/imagen` (`[FromForm] IFormFile archivo`).
- Reutiliza `IStorageService` que ya está inyectado en el proyecto.
- Usa la ruta `alimentos/{alimentoId}/{Guid}.jpg` dentro del bucket.
- Llama a `ActualizarAlimentoAsync` para guardar la nueva URL.

Si no llegas, no pasa nada: el campo `ImagenUrl` ya acepta una URL directa por `POST`/`PUT`,
así que se puede usar el módulo sin esto.

---

## 🧪 Cómo probar todo

El backend debe estar corriendo:

```bash
cd nutriclinica-backend/nutriclinica-backend
dotnet run --urls "http://localhost:5199"
```

1. Login en `POST /api/auth/login` y copia el `accessToken` (expira en 1 h).
2. Ejecuta las peticiones del bloque nuevo del `.http`.
3. Verifica que `409` y `400` salgan con el formato del proyecto:

```json
{
  "codigo": "VALIDACION",
  "mensaje": "Se encontraron errores de validación",
  "errores": [ { "campo": "Calorias", "mensaje": "..." } ]
}
```

4. **No hagas `git push`** hasta que yo lo indique. Solo `git commit`.

---

## ❓ Si algo no cuadra

Los errores del servicio ya los maneja `GlobalExceptionMiddleware`, así que **no** agregues
`try/catch` en el controller:

| Excepción | Status |
|---|---|
| `ValidationException` (FluentValidation) | 400 |
| `KeyNotFoundException` (no existe el ID) | 404 |
| `ConflictException` (nombre duplicado / ya eliminado) | 409 |

Si un endpoint te responde algo raro, escríbeme antes de "arreglarlo" a tu manera.

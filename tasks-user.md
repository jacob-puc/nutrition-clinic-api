# Tareas asignadas — Módulo de Plantillas de Menús (capa PLATO)

## Contexto

El módulo de menús ya tiene la base lista y compilando. Como te comenté, cambié tu idea
original (`DetallePlantillaMenu → Alimento`) por una capa intermedia de **PLATOS**:

```
PlantillaMenu ──< DetallePlantillaMenu >── Plato ──< DetallePlato >── Alimento
   (semana)         (día + tiempo)        (receta)    (gramajes)      (catálogo)
```

¿Por qué? Porque en la vida real un menú clínico no repite "arroz 150 g, pollo 200 g, ensala 100 g"
en cada día: se define **un plato** una vez y se reutiliza en toda la semana. Además así puedes
sustituir un plato completo si el paciente es alérgico, sin tocar el resto.

Mi parte (entidades, DTOs, validadores, servicios) está hecha. Esta es **tu parte**: los dos
controllers y las pruebas manuales.

---

## ✅ Lo que ya quedó listo (no tocar)

| Archivo | Qué hace |
|---|---|
| `Core/Enums/DiaSemana.cs` | `Lunes=1 … Domingo=7` |
| `Core/Enums/TiempoComida.cs` | `Desayuno=1 … Colacion=6` |
| `Core/Entities/Plato.cs`, `DetallePlato.cs` | Plato (receta) y sus ingredientes |
| `Core/Entities/PlantillaMenu.cs`, `DetallePlantillaMenu.cs` | Semana y su agenda |
| `Infrastructure/Persistence/ApplicationDbContext.cs` | `DbSet` + configuración, FKs, check constraints |
| `Migrations/20261010215328_AgregarPlantillasMenu.cs` | Tablas nuevas, **ya aplicada** |
| `Features/Menus/DTOs/*.cs` | 14 DTOs de creación, respuesta, filtro y duplicado |
| `Features/Menus/DTOs/Validators/*.cs` | FluentValidation |
| `Features/Menus/Interfaces/*.cs` | `IPlatoService`, `IPlantillaMenuService` |
| `Features/Menus/Services/*.cs` | Toda la lógica (abajo detallo) |
| `Program.cs` | Ambos servicios registrados en DI |

**Decisiones ya tomadas — respétalas:**

- Los macros del menú se **calculan**, no se guardan: el servicio suma
  `Alimento.Calorias * RacionGramos / 100` por ingrediente, luego `* Cantidad` por plato.
- El borrado es **lógico** en las 4 entidades (`IsActive = false`) y existe `Restaurar`.
- Un plato que está asignado a una plantilla **no se puede eliminar** → responde `409`.
- `NutricionistaId` es **nullable**: `null` = catálogo compartido, con valor = personal de ese nutricionista.
  **No** existe campo `EsPersonalizado`.
- `PlantillaMenuRespuestaDto` trae campos calculados que **no se envían en requests**:
  `CaloriasTotales`, `ProteinasTotales`, `CarbohidratosTotales`, `GrasasTotales`,
  `DesviacionCalorica` y `ResumenPorTiempoComida`.

---

## 📌 Tarea 1 — Crear `PlatosController.cs`

**Archivo a crear:** `nutriclinica-backend/Controllers/PlatosController.cs`

Copia la estructura de `Controllers/AlimentosController.cs` (misma forma, mismos atributos):

```csharp
[ApiController]
[Authorize]
[Route("api/platos")]
```

**Endpoints a exponer (7):**

| Método | Ruta | Método del servicio |
|---|---|---|
| `GET` | `/api/platos` | `ObtenerPlatosAsync([FromQuery] PlatoFiltroDto filtro)` |
| `GET` | `/api/platos/{platoId:guid}` | `ObtenerPlatoPorIdAsync(platoId)` |
| `POST` | `/api/platos` | `CrearPlatoAsync([FromBody] CrearPlatoDto dto)` |
| `PUT` | `/api/platos/{platoId:guid}` | `ActualizarPlatoAsync(platoId, dto)` |
| `POST` | `/api/platos/{platoId:guid}/duplicar` | `DuplicarPlatoAsync(platoId, [FromBody] DuplicarPlatoDto? dto)` |
| `PATCH` | `/api/platos/{platoId:guid}/restaurar` | `RestaurarPlatoAsync(platoId)` |
| `DELETE` | `/api/platos/{platoId:guid}` | `EliminarPlatoAsync(platoId)` |

> **Atención con `/duplicar`**: la firma del servicio es `DuplicarPlatoAsync(Guid platoId, string? nombreNuevo)`.
> Ya creé el DTO `DuplicarPlatoDto.cs` (una sola propiedad `Nombre`). En el controller recibe
> `[FromBody] DuplicarPlatoDto dto` y pasas `dto.Nombre`. Si el body viene vacío, manda `null`
> y el servicio le pone `"Nombre (copia)"` solo.

**No olvides:**

- `POST` devuelve `CreatedAtAction(nameof(ObtenerPlatoPorId), new { platoId = plato.Id }, plato)`.
- `DELETE` → `NoContent()`, `PATCH .../restaurar` → `Ok(...)`.
- El servicio ya devuelve el DTO armado: **no mapees nada a mano**.

---

## 📌 Tarea 2 — Crear `PlantillasMenuController.cs`

**Archivo a crear:** `nutriclinica-backend/Controllers/PlantillasMenuController.cs`

```csharp
[ApiController]
[Authorize]
[Route("api/plantillas-menu")]
```

**Endpoints a exponer (7):**

| Método | Ruta | Método del servicio |
|---|---|---|
| `GET` | `/api/plantillas-menu` | `ObtenerPlantillasAsync([FromQuery] PlantillaMenuFiltroDto filtro)` |
| `GET` | `/api/plantillas-menu/{plantillaId:guid}` | `ObtenerPlantillaPorIdAsync(plantillaId)` |
| `POST` | `/api/plantillas-menu` | `CrearPlantillaAsync([FromBody] CrearPlantillaMenuDto dto)` |
| `PUT` | `/api/plantillas-menu/{plantillaId:guid}` | `ActualizarPlantillaAsync(plantillaId, dto)` |
| `POST` | `/api/plantillas-menu/{plantillaId:guid}/duplicar` | `DuplicarPlantillaAsync(plantillaId, dto?.Nombre)` |
| `PATCH` | `/api/plantillas-menu/{plantillaId:guid}/restaurar` | `RestaurarPlantillaAsync(plantillaId)` |
| `DELETE` | `/api/plantillas-menu/{plantillaId:guid}` | `EliminarPlantillaAsync(plantillaId)` |

Usa `DuplicarPlantillaMenuDto` (ya creado, misma forma: solo `Nombre`).

---

## 📌 Tarea 3 — Agregar pruebas al `.http`

**Archivo a editar:** `nutriclinica-backend/nutriclinica-backend.http`

Agrega un bloque nuevo (usa `{{token}}` que ya existe). Necesitas IDs de alimento reales: haz
un `GET /api/alimentos` antes y copia dos IDs.

**Bloque A — Platos (9 pruebas):**

1. `POST /api/platos` — plato simple: "Ensalada César", 1 alimento (lechuga, 100 g).
2. `POST /api/platos` — plato compuesto: "Pollo con arroz" con 3 alimentos (pollo 200 g, arroz 150 g, brócoli 100 g).
3. `GET /api/platos` — debe devolver ambos.
4. `GET /api/platos/{id}` — verifica que `CaloriasTotales` sea la suma correcta (revisa el `Detalles` con sus macros por línea).
5. `GET /api/platos?buscar=pollo` — búsqueda.
6. `GET /api/platos?soloCompuestos=true` — solo debe salir "Pollo con arroz".
7. `POST /api/platos` con `racionGramos = 0` — debe responder `400`.
8. `POST /api/platos` con el **mismo alimento dos veces** — debe responder `400` (mensaje de "suma sus gramajes").
9. `POST /api/platos/{id}/duplicar` con body `{ "nombre": "Pollo con arroz v2" }` — debe crear una copia.

**Bloque B — Plantillas de menú (10 pruebas):**

10. `POST /api/plantillas-menu` — "Semana estándar 2000 kcal" con 4 detalles usando los platos anteriores:
    - Lunes / Desayuno / Ensalada César / cantidad 1
    - Lunes / Almuerzo / Pollo con arroz / cantidad 1
    - Martes / Almuerzo / Pollo con arroz / cantidad 1
    - Martes / Cena / Ensalada César / cantidad 0.5
11. `GET /api/plantillas-menu` — lista.
12. `GET /api/plantillas-menu/{id}` — revisa `CaloriasTotales`, `DesviacionCalorica` (total − 2000) y `ResumenPorTiempoComida`.
13. `GET /api/plantillas-menu?diaSemana=Lunes` — solo la semana que tiene lunes.
14. `GET /api/plantillas-menu?tiempoComida=Almuerzo`.
15. `GET /api/plantillas-menu?objetivoCaloricoMinimo=1500&objetivoCaloricoMaximo=2500`.
16. `POST /api/plantillas-menu` con el **mismo plato dos veces en Lunes/Almuerzo** — debe responder `400`.
17. `POST /api/plantillas-menu` con `cantidad = 0` — debe responder `400`.
18. `DELETE /api/platos/{id}` de un plato **usado en una plantilla** — debe responder `409`.
19. `DELETE /api/plantillas-menu/{id}` y luego `GET` — ya no aparece; `PATCH .../restaurar` lo devuelve.

---

## 🧪 Cómo probar todo

```bash
cd nutriclinica-backend/nutriclinica-backend
dotnet run --urls "http://localhost:5036"
```

1. Recuerda que los **user-secrets solo cargan en Development**, así que no pongas `--environment Production` en local.
2. Login en `POST /api/auth/login` y copia el `accessToken` (expira en 1 h).
3. Ejecuta los bloques A y B en orden (el B depende de los IDs del A).

---

## ❓ Si algo no cuadra

Los errores del servicio ya los maneja `GlobalExceptionMiddleware`, así que **no** agregues
`try/catch` en el controller:

| Excepción | Status |
|---|---|
| `ValidationException` (FluentValidation) | 400 |
| `KeyNotFoundException` (ID no existe / plato o alimento inexistente) | 404 |
| `ConflictException` (plato en uso, ya eliminado, ya activo) | 409 |

Si un endpoint te responde algo raro, escríbeme antes de "arreglarlo" a tu manera.

**No hagas `git push`** hasta que yo lo indique. Solo `git commit`.

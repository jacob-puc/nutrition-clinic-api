# Menús Asignados — módulo COMPLETO (faltan pruebas .http en conjunto)

## ✅ Estado: código terminado

Build limpio: **0 errores, 0 advertencias** (`dotnet build` desde `nutriclinica-backend/nutriclinica-backend`).

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

## 🔬 Siguiente paso — pruebas `.http` (juntos, no es tarea tuya)

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

Escenarios con valor clínico (te digo qué esperar en cada respuesta):

1. Asignar plantilla a paciente → macros cuadran. Cambiar el plato en el catálogo → **sí**
   debe reflejarse en el plan (sin clonado, es lo esperado).
2. `PUT` ajustando porciones → `CaloriasTotales` recalculado.
3. Cambiar un plato en un día/tiempo → simula alergia detectada.
4. Poner `Estado = Finalizado` y luego `PUT` → **409**.
5. `DELETE` de un plan `Activo` → **409**; de uno `Finalizado` → 204; `PATCH /restaurar` → 200.
6. Filtro por `pacienteId` → solo ese paciente. Rango de fechas → solo ese rango.
7. 401 sin token, 404 con GUID inventado.

---

## ⚠️ Recordatorio de git

Solo `git commit`. **No hacer `git push`** hasta que yo lo indique.

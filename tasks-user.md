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

## ⚠️ Recordatorio de git

Solo `git commit`. **No hacer `git push`** hasta que yo lo indique.
(Último push: **2026-10-16**, commits `deb798a..cbb29fb`.)

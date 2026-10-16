# Progreso del Proyecto NutriClínica (Backend)

## Estado Actual: **API desplegada, Auth completo, Alimentos completo, módulo Menús en curso**

**Última actualización:** 2026-10-16

---

## Resumen Ejecutivo

- ✅ **API (.NET 10)** desplegada en Render: `https://nutrition-clinic-api.onrender.com`
- ✅ **Autenticación JWT completa** (login, refresh, logout, guard)
- ✅ **Upload de archivos** (fotos/documentos) vía Supabase Storage
- ✅ **Catálogo de Alimentos** completo (24/24 pruebas manuales OK)
- 🔄 **Plantillas de Menús** con capa `PLATO`: 14 endpoints funcionando y verificados
- ✅ **Menús Asignados**: **módulo completo y verificado en vivo** (pruebas 2026-10-16:
  7 endpoints + reglas clínicas + filtros + auth, todo OK)
- ✅ **Base de datos:** Supabase (PostgreSQL) en `us-west-2`
- ✅ **Despliegue:** Render (free tier)
- ✅ **Repositorio:** https://github.com/jacob-puc/nutrition-clinic-api

---

## Backend (nutriclinica-backend)

### Módulos implementados (9 features, 61 endpoints)
| Módulo | Entidad | Endpoints | Estado |
|---|---|---|---|
| Pacientes | Paciente | 7 (CRUD + expediente + objetivo) | ✅ |
| Historiales Clínicos | HistorialClinico | 2 | ✅ |
| Antropometría | MedidaAntropometrica | 5 | ✅ |
| Expediente Media | FotoSeguimiento, DocumentoPaciente | 12 (CRUD + subida) | ✅ |
| Nutricionistas | Nutricionista | 5 | ✅ |
| Citas | Cita | 6 | ✅ |
| Consultas | Consulta | 8 | ✅ |
| Alimentos | Alimento | 6 (CRUD + restaurar) + imagen | ✅ 24/24 pruebas |
| Menús | Plato, PlantillaMenu | 14 (2 controllers) | ✅ Verificado en vivo |
| Menús Asignados | MenuAsignado | 7 (controller del usuario) | ✅ Verificado en vivo (2026-10-16) |

### Módulo de Menús — capa PLATO

Se descartó la idea original `DetallePlantillaMenu → Alimento` y se insertó una capa intermedia:

```
PlantillaMenu ──< DetallePlantillaMenu >── Plato ──< DetallePlato >── Alimento
   (semana)         (día + tiempo)        (receta)    (gramajes)      (catálogo)
```

Motivo: un menú clínico son **platillos compuestos** reutilizables en toda la semana, y permite
sustituir un plato entero si el paciente es alérgico sin tocar el resto de la agenda.

- **Enums**: `DiaSemana` (Lunes=1..Domingo=7), `TiempoComida` (Desayuno=1..Colacion=6),
  almacenados como `string` con `HasConversion<string>()` + `CHECK` constraints.
- **Migración** `20261010215328_AgregarPlantillasMenu` **aplicada**.
- **Macros calculados, no persistidos**: `Alimento.Calorias * RacionGramos / 100`, luego `* Cantidad`.
- **`PlantillaMenuRespuestaDto`** trae `DesviacionCalorica` (total − objetivo) y
  `ResumenPorTiempoComida` (agrupado por día + tiempo).
- **Borrado lógico** en las 4 entidades; un plato en uso por una plantilla devuelve `409`.
- **`NutricionistaId` nullable** = catálogo compartido (`null`) vs personal del nutricionista.
- **`DuplicarPlatoAsync` / `DuplicarPlantillaAsync`**: clonado en profundidad (copia receta o
  semana; **los platos siempre se referencian, nunca se copian**, revisado 2026-10-15).
- **Estado final**: los 14 endpoints funcionando y verificados en vivo (el usuario corrigió el
  `CreatedAtAction` que devolvía 500 en `POST /api/plantillas-menu`).

### Módulo de Menús Asignados — el plan del paciente

`MenuAsignado` es el plan de comidas **de un paciente concreto**, con fecha de inicio real.

- **Migración** `20261014201651_AgregarMenusAsignados` **aplicada** (tablas `MenusAsignados`,
  `DetallesMenuAsignado`).
- **Enum `MenuAsignadoEstado`**: `Borrador` → `Activo` → `Finalizado`.
- **Sin clonado de platos** (decisión revisada 2026-10-15): `DetalleMenuAsignado.PlatoId` apunta al
  plato del catálogo. Se quitó el clonado porque las copias no llevaban marca en `Plato` y se
  mezclaban con el catálogo en `GET /api/platos`, y porque `DuplicarPlantillaAsync` nunca copió
  platos. Si se corrige un plato del catálogo, los planes que lo usan **se actualizan**; la
  personalización por paciente vive en `Cantidad` e `Indicaciones` del detalle.
- **`DuplicarMenuAsignadoAsync`**: copia un plan existente, opcionalmente a otro paciente, y lo
  deja en `Borrador`.
- **Reglas clínicas** (todas verificadas en vivo 2026-10-16): un plan `Finalizado` no se edita
  (409); un plan `Activo` no se elimina (409); `PacienteId` no se cambia (el DTO no lo trae);
  `FechaInicio` solo cambia en `Borrador` (409); `Restaurar` reactiva soft-delete (409 si ya
  estaba activo).
- **`PlatoService.EliminarPlatoAsync`**: además del check de plantillas, devuelve **409** si el
  plato está en el plan activo de un paciente.
- **Bug corregido 2026-10-16**: `ActualizarPlatoAsync` daba **500**
  (`DbUpdateConcurrencyException`) al reemplazar ingredientes sin enviar `Id` — el `AddRange` de
  detalles nuevos estaba tras una condición que nunca se cumplía (`!eliminados.Any() && !IsLoaded`);
  EF adjuntaba las entidades nuevas como `Modified` → `UPDATE` con GUID inexistente → 0 filas.
  Corregido con `AddRange(conservados.Where(c => !existentes.ContainsKey(c.Id)))` (mismo patrón
  que `PlantillaMenuService` y `MenuAsignadoService`).
- **Servicio completo**: `ObtenerPorId`, `ObtenerLista` (filtros `PacienteId`, `Estado`,
  `FechaDesde/Hasta`, `SoloActivos`, orden `nombre|fechainicio|fechacreacion`), `Actualizar`
  (reconciliado de detalles + reglas clínicas), `Restaurar` y `Eliminar` (escritos por el usuario).
- **Controller `MenuAsignadosController`** (7 endpoints) escrito y corregido por el usuario
  (ruta relativa `asignar`, `dto?.Nombre`, `ActionResult` en PATCH).
- **Estado**: módulo **COMPLETO y VERIFICADO en vivo** (2026-10-16), build **0/0**. Pruebas
  cubrieron: asignar (201 + macros), sin-clonado (editar plato del catálogo → el plan se
  actualiza solo), recálculo de porciones, cambiar plato (alergia), duplicar, filtros, 9/9
  reglas clínicas, 401/404/409/400. **Pregunta abierta**: `DELETE plantilla` en uso por planes
  activos hoy devuelve 204 (sin check) — decidir si va 409 como el plato.

### Upload de archivos
- **Almacenamiento**: Supabase Storage, consumido por su REST API directa
  (el cliente NuGet `Supabase` fue descartado: fallaba en reflexión por dependencias
  `Supabase.Core` / `Gotrue` / `Postgrest`)
- **Dos buckets**:

  | Bucket | Contenido | Acceso | Tamaño máx. | MIME |
  |---|---|---|---|---|
  | `nutriclinica-media` | Imágenes de pacientes y de alimentos (módulo futuro) | público | 5 MB | jpeg, png, webp |
  | `nutriclinica-docs` | Documentos del paciente | **privado** | 10 MB | pdf, jpeg, png, doc, docx |

- **Endpoints**: `POST /api/pacientes/{id}/fotos/subir`, `POST /api/pacientes/{id}/documentos/subir`
  (multipart/form-data, `[RequestSizeLimit]` por endpoint)
- **URLs**: las fotos guardan la **URL pública**; los documentos guardan la **ruta** en BD y la API
  devuelve una **URL firmada** de 1 hora en cada lectura
- **Ruta en Storage**: `{pacienteId}/fotos|documentos/{Guid}.{ext}`
- **Sin migración de BD**: las entidades ya guardan `UrlFoto` / `UrlDocumento`
- **Configuración opcional**: si faltan las credenciales, el resto de la API sigue funcionando y
  el endpoint responde `503 STORAGE_NO_CONFIGURADO` con mensaje claro
- **Variables de entorno**: `Supabase__Url` (pre-llenada), `Supabase__StorageKey` (service role key,
  **secreta** — solo user-secrets / env vars), `Supabase__BucketMedia`, `Supabase__BucketDocumentos`
- **Verificado end-to-end**: subida real a ambos buckets y descarga HTTP 200; archivo de 6 MB → 400

### Autenticación
- **JWT HS256**: access token 1h, refresh token 7 días (opaco)
- **Hash**: `PasswordHasher<T>` (PBKDF2, sal por usuario)
- **Endpoints**: `POST /api/auth/login`, `/refresh`, `/logout`, `GET /api/auth/yo`
- **Protección**: `[Authorize]` en 10 controllers, `UnauthorizedAccessException` → 401
- **Seed**: Admin por variables de entorno (`Seed__AdminCorreo`, `Seed__AdminContrasena`)

### Configuración de despliegue
- **Dockerfile** multi-stage (SDK 10 → ASP.NET 10, puerto 8080, `libgssapi-krb5-2`)
- **Render**: Auto-deploy desde `main`, variables de entorno en dashboard
- **Variables críticas**: `ConnectionStrings__DefaultConnection`, `Jwt__Secret`, `Jwt__Issuer`, `Jwt__Audience`, `Cors__AllowedOrigins__0`, `Seed__*`
- **Health check**: `GET /health` → `200 Healthy`

### Base de datos
- **Supabase** (pooler `aws-1-us-west-2.pooler.supabase.com:5432`)
- Migración `20261003184724_AgregarAutenticacionNutricionistas` (PasswordHash, RefreshToken, RefreshTokenExpiresAt)
- Migración `20261009174116_AgregarCatalogoAlimentos` (tabla `Alimentos`)
- Migración `20261010215328_AgregarPlantillasMenu` (tablas `Platos`, `DetallesPlato`, `PlantillasMenu`, `DetallesPlantillaMenu`)
- Migración `20261014201651_AgregarMenusAsignados` (tablas `MenusAsignados`, `DetallesMenuAsignado`)
- Connection string solo por variable de entorno
- `MaxPoolSize=5` en código para no agotar pooler

---

## Frontend (referencia únicamente)
- **Proyecto separado**: `../nutriclinica-web` (React + Vite + TS)
- **Ya implementado**: Auth completo (login, interceptor, guard, refresh automático)
- **Variables**: `VITE_API_URL_LOCAL`, `VITE_API_URL_PUBLIC`, `VITE_API_URL` activa
- **Credenciales admin**: `admin@nutriclinica.cl` / `NutriClinica2026`
- **Cambio de entorno**: Cambiar `VITE_API_URL` en `.env` y `npm run dev`

---

## Despliegue Actual

| Componente | Plataforma | URL | Estado |
|---|---|---|---|
| API | Render | https://nutrition-clinic-api.onrender.com | ✅ Live |
| DB | Supabase | us-west-2 | ✅ Live |
| Web (dev) | Local Vite | http://localhost:5173 | ✅ Funciona |
| Web (prod) | Pendiente (S3 + CloudFront) | — | ⏳ Pendiente |

### Credenciales admin (local y Render)
- **Correo**: `admin@nutriclinica.cl`
- **Contraseña**: `NutriClinica2026` (cambiar en producción)

---

## Próximos pasos prioritarios (Backend)

1. **Decidir `DELETE plantilla` en uso**: ¿409 si planes activos la referencian? (hoy 204)
2. **Definir `Supabase__StorageKey` en Render** (los buckets ya existen)
3. Borrar el archivo del bucket al eliminar un registro (hoy queda huérfano)
4. **Paginación** en Citas y Consultas (ya hay DTOs, falta implementar)
5. **Tests** (xUnit + Testcontainers)
6. **Web a producción** → S3 + CloudFront + dominio
7. **CI/CD** GitHub Actions (build + deploy automático a Render)

> **⚠️ Git**: solo `commit` local. **No hacer `push`** hasta que el usuario lo indique.

---

## Comandos útiles (Backend)

```bash
# Backend local
cd nutriclinica-backend/nutriclinica-backend
dotnet run --urls "https://localhost:7140;http://localhost:5036"

# Migraciones
dotnet ef migrations add NombreMigracion
dotnet ef database update

# Tests
dotnet test
```
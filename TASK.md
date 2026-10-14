# Task List - NutriClínica Backend

## ✅ Completadas

### Infraestructura y Despliegue
- [x] Dockerfile multi-stage (.NET 10 SDK → ASP.NET 10 runtime)
- [x] .dockerignore
- [x] Configuración de producción: forwarded headers, CORS por env var, health check
- [x] Migraciones automáticas al arrancar con reintentos exponenciales
- [x] Connection string por variable de entorno + MaxPoolSize=5 en código
- [x] Despliegue en Render (API live en https://nutrition-clinic-api.onrender.com)
- [x] Variables de entorno en Render (JWT, Supabase, Seed, CORS)
- [x] Design-time DbContext factory para `dotnet ef` sin ejecutar Program.cs

### Autenticación y Autorización
- [x] Entidad Nutricionista: PasswordHash, RefreshToken, RefreshTokenExpiresAt
- [x] JWT HS256: access token 1h, refresh token 7 días
- [x] Hash con PasswordHasher<T> (PBKDF2 + sal por usuario)
- [x] Endpoints: POST /api/auth/login, /api/auth/refresh, /api/auth/logout, GET /api/auth/yo
- [x] `[Authorize]` en los 10 controllers existentes
- [x] GlobalExceptionMiddleware: UnauthorizedAccessException → 401
- [x] Seed configurable del admin inicial (variables Seed__AdminCorreo, Seed__AdminContrasena)
- [x] JwtConfig con validación al arranque (secret ≥ 32 chars, issuer/audience requeridos)

### Frontend (Web) - Auth completo
- [x] Interceptor request: envía `Authorization: Bearer` automáticamente
- [x] Interceptor response: 401 → refresh automático → reintento petición original
- [x] AuthProvider + useAuth: contexto global, carga usuario al montar con auto-refresh
- [x] AuthGuard: protege rutas, redirige a /login si no autenticado
- [x] Login page: /login con formulario, validación, prefill admin
- [x] Tokens en localStorage: nc_access_token, nc_refresh_token
- [x] Router: /login público, resto protegido con AuthGuard
- [x] Variables de entorno: VITE_API_URL_LOCAL, VITE_API_URL_PUBLIC, VITE_API_URL

### Base de Datos
- [x] Migración AgregarAutenticacionNutricionistas (PasswordHash, RefreshToken, RefreshTokenExpiresAt)
- [x] Connection string solo por variable de entorno (fuera de git)
- [x] Supabase pooler us-west-2 configurado

### Upload de Archivos
- [x] Cliente HTTP directo a la REST API de Supabase Storage (paquete NuGet descartado por fallo de reflexión)
- [x] `SupabaseStorageService`: subir, eliminar, URL pública y URL firmada (`x-upsert`)
- [x] `IStorageService` + `StorageConfig` (opcional: no rompe el deploy actual)
- [x] `POST /api/pacientes/{id}/fotos/subir` y `POST /api/pacientes/{id}/documentos/subir` (`[FromForm]`)
- [x] **Dos buckets**: `nutriclinica-media` (público, imágenes de paciente y de alimentos, máx. 5 MB) y `nutriclinica-docs` (privado, máx. 10 MB)
- [x] Validación de tipo MIME y tamaño
- [x] Ruta en Storage: `{pacienteId}/fotos|documentos/{Guid}.{ext}`
- [x] Fotos guardan la URL pública; documentos guardan la **ruta** y la API devuelve una **URL firmada** de 1 h
- [x] Sin migración de BD (las entidades ya guardan `UrlFoto` / `UrlDocumento`)
- [x] `StorageNoConfiguradoException` → 503 con mensaje claro
- [x] `[RequestSizeLimit]` por endpoint (5 MB fotos, 10 MB documentos)
- [x] Pruebas manuales en `utriclinica-backend.http`
- [x] Verificación end-to-end local: subida real a ambos buckets, descarga 200, límite 5 MB → 400
- [ ] Al eliminar un registro **no se borra el archivo** del bucket (queda huérfano)
- [ ] **Bloqueado**: definir `Supabase__StorageKey` en Render (service role key)

> **⚠️ No hacer `git push`** hasta que el usuario lo indique. Solo commit local.

---

## 🔨 En progreso

### Módulo de Menús → Catálogo de Alimentos
- [x] Entidad `Alimento` (auditoría `FechaCreacion`, `IsActive`)
- [x] Configuración en `ApplicationDbContext` (`NombreNormalizado` + índice único parcial)
- [x] Migración `20261009174116_AgregarCatalogoAlimentos` (aplicada)
- [x] DTOs: Crear / Actualizar / Respuesta / Filtro
- [x] Validadores FluentValidation (rangos, URL de imagen, filtro)
- [x] `IAlimentoService` + `AlimentoService` (normalización de nombres, orden dinámico, cálculo calórico)
- [x] Registro en DI (`Program.cs`)
- [x] **Controller `AlimentosController`** + subida de `ImagenUrl` a Supabase Storage
- [x] **24/24 pruebas manuales en `.http`** (CRUD, acentos, orden, rangos, 409, 400, borrar/restaurar, 401/404)

### Módulo de Menús → capa PLATO + Plantillas (arquitectura)
- [x] Enums `DiaSemana` (Lunes..Domingo) y `TiempoComida` (Desayuno..Colacion)
- [x] Entidades `Plato`, `DetallePlato`, `PlantillaMenu`, `DetallePlantillaMenu`
- [x] Configuración en `ApplicationDbContext` (FKs, `HasConversion<string>()` + check constraints, índices únicos, precisiones)
- [x] Migración `20261010215328_AgregarPlantillasMenu` (aplicada)
- [x] 14 DTOs + 8 validadores FluentValidation
- [x] `IPlatoService` + `PlatoService` (agregación de macros por gramaje, orden dinámico, duplicado en profundidad, bloqueo de borrado si está en uso)
- [x] `IPlantillaMenuService` + `PlantillaMenuService` (agregación de macros por cantidad, resumen por tiempo de comida, desviación calórica, duplicado)
- [x] Registro en DI (`Program.cs`)
- [x] **Controller `PlatosController`** y **`PlantillasMenuController`** escritos por el usuario
- [x] 2 errores CS1503 corregidos (`Ok(await Task)` → `await Task; Ok()`)
- [x] Constraint de ruta sin cerrar corregida (`{platoId:Guid` → `{platoId:guid}`)
- [x] `[FromQuery]` → `[FromBody]` en `/duplicar` + `dto?.Nombre`
- [x] `CreatedAtAction` corregido (`plantillaId` en vez de `id`) — resolvía un **500** real
- [x] 14 endpoints verificados en vivo (macros cuadran: 525 kcal para pollo 200 g + arroz 150 g)
- [ ] Pruebas manuales en `.http` (19 pruebas)

---

## 🔨 En progreso — Módulo: Menús Asignados (`MenuAsignado`)

El plan de comidas **de un paciente concreto**, con fecha de inicio. Detalle completo en `tasks-user.md`.

**Decisión de dominio tomada:** al asignar una plantilla a un paciente se **clona en profundidad**.
Los platos del paciente son copias propias, no referencias al catálogo. Motivo: si 30 pacientes
tienen "Pollo con arroz" y el nutricionista ajusta ese plato del catálogo, los 30 planes no deben
cambiar sin decisión clínica. Los planes son historia clínica, no borradores.

**Mi parte (arquitectura) — COMPLETADA:**
- [x] Enum `MenuAsignadoEstado` (`Borrador` → `Activo` → `Finalizado`)
- [x] Entidades `MenuAsignado` y `DetalleMenuAsignado`
- [x] Configuración en `ApplicationDbContext` (FKs a `Paciente` y `PlantillaMenu`, enums como
  string + check constraints, índice único en `{MenuAsignadoId, DiaSemana, TiempoComida, PlatoId}`)
- [x] Migración `20261014201651_AgregarMenusAsignados` **aplicada**
- [x] DTOs `CrearMenuAsignadoDto`, `DetalleMenuAsignadoRespuestaDto`, `MenuAsignadoRespuestaDto`
- [x] Validador `CrearMenuAsignadoValidator`
- [x] `IMenuAsignadoService` + `MenuAsignadoService` con **`AsignarPlantillaAsync`** (clonado
  profundo de platos + detalles) y `DuplicarMenuAsignadoAsync` (copia el plan, opcionalmente a
  otro paciente, dejándolo en `Borrador`)
- [x] Registro en DI. Build limpio (0 errores, 0 advertencias)

**Parte del usuario (lógica + controller) — pendiente:**
- [ ] **`MenuAsignadoService`**: 5 métodos añadidos sobre lo existente (`ObtenerPorId`,
  `ObtenerLista`, `Actualizar`, `Restaurar`, `Eliminar`) → `tasks-user.md` Tarea 1
- [ ] **`MenuAsignadosController`** (7 endpoints) → `tasks-user.md` Tarea 2
- [ ] **DTOs** `ActualizarMenuAsignadoDto`, `MenuAsignadoFiltroDto`,
  `ActualizarDetalleMenuAsignadoDto`, `DuplicarMenuAsignadoDto` → `tasks-user.md` Tarea 3
- [ ] **Pruebas `.http`** (ejecución guiada conmigo, no asignada como tarea)

**Reglas clínicas del módulo:**
- `Estado`: `Borrador` → `Activo` → `Finalizado`
- Un plan `Finalizado` **no se puede editar** (409): es historia clínica
- Un plan `Activo` **no se puede eliminar** (409): pasar a finalizado primero
- `PacienteId` **no se puede cambiar** en una actualización
- `FechaInicio` solo se puede cambiar si el plan está en `Borrador`

**Módulo Menús (PLATO + Plantillas) — corregido y compilando:**
- [x] El usuario corrigió el `CreatedAtAction` de `PlantillasMenuController` (`plantillaId` en
  vez de `id`), que devolvía **500** en `POST /api/plantillas-menu`
- [x] Los 14 endpoints de Platos y Plantillas quedan funcionando

---

## ⏳ Pendientes (Prioridad Alta)

### Paginación
- [ ] Implementar paginación en Citas (CitaFiltroDto ya existe)
- [ ] Implementar paginación en Consultas (ConsultaFiltroDto ya existe)
- [ ] Retornar metadata: total, página, tamaño, total páginas

### Tests
- [ ] Configurar xUnit + FluentAssertions
- [ ] Testcontainers para PostgreSQL en tests de integración
- [ ] Tests de Auth (login, refresh, logout, acceso denegado)
- [ ] Tests de CRUD principales (Pacientes, Citas, Consultas)
- [ ] Tests de validación (FluentValidation)

### CI/CD
- [ ] GitHub Actions: build + test en PR
- [ ] GitHub Actions: deploy automático a Render (OIDC)
- [ ] GitHub Actions: lint + typecheck

### Web a Producción
- [ ] S3 bucket + CloudFront distribution
- [ ] Build automático en GitHub Actions
- [ ] Dominio personalizado + certificado ACM

### Seguridad / Hardening
- [ ] Rate limiting (endpoint auth y API general)
- [ ] Headers de seguridad (HSTS, CSP, etc.)
- [ ] Auditoría de CORS (orígenes específicos, no wildcard)
- [ ] Rotación de secretos (JWT, DB) en Render

---

## 📝 Notas

- **⚠️ Flujo de git**: solo hacer `commit` local. **NO hacer `push`** hasta que el usuario lo indique.
- **Diseño del módulo Menús**: se descartó `DetallePlantillaMenu → Alimento` y se insertó la capa
  **PLATO** (`PlantillaMenu → DetallePlantillaMenu → Plato → DetallePlato → Alimento`), porque un menú
  clínico son platillos compuestos reutilizables y permite sustituirlos enteros por alergia.
  `PlantillaMenu` = semana reutilizable (puede ser 1 día); `MenuAsignado` (siguiente módulo) = plan
  por paciente con clonado en profundidad.
- **Supabase Storage**: dos buckets —
  `nutriclinica-media` (público, imágenes, 5 MB) y `nutriclinica-docs` (privado, documentos, 10 MB).
  La key `service_role` va **solo** en user-secrets / variables de entorno, nunca en `appsettings.json`.
- **Credenciales admin**: `admin@nutriclinica.cl` / `NutriClinica2026` (cambiar en producción)
- **API Render**: https://nutrition-clinic-api.onrender.com
- **Web local**: `npm run dev` en `nutriclinica-web` (puerto 5173)
- **API local**: `dotnet run --urls "https://localhost:7140;http://localhost:5036"` (puerto 7140 HTTPS)
- **Cambiar entorno web**: Editar `VITE_API_URL` en `.env` y `npm run dev`
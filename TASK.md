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
- [x] `SupabaseStorageService`: subir y eliminar archivos con `x-upsert`
- [x] `IStorageService` + `StorageConfig` (opcional: no rompe el deploy actual)
- [x] `POST /api/pacientes/{id}/fotos/subir` y `POST /api/pacientes/{id}/documentos/subir` (`[FromForm]`)
- [x] Validación de tipo MIME y tamaño (máx. 10 MB)
- [x] Ruta en Storage: `{pacienteId}/fotos|documentos/{Guid}.{ext}`
- [x] Reutiliza `RegistrarFotoAsync` / `RegistrarDocumentoAsync` (sin migración de BD)
- [x] `StorageNoConfiguradoException` → 503 con mensaje claro
- [x] `[RequestSizeLimit(10MB)]` en los dos endpoints
- [x] Pruebas manuales en `utriclinica-backend.http`
- [ ] **Bloqueado**: configurar `Supabase__StorageKey` (service role key) y crear el bucket `nutriclinica-storage`

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

- **Credenciales admin**: `admin@nutriclinica.cl` / `NutriClinica2026` (cambiar en producción)
- **API Render**: https://nutrition-clinic-api.onrender.com
- **Web local**: `npm run dev` en `nutriclinica-web` (puerto 5173)
- **API local**: `dotnet run --urls "https://localhost:7140;http://localhost:5036"` (puerto 7140 HTTPS)
- **Cambiar entorno web**: Editar `VITE_API_URL` en `.env` y `npm run dev`
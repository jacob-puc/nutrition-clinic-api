# Progreso del Proyecto NutriClínica (Backend)

## Estado Actual: **API desplegada, Auth completo y upload de archivos implementado**

**Última actualización:** 2026-10-09

---

## Resumen Ejecutivo

- ✅ **API (.NET 10)** desplegada en Render: `https://nutrition-clinic-api.onrender.com`
- ✅ **Autenticación JWT completa** (login, refresh, logout, guard)
- ✅ **Upload de archivos** (fotos/documentos) vía Supabase Storage
- ✅ **Base de datos:** Supabase (PostgreSQL) en `us-west-2`
- ✅ **Despliegue:** Render (free tier)
- ✅ **Repositorio:** https://github.com/jacob-puc/nutrition-clinic-api

---

## Backend (nutriclinica-backend)

### Módulos implementados (7 features, 47 endpoints)
| Módulo | Entidad | Endpoints | Estado |
|---|---|---|---|
| Pacientes | Paciente | 7 (CRUD + expediente + objetivo) | ✅ |
| Historiales Clínicos | HistorialClinico | 2 | ✅ |
| Antropometría | MedidaAntropometrica | 5 | ✅ |
| Expediente Media | FotoSeguimiento, DocumentoPaciente | 12 (CRUD + subida) | ✅ |
| Nutricionistas | Nutricionista | 5 | ✅ |
| Citas | Cita | 6 | ✅ |
| Consultas | Consulta | 8 | ✅ |

### Upload de archivos
- **Almacenamiento**: Supabase Storage, consumido por su REST API directa
  (el cliente NuGet `Supabase` fue descartado: fallaba en reflexión por dependencias
  `Supabase.Core` / `Gotrue` / `Postgrest`)
- **Endpoints**: `POST /api/pacientes/{id}/fotos/subir`, `POST /api/pacientes/{id}/documentos/subir`
  (multipart/form-data, `[RequestSizeLimit(10MB)]`)
- **Validación**: MIME (JPEG/PNG/WEBP para fotos; PDF/JPEG/PNG/DOC/DOCX para documentos), máx. 10 MB
- **Ruta en Storage**: `{pacienteId}/fotos|documentos/{Guid}.{ext}` → URL pública devuelta al cliente
- **Sin migración de BD**: las entidades ya guardan `UrlFoto` / `UrlDocumento`
- **Configuración opcional**: si faltan las credenciales, el resto de la API sigue funcionando y
  el endpoint responde `503 STORAGE_NO_CONFIGURADO` con mensaje claro
- **Variables de entorno**: `Supabase__Url` (pre-llenada en `appsettings.json`), `Supabase__StorageKey`
  (service role key, secreta), `Supabase__Bucket` (default `nutriclinica-storage`)

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

1. **Configurar Supabase Storage** → crear bucket `nutriclinica-storage` (público) y definir `Supabase__StorageKey`
2. **Paginación** en Citas y Consultas (ya hay DTOs, falta implementar)
3. **Tests** (xUnit + Testcontainers)
4. **Web a producción** → S3 + CloudFront + dominio
5. **CI/CD** GitHub Actions (build + deploy automático a Render)

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
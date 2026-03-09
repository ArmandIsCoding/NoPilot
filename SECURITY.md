# 🔒 Política de Seguridad

## Versiones Soportadas

Actualmente soportamos la última versión publicada de NoPilot:

| Versión | Soportada          |
| ------- | ------------------ |
| 0.1.x   | ✅ Sí              |
| < 0.1   | ❌ No              |

---

## 🛡️ Consideraciones de Seguridad

### Privacidad de Datos

NoPilot está diseñado para **privacidad por defecto**:

✅ **Qué hace:**
- Ejecuta **100% localmente** (Ollama en localhost)
- No envía código a servicios externos
- No hace telemetría ni tracking
- Almacena datos solo en tu máquina (SQLite local)

❌ **Qué NO hace:**
- No se conecta a APIs cloud
- No comparte tu código con terceros
- No tiene backdoors ni reporting automático

### Datos Almacenados

NoPilot almacena en `nopilot.db` (SQLite):
- Rutas relativas de archivos
- Contenido de código fragmentado (chunks)
- Embeddings numéricos (vectores de 1024 dimensiones)
- Metadatos (timestamps, índices)

**Importante:** 
- El archivo `nopilot.db` contiene tu código en texto plano
- Añade `nopilot.db` a `.gitignore` (ya incluido por defecto)
- No compartas este archivo públicamente

---

## 🚨 Reportar Vulnerabilidades

Si descubres una vulnerabilidad de seguridad en NoPilot, por favor **NO la reportes públicamente**.

### Proceso de Reporte

1. **Envía un email privado** a: [TU_EMAIL_SEGURIDAD] o usa [GitHub Security Advisories](https://github.com/ArmandIsCoding/NoPilot/security/advisories/new)

2. **Incluye:**
   - Descripción detallada de la vulnerabilidad
   - Pasos para reproducir
   - Impacto potencial
   - Versión afectada
   - (Opcional) Propuesta de fix

3. **Tiempo de respuesta:**
   - Acuse de recibo: **< 48 horas**
   - Evaluación inicial: **< 7 días**
   - Fix (si aplica): **< 30 días** según severidad

4. **Divulgación:**
   - Coordinaremos la divulgación pública contigo
   - Te acreditaremos en el changelog (si lo deseas)
   - Publicaremos un security advisory en GitHub

---

## 🔐 Buenas Prácticas para Usuarios

### Configuración Segura

1. **No hardcodees credenciales** en `appsettings.json`
   - Si añades soporte para APIs cloud en el futuro, usa variables de entorno

2. **Protege tu `nopilot.db`**
   - No lo subas a repositorios públicos
   - Considera encriptar el disco donde se almacena

3. **Ollama local únicamente**
   - No expongas el puerto 11434 de Ollama a internet sin autenticación
   - Usa firewall para bloquear conexiones externas

4. **Permisos de archivos**
   - NoPilot solo necesita **lectura** en `SourceFolder`
   - No requiere permisos de administrador

### Carpetas Sensibles

⚠️ **Ten cuidado al indexar:**
- Carpetas con credenciales (`.env`, `secrets.json`, `.aws`, `.ssh`)
- Archivos de configuración con passwords
- Tokens de API o keys en comentarios

**Sugerencia:** Añade exclusiones en `SupportedExtensions` o filtra manualmente antes de ingestar.

---

## 🧪 Dependencias y Supply Chain

### Paquetes NuGet

Todas las dependencias provienen de fuentes oficiales:
- `nuget.org` (paquetes de Microsoft)
- Versiones pinneadas en `.csproj`

### Auditoría de Dependencias

Ejecuta regularmente:

```bash
dotnet list package --vulnerable
dotnet list package --outdated
```

Para actualizar paquetes con vulnerabilidades:

```bash
dotnet add package <PackageName> --version <SafeVersion>
```

---

## 🛠️ Consideraciones de Desarrollo

### Revisión de Código

- Todos los PRs requieren revisión antes de merge
- CI/CD verificará compilación exitosa
- No hay secretos en el código fuente

### Dependencies Pinning

Las versiones de paquetes están fijadas para evitar ataques de supply chain:

```xml
<PackageReference Include="Microsoft.SemanticKernel" Version="1.73.0" />
```

---

## 📊 Divulgación Responsable

Agradecemos a los investigadores de seguridad que reportan vulnerabilidades de forma responsable. Los reconoceremos públicamente (si lo desean) en:

- El CHANGELOG.md
- El security advisory correspondiente
- El README.md en una sección de agradecimientos especiales

---

## 🔗 Recursos Adicionales

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [.NET Security Best Practices](https://learn.microsoft.com/en-us/dotnet/standard/security/)
- [SQLite Security](https://www.sqlite.org/security.html)

---

## 📧 Contacto de Seguridad

Para reportes de seguridad sensibles:
- **GitHub Security Advisories**: [Crear advisory privado](https://github.com/ArmandIsCoding/NoPilot/security/advisories/new)
- **Email**: [CONFIGURAR_EMAIL_SEGURIDAD] (TODO: añadir email real)

---

<div align="center">

**[🏠 Volver al README](../README.md)**

</div>

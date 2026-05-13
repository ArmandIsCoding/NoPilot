# NoPilot - Capacidades de Edición y Creación de Archivos

## Resumen

NoPilot ahora permite no solo **chatear con tus archivos locales**, sino también:
- 📝 **Crear nuevos archivos** con contenido generado por IA
- ✏️ **Editar múltiples archivos existentes** en una sola instrucción
- 📊 **Ver diffs unificados** antes de aplicar cambios
- 💾 **Backups automáticos** de cada cambio realizado

---

## Creación de Archivos

### Patrón
```
crea <ruta-archivo> <instrucción sobre contenido>
```

### Ejemplos
```
>> crea config.json con configuracion para conexion a base de datos
>> crear helpers.ts para utilidades de formateo
>> create README.md explicando la arquitectura del proyecto
```

**Flujo:**
1. NoPilot detecta la solicitud de creación
2. Genera el contenido usando el modelo de IA
3. Muestra un **preview** (primeras 50 líneas)
4. Pide confirmación: `Crear archivo? (s/N):`
5. Si confirmas, crea el archivo dentro de `SourceFolder` (respetando subdirectorios)
6. Salida: `[OK] Archivo creado: config.json`

**Restricciones:**
- Solo extensiones permitidas (definidas en `Ingestion:SupportedExtensions`)
- Must be inside `SourceFolder` (no escapes a rutas externas)
- No sobrescribe archivos existentes (usa edición para eso)

---

## Edición de Archivos

### Patrón
```
<verbo-edicion> <ruta-archivo> <instrucción>
```

### Verbos soportados
- `edita` / `editar`
- `modifica` / `modificar`
- `cambia` / `cambiar`
- `actualiza` / `actualizar`
- `traduce` / `traducir`
- `corrige` / `corregir`
- `refactoriza` / `refactorizar`
- `rewrite` / `translate`

### Ejemplos
```
>> traduce README.md al inglés
>> corrige errores de sintaxis en src/main.cs
>> refactoriza utils.js para mejor legibilidad
>> modifica config.json y config2.json para usar puerto 8080
```

**Flujo:**
1. NoPilot detecta múltiples archivos en la solicitud
2. Valida cada archivo (existe, extensión, tamaño)
3. Genera propuestas de cambio para cada archivo válido
4. Muestra un **diff unificado** (estilo `git diff`) con:
   - Líneas eliminadas en rojo (`-`)
   - Líneas añadidas en verde (`+`)
   - Contexto alrededor de cambios (3 líneas por defecto)
5. Pide confirmación única: `Aplicar cambios en X archivo(s)? (s/N):`
6. Si confirmas:
   - Crea **backups automáticos** en `.nopilot-backups/` con timestamp
   - Escribe los cambios
   - Muestra rutas de archivos actualizados y backups

**Restricciones:**
- Solo edita archivos existentes
- Same security checks como creación

### Backups Automáticos

**Ubicación:** `.nopilot-backups/` (relativo a `SourceFolder`)

**Formato:**
```
.nopilot-backups/
├── config.json.20260513-143022.bak
├── utils.js.20260513-143022.bak
└── src/
    └── main.cs.20260513-143015.bak
```

**Estructura:** `{original_filename}.{YYYYMMDD-HHmmss}.bak`

Cada backup contiene la versión **anterior** del archivo antes de aplicar cambios.

---

## Diff Unificado

**Cuando ves:**
```
[Diff] config.json
--- a/config.json
+++ b/config.json
@@ -5,7 +5,7 @@
   "version": "1.0",
   "settings": {
-    "port": 3000,
+    "port": 8080,
     "timeout": 3000
   }
 }
```

**Significado:**
- `@@ -5,7 +5,7 @@` = cambios empezando línea 5, 7 líneas (old) y 7 líneas (new)  
- `" "` (espacio) = línea sin cambios  
- `"-"` = línea eliminada  
- `"+"` = línea añadida  

---

## Ejemplo Completo: Crear un Archivo

```bash
>> crea config.json con template de configuracion para desarrollo local con puerto 3000 y base de datos sqlite

[INFO] Generando contenido para nuevo archivo 'config.json'...

[Nuevo archivo] config.json
{
  "app": {
    "name": "MyApp",
    "port": 3000,
    "environment": "development"
  },
  "database": {
    "type": "sqlite",
    "path": "./app.db",
    "logging": true
  },
  "features": {
    "caching": true,
    "compression": true
  }
}
...truncado (0 lineas mas)...

Crear archivo? (s/N): s
[OK] Archivo creado: config.json
[INFO] Sugerencia: ejecuta INGESTAR para indexar este nuevo archivo.
```

---

## Ejemplo Completo: Editar Múltiples Archivos

```bash
>> traduce README.md y CHANGELOG.md al ingles

[INFO] Preparando propuesta para 'README.md'...
[INFO] Preparando propuesta para 'CHANGELOG.md'...

[Diff] README.md
--- a/README.md
+++ b/README.md
@@ -1,5 +1,5 @@
-# Mi Proyecto Especial
+# My Special Project
-Una herramienta increíble para...
+An incredible tool for...

[Diff] CHANGELOG.md
--- a/CHANGELOG.md
+++ b/CHANGELOG.md
@@ -1,3 +1,3 @@
-## Cambios v1.0
+## Changes v1.0
-Se añadió soporte para...
+Added support for...

Aplicar cambios en 2 archivo(s)? (s/N): s
[OK] Archivo actualizado: README.md
[INFO] Backup: .nopilot-backups/README.md.20260513-143022.bak
[OK] Archivo actualizado: CHANGELOG.md
[INFO] Backup: .nopilot-backups/CHANGELOG.md.20260513-143022.bak
[INFO] Sugerencia: ejecuta INGESTAR para refrescar el indice semantico con este cambio.
```

---

## Comandos Principales

| Comando | Descripción |
|---------|-----------|
| `INGESTAR` | Indexa archivos de la carpeta configurada al vector store |
| `LIMPIAR` | Limpia índice y historial de chat |
| `AYUDA` | Muestra esta lista de comandos |
| `SALIR` | Cierra la aplicación |
| `<texto>` | Pregunta sobre código indexado (RAG) |
| `<edicion>` | Edita archivos existentes |
| `<crear>` | Crea nuevos archivos |

---

## Configuración Relevante (appsettings.json)

```json
{
  "Ingestion": {
    "SourceFolder": "/ruta/a/tu/codigo",
    "SupportedExtensions": [".cs", ".md", ".json", ".js", "..."],
    "MaxFileSizeBytes": 1048576
  }
}
```

- `SourceFolder`: Carpeta base donde NoPilot puede crear/editar archivos
- `SupportedExtensions`: Extensiones permitidas para operaciones de archivo
- `MaxFileSizeBytes`: Límite de tamaño por archivo (1MB por defecto)

---

## Seguridad y Validaciones

✅ **Protecciones implementadas:**
- Path traversal check (no permite `../../`)
- Restricción a `SourceFolder` (no escapa fuera)
- Validación de extensión permitida
- Validación de tamaño máximo
- Preview + confirmación antes de escribir
- **Backups automáticos** de cada cambio

⚠️ **Úsalo responsablemente:**
- Revisa siempre el diff/preview antes de confirmar
- NoPilot puede alucinar; verifica cambios críticos
- Los backups se guardan en `.nopilot-backups/` para recuperación manual

---

## Próximas Mejoras

- [ ] Comando `DESHACER` para restaurar último backup
- [ ] Modo "dry-run" (solo preview, sin escribir)
- [ ] Excluir carpetas sensibles por defecto (`.git`, `bin`, `obj`)
- [ ] Soporte para operaciones en batch sin confirmación individual
- [ ] Historial de cambios con timestamps

---

**Última actualización:** 2026-05-13


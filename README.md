# NaturalQuery-Ollama

Prueba de concepto para consultar SQL Server en español usando una API .NET y un modelo local de Ollama (`qwen3:4b`). La API genera SQL de solo lectura, lo valida antes de ejecutarlo y devuelve los datos junto con una respuesta breve.

## Arquitectura

- `NaturalQuery.Domain`: entidades y reglas del dominio de ventas.
- `NaturalQuery.Application`: casos de uso, contratos y validación SQL.
- `NaturalQuery.Infrastructure`: EF Core, SQL Server y cliente de Ollama.
- `NaturalQuery.Api`: endpoints HTTP y composición de dependencias.

La solución practica Clean Architecture, DDD, repositorios, Unit of Work, inyección de dependencias y EF Core.

## Requisitos

- SDK de .NET 10.
- SQL Server accesible desde la máquina donde corre la API.
- Ollama instalado y activo, con el modelo `qwen3:4b` disponible.

## Configuración local

1. Crea una base vacía llamada `LocalAiDemo` en tu SQL Server.
2. Configura temporalmente `ConnectionStrings__LocalAiDemo` con una cuenta que pueda crear tablas y ejecutar migraciones. No guardes la cadena en archivos del repositorio.
3. Restaura la herramienta local de EF Core y aplica las migraciones:

   ```bash
   dotnet tool restore
   dotnet ef database update --project src/NaturalQuery.Infrastructure --startup-project src/NaturalQuery.Api
   ```

4. Crea una cuenta SQL de solo lectura para que la API consulte las tablas. Puedes adaptar [`scripts/LocalAiDemo.Reader.sql`](scripts/LocalAiDemo.Reader.sql); reemplaza el marcador de contraseña solo en tu copia local y nunca publiques la contraseña.
5. Configura `ConnectionStrings__LocalAiDemo` con esa cuenta de solo lectura e inicia la API:

   ```bash
   dotnet run --project src/NaturalQuery.Api
   ```

La API espera Ollama en `http://localhost:11434` y usa `qwen3:4b`, configurado en `appsettings.Development.json`. La variable de entorno de conexión tiene prioridad sobre cualquier configuración local.

Este repositorio no incluye respaldos, volcados ni datos de bases de datos. Después de aplicar las migraciones, agrega datos de demostración únicamente a tu instancia local.

## Endpoints

- `GET /health`: estado de la API y configuración de Ollama/SQL Server.
- `GET /health/database`: comprueba la conexión a SQL Server.
- `POST /api/ai/sql/preview`: genera y valida SQL sin ejecutarlo.
- `POST /api/ai/sql`: genera, valida y ejecuta la consulta de solo lectura.
- En desarrollo, el documento OpenAPI está disponible en `/openapi/v1.json`.

La ejecución limita el resultado a 100 filas y el tiempo de consulta a 15 segundos. Las consultas se restringen a las tablas de demostración `Cliente`, `Producto`, `Venta` y `DetalleVenta`.

Puedes probar los endpoints con [`src/NaturalQuery.Api/NaturalQuery.Api.http`](src/NaturalQuery.Api/NaturalQuery.Api.http). La batería manual está en [`docs/Evaluacion-POC.md`](docs/Evaluacion-POC.md).

## Pruebas

Las pruebas unitarias del validador SQL y del flujo de aplicación están en `tests/NaturalQuery.Application.Tests`. Usan dependencias simuladas y no requieren SQL Server ni Ollama:

```bash
dotnet test
```

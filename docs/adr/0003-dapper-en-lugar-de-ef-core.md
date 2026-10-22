# 0003 · Dapper en lugar de Entity Framework Core

**Estado:** Aceptada

## Contexto

Las escrituras importantes pasan por procedimientos almacenados (ADR 0002) y las lecturas de stock e historial por funciones SQL. El modelo de datos es pequeño (usuarios, productos, movimientos, auditoría) y el esquema se mantiene con scripts SQL versionados, no con migraciones.

## Decisión

Usar **Dapper** sobre **Npgsql** (`NpgsqlDataSource`) para todo el acceso a datos:

- Las consultas y llamadas `CALL` se escriben en SQL explícito y siempre **parametrizado**.
- Cada repositorio mapea filas a registros privados (`ProductRow`, `StockRow`…) y de ahí a entidades o modelos, sin exponer detalles de la base al resto de la aplicación.
- Los errores de PostgreSQL (`PostgresException`) se traducen a excepciones de dominio en un solo lugar (`PostgresErrorMapper`).

## Alternativas consideradas

- **EF Core:** excelente para CRUD y para modelos ricos con *change tracking*. Aquí la mayor parte del trabajo son llamadas a procedimientos y funciones, donde EF aporta poco (`FromSql`, `ExecuteSql`), y sus migraciones competirían con los scripts SQL como fuente de verdad del esquema.
- **ADO.NET puro:** sin dependencias, pero con mucho código repetido para leer cada columna.

## Consecuencias

- El SQL que se ejecuta es exactamente el que está escrito: fácil de revisar, de optimizar y de probar.
- No hay *change tracking*: actualizar un producto es un `UPDATE` explícito.
- El mapeo y los tipos (por ejemplo `timestamptz` a `DateTimeOffset`) se manejan a mano, con pruebas de integración contra PostgreSQL real para cubrirlos.

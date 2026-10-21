# Base de datos

Scripts de PostgreSQL 17 para el sistema de inventario. Todos son idempotentes y deben ejecutarse en orden.

| Script | Contenido |
|---|---|
| `01_schema.sql` | Esquema `inventory`: tablas `users`, `products` y `movements`, restricciones e índices |
| `02_audit.sql` | Auditoría: tabla `audit_log` (solo inserciones) y trigger genérico sobre las tablas de negocio |

## Ejecución

```bash
createdb -U postgres inventory
psql -U postgres -d inventory -v ON_ERROR_STOP=1 -f database/01_schema.sql
psql -U postgres -d inventory -v ON_ERROR_STOP=1 -f database/02_audit.sql
```

## Auditoría

Cada INSERT, UPDATE y DELETE sobre `users`, `products` y `movements` se registra en `inventory.audit_log` con los datos anteriores y nuevos en JSONB.

- El usuario de la aplicación se toma de la variable de sesión `app.user_id`, que fijan los procedimientos con `SET LOCAL`.
- `password_hash` nunca se guarda en la auditoría.
- Un UPDATE sin cambios reales no genera registro.
- El registro no se puede modificar ni borrar: un trigger lo rechaza con SQLSTATE `42501`.

## Pruebas

Cada script tiene sus pruebas en `tests/`. Corren dentro de una transacción que se revierte, así que no dejan datos; imprimen `OK` por cada verificación y fallan con `FAIL` y código de salida distinto de cero.

```bash
psql -U postgres -d inventory -v ON_ERROR_STOP=1 -f database/tests/01_schema.test.sql
```

El pipeline de CI aplica todos los scripts dos veces (para verificar que son idempotentes) sobre un PostgreSQL 17 limpio y luego ejecuta las pruebas.

## Modelo

```
users 1 ──< movements >── 1 products
```

- `products.current_stock` guarda el stock vigente y nunca puede ser negativo (`CHECK`).
- `movements` es el libro de entradas (`IN`) y salidas (`OUT`); `stock_after` registra el stock resultante.
- Los productos y usuarios con movimientos no se pueden borrar (`ON DELETE RESTRICT`); se desactivan con `is_active`.

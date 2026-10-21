# Base de datos

Scripts de PostgreSQL 17 para el sistema de inventario. Todos son idempotentes y deben ejecutarse en orden.

| Script | Contenido |
|---|---|
| `01_schema.sql` | Esquema `inventory`: tablas `users`, `products` y `movements`, restricciones e índices |
| `02_audit.sql` | Auditoría: tabla `audit_log` (solo inserciones) y trigger genérico sobre las tablas de negocio |
| `03_procedures.sql` | Procedimientos de entradas y salidas, y funciones de consulta de stock e historial |
| `04_seed.sql` | Datos de demostración: usuarios `admin` / `operador` y productos con stock inicial |

## Ejecución

```bash
createdb -U postgres inventory
for script in database/0*.sql; do psql -U postgres -d inventory -v ON_ERROR_STOP=1 -f "$script"; done
```

## Procedimientos

| Objeto | Tipo | Uso |
|---|---|---|
| `sp_register_entry(product_id, quantity, user_id, reason, OUT movement_id, OUT stock_after)` | PROCEDURE | Registra una entrada |
| `sp_register_exit(product_id, quantity, user_id, reason, OUT movement_id, OUT stock_after)` | PROCEDURE | Registra una salida |
| `fn_get_stock(search, only_active, limit, offset)` | FUNCTION | Stock actual paginado, con filtro por SKU o nombre |
| `fn_get_movements(product_id, limit, offset)` | FUNCTION | Historial de movimientos paginado |

```sql
CALL inventory.sp_register_exit(1, 5, 1, 'Venta', NULL, NULL);
SELECT * FROM inventory.fn_get_stock('lap');
```

Entradas y salidas comparten `sp_register_movement`, que bloquea la fila del producto con `FOR UPDATE`: dos salidas simultáneas sobre el mismo producto se aplican una después de otra, y la segunda valida con el stock ya actualizado. Las consultas son funciones porque en PostgreSQL un procedimiento no puede devolver un conjunto de filas.

### Errores de negocio

| SQLSTATE | Significado |
|---|---|
| `IN001` | Producto no encontrado |
| `IN002` | Stock insuficiente (`DETAIL` trae `available` y `requested`) |
| `IN003` | Producto inactivo |
| `IN004` | Usuario no encontrado o inactivo |
| `IN005` | Cantidad inválida |

### Usuarios de demostración

| Usuario | Contraseña | Rol |
|---|---|---|
| `admin` | `Admin123*` | Admin |
| `operador` | `Operador123*` | Operator |

Solo para desarrollo local; las contraseñas se guardan con BCrypt.

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

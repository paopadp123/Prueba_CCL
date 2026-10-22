# 0004 · Auditoría con triggers en la base de datos

**Estado:** Aceptada

## Contexto

En un inventario hay que poder responder "¿quién cambió esto y cuándo?" para productos, usuarios y movimientos. Si la auditoría depende de que cada caso de uso en C# se acuerde de registrarla, basta un olvido, o un cambio hecho directamente en la base, para que quede un hueco.

## Decisión

- Un **trigger genérico** (`AFTER INSERT OR UPDATE OR DELETE`) sobre `users`, `products` y `movements` escribe en `inventory.audit_log`: la tabla, la operación, los datos anteriores y nuevos en `JSONB`, la fecha y el usuario.
- El **usuario de la aplicación** llega a la base por la variable de sesión `app.user_id`. La API la fija con `SET LOCAL` en la misma transacción del cambio, a partir del `sub` del token (`IUserContext`).
- `password_hash` **nunca** se guarda en la auditoría.
- Un `UPDATE` que no cambia nada no genera registro.
- `audit_log` es de solo inserción: otro trigger bloquea `UPDATE` y `DELETE` sobre ella.

## Alternativas consideradas

- **Auditoría en la aplicación** (interceptores, decoradores): más visible en el código, pero no cubre cambios fuera de la API y es fácil de omitir.
- **Tablas temporales o *Change Data Capture*:** más potentes, pero requieren infraestructura extra que no se justifica aquí.

## Consecuencias

- Todo cambio queda registrado, venga de donde venga, y con el usuario real cuando pasa por la API. Una prueba de integración verifica la cadena completa: token → `IUserContext` → `app.user_id` → trigger.
- Los cambios hechos directamente en la base quedan con usuario vacío, lo que también es información útil.
- La lógica de auditoría está en SQL; está cubierta por las pruebas de `database/tests/02_audit.test.sql`.

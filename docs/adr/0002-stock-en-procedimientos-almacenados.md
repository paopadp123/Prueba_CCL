# 0002 · Reglas de stock en procedimientos almacenados con bloqueo de fila

**Estado:** Aceptada

## Contexto

La prueba exige procedimientos almacenados para entradas, salidas y stock. Además, la regla más crítica del sistema, **no sacar más unidades de las que hay**, tiene un problema clásico de concurrencia: si dos salidas leen "hay 1 unidad" al mismo tiempo y ambas restan, el stock queda en −1 (*lost update*).

Validar en C# (leer el stock, comparar y luego escribir) deja una ventana entre la lectura y la escritura en la que otra petición puede colarse.

## Decisión

- `sp_register_entry` y `sp_register_exit` delegan en `sp_register_movement`, que en **una sola transacción**:
  1. Bloquea la fila del producto con `SELECT … FOR UPDATE`.
  2. Valida que exista, esté activo y, si es salida, que el stock alcance.
  3. Inserta el movimiento con `stock_after` y actualiza `current_stock`.
- Los errores de negocio se lanzan con **SQLSTATE propios**: `IN001` no existe, `IN002` stock insuficiente (con `DETAIL available=…;requested=…`), `IN003` inactivo, `IN004` usuario, `IN005` cantidad.
- Como red de seguridad, la tabla tiene `CHECK (current_stock >= 0)`.
- Las consultas son **funciones** (`fn_get_stock`, `fn_get_movements`), porque en PostgreSQL un procedimiento no devuelve conjuntos de filas. Calculan el total para paginar con `count(*) OVER ()` en la misma consulta.

## Alternativas consideradas

- **Validación en C# con concurrencia optimista** (columna de versión): funciona, pero obliga a reintentar en la aplicación cuando hay conflicto. Con mucha contención sobre pocos productos, los reintentos se acumulan.
- **Nivel de aislamiento `SERIALIZABLE`:** también evita el problema, pero aborta transacciones que luego hay que reintentar, y afecta a todas las consultas de la transacción.
- **Solo el `CHECK`:** evita el negativo, pero el error llega como violación de restricción, sin los datos de "disponible" y "solicitado" para el usuario.

## Consecuencias

- La garantía la da la base de datos, la única pieza que ve todas las peticiones. Lo demuestra una prueba de integración: 10 salidas concurrentes por la API sobre 5 unidades dan siempre 5 aceptadas, 5 rechazadas con `409` y stock final 0.
- El dominio tiene `Product.EnsureCanWithdraw`, pero el flujo de salidas **no lo usa** a propósito: sería una lectura extra que igual podría quedar desactualizada. La única validación de stock es la del procedimiento.
- Parte de la lógica vive en SQL, así que hay que versionarla y probarla. Los scripts son idempotentes y tienen pruebas propias que corre el CI.

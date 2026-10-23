# 0007 · Movimientos inmutables y baja lógica de productos

**Estado:** Aceptada

## Contexto

El stock de un producto tiene que poder explicarse siempre: "hay 7 porque entraron 10 y salieron 3". Si se permite editar el stock a mano, cambiar un movimiento ya registrado o borrar productos con historial, esa explicación se rompe y la auditoría pierde valor.

## Decisión

- **El stock solo cambia con movimientos.** Un producto se crea con stock 0, y la API no tiene ningún endpoint para editar `current_stock`.
- **Los movimientos no se editan ni se borran.** Funcionan como un libro contable: si hubo un error, se registra el movimiento contrario con su motivo.
- Cada movimiento guarda `stock_after` (el stock resultante), el usuario que lo hizo y un motivo opcional.
- **Los productos no se borran físicamente:** `PATCH /api/products/{id}/status` los desactiva. Un producto inactivo no admite movimientos (`409 inactive_product`), pero conserva su historial y se puede consultar.
- **El SKU no se puede modificar** después de creado, porque es el identificador que usan las personas y otros sistemas.

## Alternativas consideradas

- **Permitir editar movimientos:** más cómodo ante un error de digitación, pero el historial dejaría de reflejar lo que realmente pasó.
- **Borrado físico de productos:** rompería la integridad referencial con los movimientos, o obligaría a borrarlos también y perder la trazabilidad.

## Consecuencias

- El historial es la fuente de verdad: `stock_after` permite reconstruir la evolución del stock en el tiempo.
- Corregir un error requiere un movimiento adicional, que también queda registrado, con su autor y su motivo.
- Los listados filtran los inactivos por defecto (`includeInactive=false`), para que no estorben en el día a día.

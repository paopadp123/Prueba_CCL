# 0005 · Errores con ProblemDetails y códigos estables

**Estado:** Aceptada

## Contexto

El front necesita distinguir errores para mostrarlos bien: no es lo mismo "stock insuficiente" (se muestra junto a la cantidad, con lo disponible) que "SKU duplicado" (junto al campo SKU) o un error del servidor (aviso global). Comparar textos de mensajes es frágil, y devolver detalles internos, como un *stack trace*, es un riesgo de seguridad.

## Decisión

- Todas las respuestas de error siguen **ProblemDetails (RFC 9457)**: `status`, `title`, `detail` y `instance`.
- Se agregan extensiones propias:
  - **`code`**: identificador estable en inglés (`insufficient_stock`, `duplicate_sku`, `invalid_credentials`…), que es lo que usa el front.
  - **`correlationId`**: el mismo valor que el encabezado `X-Correlation-ID` y que aparece en cada línea de log de esa petición.
  - **Datos útiles** cuando aplica, por ejemplo `available` y `requested` en stock insuficiente.
- Las excepciones se traducen en un solo lugar (`ExceptionProblemMapper`, usado por un `IExceptionHandler`). Los errores inesperados devuelven un mensaje genérico; el detalle solo queda en el log.
- **Cadena completa:** SQLSTATE propio (`IN002`) → `PostgresErrorMapper` → `InsufficientStockException` → `409 insufficient_stock`.
- En el front, un interceptor convierte cada respuesta en `ApiError` con el mensaje en español:
  - Errores de conexión, `403`, `429` y `5xx` → aviso global.
  - `401` → cierra la sesión.
  - `400`, `404` y `409` → los muestra la pantalla junto al campo correspondiente.

## Alternativas consideradas

- **Formato propio** (`{ success, error }`): habría que documentarlo y no lo entiende ninguna herramienta estándar.
- **Solo códigos HTTP:** un `409` no dice si fue por stock, por SKU o por producto inactivo.

## Consecuencias

- El contrato de errores es predecible y está documentado; agregar un error nuevo es una línea en el mapeador.
- Un usuario puede reportar el `correlationId` y se encuentra su petición exacta en los logs.
- Los mensajes de `detail` están en español porque se muestran al usuario; los `code` no cambian aunque cambie el texto.

/**
 * Configuración de producción. La API se sirve desde el mismo origen que el front
 * (un proxy reenvía /api), por eso la URL base queda vacía.
 */
export const environment = {
  production: true,
  apiUrl: '',
};

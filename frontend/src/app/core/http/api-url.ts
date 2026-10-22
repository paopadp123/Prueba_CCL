import { environment } from '../../../environments/environment';

/**
 * Construye la URL absoluta de un endpoint de la API, p. ej. `apiUrl('/api/stock')`.
 */
export function apiUrl(path: string): string {
  return `${environment.apiUrl}${path}`;
}

/**
 * Indica si la petición va a nuestra API. Solo a esas se les agrega el token:
 * nunca se envía el JWT a un dominio de terceros.
 */
export function isApiRequest(url: string): boolean {
  return url.startsWith(apiUrl('/api/'));
}

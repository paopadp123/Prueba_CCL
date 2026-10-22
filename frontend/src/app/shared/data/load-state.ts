import { catchError, map, Observable, of, OperatorFunction, startWith } from 'rxjs';

import { ApiError } from '../../core/http/api-error';

/**
 * Estado de una carga de datos para pintar "cargando", el resultado o el error
 * sin repetir banderas en cada pantalla.
 */
export type LoadState<T> =
  | { status: 'loading' }
  | { status: 'loaded'; data: T }
  | { status: 'error'; message: string };

/**
 * Envuelve una petición: emite `loading` al empezar y luego `loaded` o `error`.
 * El error se convierte en estado, así un fallo no corta el flujo de búsquedas siguientes.
 */
export function toLoadState<T>(): OperatorFunction<T, LoadState<T>> {
  return (source: Observable<T>) =>
    source.pipe(
      map((data): LoadState<T> => ({ status: 'loaded', data })),
      startWith<LoadState<T>>({ status: 'loading' }),
      catchError((error: unknown) =>
        of<LoadState<T>>({
          status: 'error',
          message: error instanceof ApiError ? error.message : 'No se pudo cargar la información.',
        }),
      ),
    );
}

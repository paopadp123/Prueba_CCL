import { HttpParams } from '@angular/common/http';

/**
 * Página de resultados tal como la devuelve la API.
 */
export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

/**
 * Filtros comunes de los listados.
 */
export interface ListQuery {
  search: string;
  includeInactive: boolean;
  page: number;
  pageSize: number;
}

/**
 * Convierte los filtros en query params, omitiendo la búsqueda vacía.
 */
export function toListParams(query: ListQuery): HttpParams {
  let params = new HttpParams()
    .set('includeInactive', query.includeInactive)
    .set('page', query.page)
    .set('pageSize', query.pageSize);

  const search = query.search.trim();
  if (search) {
    params = params.set('search', search);
  }

  return params;
}

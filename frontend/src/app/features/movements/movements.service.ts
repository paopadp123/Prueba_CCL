import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { apiUrl } from '../../core/http/api-url';
import { PagedResponse } from '../../shared/data/paged';
import { Movement, MovementType, RegisteredMovement, RegisterMovementRequest } from './movement.models';

/**
 * Acceso a `/api/movements` y al historial por producto.
 */
@Injectable({ providedIn: 'root' })
export class MovementsService {
  private readonly http = inject(HttpClient);

  register(type: MovementType, request: RegisterMovementRequest): Observable<RegisteredMovement> {
    const path = type === 'Entry' ? '/api/movements/entries' : '/api/movements/exits';
    return this.http.post<RegisteredMovement>(apiUrl(path), request);
  }

  /**
   * Historial del más reciente al más antiguo; de todos los productos o de uno solo.
   */
  history(page: number, pageSize: number, productId?: number): Observable<PagedResponse<Movement>> {
    const path = productId === undefined ? '/api/movements' : `/api/products/${productId}/movements`;
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResponse<Movement>>(apiUrl(path), { params });
  }
}

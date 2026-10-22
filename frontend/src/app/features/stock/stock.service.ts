import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { apiUrl } from '../../core/http/api-url';
import { ListQuery, PagedResponse, toListParams } from '../../shared/data/paged';

/**
 * Unidades a partir de las cuales un producto se considera con stock bajo.
 * El modelo no tiene un mínimo por producto; es un umbral común para alertas.
 */
export const lowStockThreshold = 10;

export interface StockItem {
  productId: number;
  sku: string;
  name: string;
  unitPrice: number;
  currentStock: number;
  stockValue: number;
  isActive: boolean;
  updatedAt: string;
}

/**
 * Acceso a `/api/stock`.
 */
@Injectable({ providedIn: 'root' })
export class StockService {
  private readonly http = inject(HttpClient);

  list(query: ListQuery): Observable<PagedResponse<StockItem>> {
    return this.http.get<PagedResponse<StockItem>>(apiUrl('/api/stock'), { params: toListParams(query) });
  }
}

import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { apiUrl } from '../../core/http/api-url';
import { ListQuery, PagedResponse, toListParams } from '../../shared/data/paged';
import { CreateProductRequest, Product, UpdateProductRequest } from './product.models';

/**
 * Acceso a `/api/products`.
 */
@Injectable({ providedIn: 'root' })
export class ProductsService {
  private readonly http = inject(HttpClient);

  list(query: ListQuery): Observable<PagedResponse<Product>> {
    return this.http.get<PagedResponse<Product>>(apiUrl('/api/products'), { params: toListParams(query) });
  }

  get(id: number): Observable<Product> {
    return this.http.get<Product>(apiUrl(`/api/products/${id}`));
  }

  create(request: CreateProductRequest): Observable<Product> {
    return this.http.post<Product>(apiUrl('/api/products'), request);
  }

  update(id: number, request: UpdateProductRequest): Observable<Product> {
    return this.http.put<Product>(apiUrl(`/api/products/${id}`), request);
  }

  setActive(id: number, isActive: boolean): Observable<Product> {
    return this.http.patch<Product>(apiUrl(`/api/products/${id}/status`), { isActive });
  }
}

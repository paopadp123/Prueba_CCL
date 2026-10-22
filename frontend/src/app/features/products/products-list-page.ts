import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { debounceTime, switchMap } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { NotificationService } from '../../core/notifications/notification.service';
import { LoadState, toLoadState } from '../../shared/data/load-state';
import { ListQuery, PagedResponse } from '../../shared/data/paged';
import { Pagination } from '../../shared/ui/pagination';
import { Product } from './product.models';
import { ProductsService } from './products.service';

/**
 * Listado de productos con búsqueda, filtro de inactivos y paginación.
 */
@Component({
  selector: 'app-products-list-page',
  imports: [RouterLink, CurrencyPipe, Pagination],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './products-list-page.html',
})
export class ProductsListPage {
  private readonly products = inject(ProductsService);
  private readonly notifications = inject(NotificationService);
  protected readonly auth = inject(AuthService);

  protected readonly search = signal('');
  protected readonly includeInactive = signal(false);
  protected readonly page = signal(1);
  private readonly reloads = signal(0);
  protected readonly busyId = signal<number | null>(null);

  private readonly query = computed<ListQuery>(() => {
    this.reloads();
    return { search: this.search(), includeInactive: this.includeInactive(), page: this.page(), pageSize: 10 };
  });

  /**
   * Cada cambio de filtros dispara una búsqueda; `switchMap` cancela la anterior si aún no terminó
   * y `debounceTime` espera a que el usuario deje de escribir.
   */
  protected readonly state = toSignal(
    toObservable(this.query).pipe(
      debounceTime(250),
      switchMap((query) => this.products.list(query).pipe(toLoadState())),
    ),
    { initialValue: { status: 'loading' } as LoadState<PagedResponse<Product>> },
  );

  protected onSearch(value: string): void {
    this.search.set(value);
    this.page.set(1);
  }

  protected onIncludeInactive(value: boolean): void {
    this.includeInactive.set(value);
    this.page.set(1);
  }

  protected toggleActive(product: Product): void {
    this.busyId.set(product.id);
    this.products.setActive(product.id, !product.isActive).subscribe({
      next: (updated) => {
        this.busyId.set(null);
        this.notifications.success(
          `${updated.name} quedó ${updated.isActive ? 'activo' : 'inactivo'}.`,
        );
        this.reloads.update((count) => count + 1);
      },
      error: (error: unknown) => {
        this.busyId.set(null);
        this.notifications.failure(error);
      },
    });
  }
}

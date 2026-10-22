import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { switchMap } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { LoadState, toLoadState } from '../../shared/data/load-state';
import { PagedResponse } from '../../shared/data/paged';
import { Pagination } from '../../shared/ui/pagination';
import { Movement } from '../movements/movement.models';
import { MovementTable } from '../movements/movement-table';
import { MovementsService } from '../movements/movements.service';
import { Product } from './product.models';
import { ProductsService } from './products.service';

/**
 * Ficha del producto: datos, stock, valor del inventario y su historial de movimientos.
 */
@Component({
  selector: 'app-product-detail-page',
  imports: [RouterLink, CurrencyPipe, DatePipe, Pagination, MovementTable],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './product-detail-page.html',
})
export class ProductDetailPage {
  private readonly products = inject(ProductsService);
  private readonly movements = inject(MovementsService);
  protected readonly auth = inject(AuthService);

  /** Id de la ruta `/products/:id`. */
  readonly id = input.required<string>();

  protected readonly historyPage = signal(1);
  private readonly productId = computed(() => Number(this.id()));

  protected readonly product = toSignal(
    toObservable(this.productId).pipe(switchMap((id) => this.products.get(id).pipe(toLoadState()))),
    { initialValue: { status: 'loading' } as LoadState<Product> },
  );

  protected readonly history = toSignal(
    toObservable(computed(() => ({ id: this.productId(), page: this.historyPage() }))).pipe(
      switchMap(({ id, page }) => this.movements.history(page, 10, id).pipe(toLoadState())),
    ),
    { initialValue: { status: 'loading' } as LoadState<PagedResponse<Movement>> },
  );
}

import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { debounceTime, switchMap } from 'rxjs';

import { LoadState, toLoadState } from '../../shared/data/load-state';
import { ListQuery, PagedResponse } from '../../shared/data/paged';
import { Icon } from '../../shared/ui/icon';
import { Pagination, resultsLabel } from '../../shared/ui/pagination';
import { lowStockThreshold, StockItem, StockService } from './stock.service';

/**
 * Stock actual de los productos con su valor en inventario.
 */
@Component({
  selector: 'app-stock-page',
  imports: [RouterLink, CurrencyPipe, Pagination, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './stock-page.html',
})
export class StockPage {
  private readonly stock = inject(StockService);

  protected readonly lowStockThreshold = lowStockThreshold;
  protected readonly resultsLabel = resultsLabel;

  protected readonly search = signal('');
  protected readonly includeInactive = signal(false);
  protected readonly page = signal(1);

  private readonly query = computed<ListQuery>(() => ({
    search: this.search(),
    includeInactive: this.includeInactive(),
    page: this.page(),
    pageSize: 15,
  }));

  protected readonly state = toSignal(
    toObservable(this.query).pipe(
      debounceTime(250),
      switchMap((query) => this.stock.list(query).pipe(toLoadState())),
    ),
    { initialValue: { status: 'loading' } as LoadState<PagedResponse<StockItem>> },
  );

  protected onSearch(value: string): void {
    this.search.set(value);
    this.page.set(1);
  }

  protected onIncludeInactive(value: boolean): void {
    this.includeInactive.set(value);
    this.page.set(1);
  }
}

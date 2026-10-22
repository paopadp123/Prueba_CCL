import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { LoadState, toLoadState } from '../../shared/data/load-state';
import { PagedResponse } from '../../shared/data/paged';
import { Icon } from '../../shared/ui/icon';
import { Movement, movementTypeLabels } from '../movements/movement.models';
import { MovementsService } from '../movements/movements.service';
import { lowStockThreshold, StockItem, StockService } from '../stock/stock.service';
import { DailyMovementsChart } from './charts/daily-movements-chart';
import { DonutChart } from './charts/donut-chart';
import { inventoryValue, lowStock, movementsByDay, totalUnits, valueShare } from './dashboard';

/** Máximo que permite la API por página; el tablero trabaja sobre esa muestra. */
const sampleSize = 200;

interface DashboardData {
  stock: PagedResponse<StockItem>;
  movements: PagedResponse<Movement>;
}

/**
 * Inicio: accesos directos y un tablero con el estado del inventario
 * (stock bajo, valor, movimientos de la semana, últimos movimientos y alertas).
 */
@Component({
  selector: 'app-home-page',
  imports: [RouterLink, CurrencyPipe, DatePipe, Icon, DonutChart, DailyMovementsChart],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './home-page.html',
  styleUrl: './home-page.scss',
})
export class HomePage {
  protected readonly auth = inject(AuthService);
  private readonly stock = inject(StockService);
  private readonly movements = inject(MovementsService);

  protected readonly labels = movementTypeLabels;
  protected readonly lowStockThreshold = lowStockThreshold;

  protected readonly state = toSignal(
    forkJoin({
      stock: this.stock.list({ search: '', includeInactive: false, page: 1, pageSize: sampleSize }),
      movements: this.movements.history(1, sampleSize),
    }).pipe(toLoadState()),
    { initialValue: { status: 'loading' } as LoadState<DashboardData> },
  );

  private readonly data = computed(() => {
    const state = this.state();
    return state.status === 'loaded' ? state.data : null;
  });

  private readonly items = computed(() => this.data()?.stock.items ?? []);
  private readonly history = computed(() => this.data()?.movements.items ?? []);

  protected readonly lowStock = computed(() => lowStock(this.items(), lowStockThreshold));
  protected readonly outOfStock = computed(() => this.lowStock().filter((item) => item.currentStock === 0));
  protected readonly lowStockMax = computed(() => Math.max(1, ...this.lowStock().map((item) => item.currentStock)));
  protected readonly totalValue = computed(() => inventoryValue(this.items()));
  protected readonly totalUnits = computed(() => totalUnits(this.items()));
  protected readonly productCount = computed(() => this.data()?.stock.totalCount ?? 0);
  protected readonly slices = computed(() => valueShare(this.items()));
  protected readonly week = computed(() => movementsByDay(this.history(), new Date()));
  protected readonly weekEntries = computed(() => this.week().reduce((sum, day) => sum + day.entries, 0));
  protected readonly weekExits = computed(() => this.week().reduce((sum, day) => sum + day.exits, 0));
  protected readonly latest = computed(() => this.history().slice(0, 5));

  /** Si hay más datos que la muestra, los totales del tablero son parciales y se avisa. */
  protected readonly partial = computed(() => {
    const data = this.data();
    return !!data && (data.stock.totalCount > sampleSize || data.movements.totalCount > sampleSize);
  });
}

import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { switchMap } from 'rxjs';

import { LoadState, toLoadState } from '../../shared/data/load-state';
import { PagedResponse } from '../../shared/data/paged';
import { Pagination } from '../../shared/ui/pagination';
import { Movement } from './movement.models';
import { MovementTable } from './movement-table';
import { MovementsService } from './movements.service';

/**
 * Historial de todos los movimientos, del más reciente al más antiguo.
 */
@Component({
  selector: 'app-movements-page',
  imports: [RouterLink, Pagination, MovementTable],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header class="page-header">
      <div>
        <h1>Movimientos</h1>
        <p class="muted">Entradas y salidas registradas, de la más reciente a la más antigua.</p>
      </div>
      <div class="header-actions">
        <a class="button button--ghost" routerLink="new" [queryParams]="{ type: 'Entry' }">Registrar entrada</a>
        <a class="button button--primary" routerLink="new" [queryParams]="{ type: 'Exit' }">Registrar salida</a>
      </div>
    </header>

    <section class="card">
      @let current = state();
      @if (current.status === 'loading') {
        <p class="muted" role="status">Cargando movimientos…</p>
      } @else if (current.status === 'error') {
        <div class="alert alert--error" role="alert">{{ current.message }}</div>
      } @else if (current.data.items.length === 0) {
        <p class="empty">Todavía no hay movimientos registrados.</p>
      } @else {
        <app-movement-table [movements]="current.data.items" />
        <app-pagination
          [page]="current.data.page"
          [totalPages]="current.data.totalPages"
          [totalCount]="current.data.totalCount"
          (pageChange)="page.set($event)"
        />
      }
    </section>
  `,
})
export class MovementsPage {
  private readonly movements = inject(MovementsService);

  protected readonly page = signal(1);

  protected readonly state = toSignal(
    toObservable(this.page).pipe(switchMap((page) => this.movements.history(page, 15).pipe(toLoadState()))),
    { initialValue: { status: 'loading' } as LoadState<PagedResponse<Movement>> },
  );
}

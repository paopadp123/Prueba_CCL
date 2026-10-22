import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { Icon } from './icon';

/**
 * Paginador compacto: ← Anterior · Página X de Y · Siguiente →
 */
@Component({
  selector: 'app-pagination',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <nav class="pagination" aria-label="Paginación">
      <button type="button" class="pagination__step" [disabled]="page() <= 1" (click)="pageChange.emit(page() - 1)">
        <app-icon name="arrowLeft" [size]="16" /> Anterior
      </button>
      <span class="pagination__current">Página {{ page() }} de {{ lastPage() }}</span>
      <button
        type="button"
        class="pagination__step"
        [disabled]="page() >= lastPage()"
        (click)="pageChange.emit(page() + 1)"
      >
        Siguiente <app-icon name="arrowRight" [size]="16" />
      </button>
    </nav>
  `,
  styles: `
    .pagination {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 0.5rem 1rem;
    }
    .pagination__current {
      font-weight: 600;
      white-space: nowrap;
    }
    .pagination__step {
      display: inline-flex;
      align-items: center;
      gap: 0.375rem;
      padding: 0.375rem 0.5rem;
      border: 0;
      border-radius: var(--radius);
      background: none;
      color: var(--muted);
      font: inherit;
      cursor: pointer;
    }
    .pagination__step:hover:not(:disabled) {
      background: var(--primary-soft);
      color: var(--primary);
    }
    .pagination__step:disabled {
      opacity: 0.45;
      cursor: not-allowed;
    }
  `,
})
export class Pagination {
  readonly page = input.required<number>();
  readonly totalPages = input.required<number>();
  readonly pageChange = output<number>();

  protected readonly lastPage = computed(() => Math.max(this.totalPages(), 1));
}

/**
 * Texto con el total de resultados ("1 resultado", "6 resultados").
 */
export function resultsLabel(count: number): string {
  return count === 1 ? '1 resultado' : `${count} resultados`;
}

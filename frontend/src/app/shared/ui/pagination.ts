import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

/**
 * Paginador simple: anterior / siguiente y el total de resultados.
 */
@Component({
  selector: 'app-pagination',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <nav class="pagination" aria-label="Paginación">
      <span class="muted">{{ summary() }}</span>
      <div class="pagination__buttons">
        <button type="button" class="button button--ghost" [disabled]="page() <= 1" (click)="pageChange.emit(page() - 1)">
          Anterior
        </button>
        <span>Página {{ page() }} de {{ lastPage() }}</span>
        <button
          type="button"
          class="button button--ghost"
          [disabled]="page() >= lastPage()"
          (click)="pageChange.emit(page() + 1)"
        >
          Siguiente
        </button>
      </div>
    </nav>
  `,
  styles: `
    .pagination {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      justify-content: space-between;
      gap: 0.75rem;
      margin-top: 1rem;
    }
    .pagination__buttons {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }
  `,
})
export class Pagination {
  readonly page = input.required<number>();
  readonly totalPages = input.required<number>();
  readonly totalCount = input.required<number>();
  readonly pageChange = output<number>();

  protected readonly lastPage = computed(() => Math.max(this.totalPages(), 1));
  protected readonly summary = computed(() =>
    this.totalCount() === 1 ? '1 resultado' : `${this.totalCount()} resultados`,
  );
}

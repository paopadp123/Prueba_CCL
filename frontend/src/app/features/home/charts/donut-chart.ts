import { CurrencyPipe, PercentPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';

import { ValueSlice } from '../dashboard';

interface Arc {
  slice: ValueSlice;
  dash: string;
  offset: number;
}

const radius = 40;
const circumference = 2 * Math.PI * radius;
/** Separación entre segmentos para que cada color se distinga aunque sean parecidos. */
const gap = 1.5;

/**
 * Donut con leyenda. Al pasar el mouse por un segmento o por su fila de la leyenda,
 * el centro muestra ese producto; si no, el total.
 */
@Component({
  selector: 'app-donut-chart',
  imports: [CurrencyPipe, PercentPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="donut">
      <svg viewBox="0 0 100 100" role="img" [attr.aria-label]="ariaLabel()">
        <circle cx="50" cy="50" [attr.r]="radius" class="donut__track" />
        @for (arc of arcs(); track arc.slice.label) {
          <circle
            cx="50"
            cy="50"
            [attr.r]="radius"
            class="donut__arc"
            [class.is-dimmed]="active() !== null && active() !== arc.slice"
            [attr.stroke]="arc.slice.color"
            [attr.stroke-dasharray]="arc.dash"
            [attr.stroke-dashoffset]="arc.offset"
            (mouseenter)="active.set(arc.slice)"
            (mouseleave)="active.set(null)"
          />
        }
      </svg>
      <div class="donut__center">
        @if (active(); as slice) {
          <strong>{{ slice.share | percent: '1.0-1' }}</strong>
          <span>{{ slice.label }}</span>
        } @else {
          <strong>{{ total() }}</strong>
          <span>total</span>
        }
      </div>
    </div>

    <ul class="legend">
      @for (slice of slices(); track slice.label) {
        <li (mouseenter)="active.set(slice)" (mouseleave)="active.set(null)">
          <span class="legend__swatch" [style.background]="slice.color"></span>
          <span class="legend__label">{{ slice.label }}</span>
          <span class="legend__value">{{ slice.value | currency: 'COP' : 'symbol-narrow' : '1.0-0' }}</span>
          <span class="legend__share">{{ slice.share | percent: '1.0-1' }}</span>
        </li>
      }
    </ul>
  `,
  styles: `
    :host { display: flex; flex-wrap: wrap; align-items: center; gap: 1rem; }
    .donut { position: relative; width: 8.5rem; flex-shrink: 0; }
    svg { display: block; transform: rotate(-90deg); }
    .donut__track { fill: none; stroke: var(--bg); stroke-width: 14; }
    .donut__arc { fill: none; stroke-width: 14; cursor: pointer; transition: opacity 0.15s; }
    .donut__arc.is-dimmed { opacity: 0.35; }
    .donut__center {
      position: absolute; inset: 0; display: flex; flex-direction: column;
      align-items: center; justify-content: center; padding: 1.75rem; text-align: center;
      pointer-events: none;
    }
    .donut__center strong { font-size: 1.125rem; }
    .donut__center span {
      max-width: 100%; overflow: hidden; color: var(--muted); font-size: 0.75rem;
      text-overflow: ellipsis; white-space: nowrap;
    }
    .legend { flex: 1 1 10rem; margin: 0; padding: 0; list-style: none; font-size: 0.8125rem; }
    .legend li {
      display: grid; grid-template-columns: auto 1fr auto auto; align-items: center;
      gap: 0.5rem; padding: 0.1875rem 0; cursor: default;
    }
    .legend__swatch { width: 0.625rem; height: 0.625rem; border-radius: 3px; }
    .legend__label { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .legend__value { color: var(--muted); font-variant-numeric: tabular-nums; }
    .legend__share { min-width: 2.75rem; font-weight: 600; text-align: right; font-variant-numeric: tabular-nums; }
  `,
})
export class DonutChart {
  readonly slices = input.required<ValueSlice[]>();

  protected readonly radius = radius;
  protected readonly active = signal<ValueSlice | null>(null);

  protected readonly arcs = computed<Arc[]>(() => {
    let start = 0;
    return this.slices().map((slice) => {
      const length = slice.share * circumference;
      const visible = Math.max(length - gap, 0.5);
      const arc: Arc = { slice, dash: `${visible} ${circumference - visible}`, offset: -start };
      start += length;
      return arc;
    });
  });

  protected readonly total = computed(() =>
    new Intl.NumberFormat('es-CO', { notation: 'compact', style: 'currency', currency: 'COP' }).format(
      this.slices().reduce((sum, slice) => sum + slice.value, 0),
    ),
  );

  protected readonly ariaLabel = computed(() =>
    'Valor del inventario por producto: ' +
    this.slices()
      .map((slice) => `${slice.label} ${Math.round(slice.share * 100)}%`)
      .join(', '),
  );
}

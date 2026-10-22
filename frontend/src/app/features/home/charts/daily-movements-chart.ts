import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';

import { DayMovements } from '../dashboard';

interface Column {
  day: DayMovements;
  x: number;
  entries: string;
  exits: string;
}

const width = 280;
const height = 150;
const plotTop = 8;
const plotBottom = 128;
const barWidth = 11;

/**
 * Barras agrupadas por día: entradas (serie 1) y salidas (serie 2), en unidades.
 * Al pasar el mouse por un día se muestra su detalle.
 */
@Component({
  selector: 'app-daily-movements-chart',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="chart">
      <svg [attr.viewBox]="'0 0 ' + width + ' ' + height" role="img" [attr.aria-label]="ariaLabel()">
        @for (line of gridLines(); track line.y) {
          <line [attr.x1]="0" [attr.x2]="width" [attr.y1]="line.y" [attr.y2]="line.y" class="grid" />
          <text [attr.x]="0" [attr.y]="line.y - 3" class="axis">{{ line.label }}</text>
        }
        @for (column of columns(); track column.day.key) {
          <g (mouseenter)="active.set(column)" (mouseleave)="active.set(null)">
            <rect [attr.x]="column.x - 18" y="0" width="36" [attr.height]="height" class="hit" [class.is-active]="active() === column" />
            <path [attr.d]="column.entries" fill="var(--series-1)" />
            <path [attr.d]="column.exits" fill="var(--series-2)" />
            <text [attr.x]="column.x" [attr.y]="height - 4" class="axis axis--x">{{ column.day.label }}</text>
          </g>
        }
        <line x1="0" [attr.x2]="width" [attr.y1]="plotBottom" [attr.y2]="plotBottom" class="baseline" />
      </svg>

      @if (active(); as column) {
        <div class="tooltip" [style.left.%]="(column.x / width) * 100">
          <strong>{{ column.day.label }}</strong>
          <span><i class="swatch" style="background: var(--series-1)"></i>Entradas: {{ column.day.entries }}</span>
          <span><i class="swatch" style="background: var(--series-2)"></i>Salidas: {{ column.day.exits }}</span>
        </div>
      }
    </div>
  `,
  styles: `
    .chart { position: relative; }
    svg { display: block; width: 100%; height: auto; overflow: visible; }
    .grid { stroke: var(--border); stroke-dasharray: 2 3; }
    .baseline { stroke: #c5cdd8; }
    .axis { fill: var(--muted); font-size: 9px; }
    .axis--x { text-anchor: middle; text-transform: capitalize; }
    .hit { fill: transparent; }
    .hit.is-active { fill: var(--surface-tint); }
    .tooltip {
      position: absolute; top: 0; z-index: 2; display: flex; flex-direction: column; gap: 0.125rem;
      padding: 0.5rem 0.625rem; border: 1px solid var(--border); border-radius: var(--radius);
      background: var(--surface); box-shadow: var(--shadow); font-size: 0.75rem;
      white-space: nowrap; transform: translateX(-50%); pointer-events: none;
    }
    .tooltip strong { text-transform: capitalize; }
    .swatch { display: inline-block; width: 0.5rem; height: 0.5rem; margin-right: 0.375rem; border-radius: 2px; }
  `,
})
export class DailyMovementsChart {
  readonly days = input.required<DayMovements[]>();

  protected readonly width = width;
  protected readonly height = height;
  protected readonly plotBottom = plotBottom;
  protected readonly active = signal<Column | null>(null);

  /** Máximo redondeado hacia arriba para que las líneas guía caigan en números limpios. */
  private readonly maxValue = computed(() => {
    const max = Math.max(1, ...this.days().flatMap((day) => [day.entries, day.exits]));
    const magnitude = 10 ** Math.floor(Math.log10(max));
    return Math.ceil(max / magnitude) * magnitude;
  });

  protected readonly gridLines = computed(() =>
    [0.5, 1].map((fraction) => ({
      y: this.scale(this.maxValue() * fraction),
      label: compact(this.maxValue() * fraction),
    })),
  );

  protected readonly columns = computed<Column[]>(() => {
    const days = this.days();
    const step = width / days.length;
    return days.map((day, index) => {
      const x = step * index + step / 2;
      return {
        day,
        x,
        entries: bar(x - barWidth - 1, this.scale(day.entries)),
        exits: bar(x + 1, this.scale(day.exits)),
      };
    });
  });

  protected readonly ariaLabel = computed(() =>
    'Unidades por día en los últimos 7 días: ' +
    this.days()
      .map((day) => `${day.label}, ${day.entries} entradas y ${day.exits} salidas`)
      .join('; '),
  );

  private scale(value: number): number {
    return plotBottom - (value / this.maxValue()) * (plotBottom - plotTop);
  }
}

/**
 * Barra con el extremo superior redondeado y la base recta, apoyada en la línea base.
 */
function bar(x: number, top: number): string {
  const heightPx = plotBottom - top;
  if (heightPx <= 0) {
    return '';
  }

  const r = Math.min(3, heightPx, barWidth / 2);
  return (
    `M${x},${plotBottom}V${top + r}Q${x},${top} ${x + r},${top}` +
    `H${x + barWidth - r}Q${x + barWidth},${top} ${x + barWidth},${top + r}V${plotBottom}Z`
  );
}

function compact(value: number): string {
  return new Intl.NumberFormat('es-CO', { notation: 'compact' }).format(value);
}

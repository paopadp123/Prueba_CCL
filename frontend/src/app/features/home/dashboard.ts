import { Movement } from '../movements/movement.models';
import { StockItem } from '../stock/stock.service';

/**
 * Cálculos del tablero de inicio. Son funciones puras sobre los datos de la API,
 * así se prueban sin componentes ni HTTP.
 */

export interface ValueSlice {
  label: string;
  value: number;
  share: number;
  color: string;
}

export interface DayMovements {
  key: string;
  label: string;
  entries: number;
  exits: number;
}

/** Colores de serie en orden fijo (paleta validada para daltonismo); "Otros" va en gris. */
const sliceColors = ['var(--series-1)', 'var(--series-2)', 'var(--series-3)', 'var(--series-4)'];
const otherColor = 'var(--series-other)';

/**
 * Productos con stock en o por debajo del umbral, del más crítico al menos crítico.
 */
export function lowStock(items: StockItem[], threshold: number): StockItem[] {
  return items
    .filter((item) => item.currentStock <= threshold)
    .sort((a, b) => a.currentStock - b.currentStock || a.name.localeCompare(b.name));
}

export function inventoryValue(items: StockItem[]): number {
  return items.reduce((total, item) => total + item.stockValue, 0);
}

export function totalUnits(items: StockItem[]): number {
  return items.reduce((total, item) => total + item.currentStock, 0);
}

/**
 * Reparto del valor del inventario: los `top` productos de mayor valor y el resto
 * agrupado en "Otros", para no pasar de cinco colores.
 */
export function valueShare(items: StockItem[], top = 4): ValueSlice[] {
  const total = inventoryValue(items);
  if (total <= 0) {
    return [];
  }

  const sorted = items.filter((item) => item.stockValue > 0).sort((a, b) => b.stockValue - a.stockValue);
  const slices: ValueSlice[] = sorted.slice(0, top).map((item, index) => ({
    label: item.name,
    value: item.stockValue,
    share: item.stockValue / total,
    color: sliceColors[index],
  }));

  const rest = sorted.slice(top).reduce((sum, item) => sum + item.stockValue, 0);
  if (rest > 0) {
    slices.push({ label: 'Otros', value: rest, share: rest / total, color: otherColor });
  }

  return slices;
}

/**
 * Unidades que entraron y salieron por día en los últimos `days` días (hora local),
 * incluyendo los días sin movimientos.
 */
export function movementsByDay(movements: Movement[], today: Date, days = 7): DayMovements[] {
  const weekday = new Intl.DateTimeFormat('es-CO', { weekday: 'short' });
  const result: DayMovements[] = [];

  for (let offset = days - 1; offset >= 0; offset--) {
    const date = new Date(today.getFullYear(), today.getMonth(), today.getDate() - offset);
    result.push({ key: dayKey(date), label: weekday.format(date).replace('.', ''), entries: 0, exits: 0 });
  }

  const byKey = new Map(result.map((day) => [day.key, day]));
  for (const movement of movements) {
    const day = byKey.get(dayKey(new Date(movement.createdAt)));
    if (day) {
      if (movement.type === 'Entry') {
        day.entries += movement.quantity;
      } else {
        day.exits += movement.quantity;
      }
    }
  }

  return result;
}

function dayKey(date: Date): string {
  return `${date.getFullYear()}-${date.getMonth() + 1}-${date.getDate()}`;
}

import { Movement } from '../movements/movement.models';
import { StockItem } from '../stock/stock.service';
import { inventoryValue, lowStock, movementsByDay, valueShare } from './dashboard';

function stock(name: string, currentStock: number, stockValue: number): StockItem {
  return {
    productId: name.length,
    sku: name.toUpperCase(),
    name,
    unitPrice: 1,
    currentStock,
    stockValue,
    isActive: true,
    updatedAt: '2026-10-21T10:00:00Z',
  };
}

function movement(type: 'Entry' | 'Exit', quantity: number, createdAt: Date): Movement {
  return {
    movementId: 1,
    productId: 1,
    sku: 'LAP-001',
    productName: 'Portátil',
    type,
    quantity,
    stockAfter: 0,
    reason: null,
    username: 'admin',
    createdAt: createdAt.toISOString(),
  };
}

describe('dashboard', () => {
  it('lowStock devuelve solo los que están en o bajo el umbral, del más crítico al menos', () => {
    const items = [stock('a', 12, 0), stock('b', 3, 0), stock('c', 0, 0), stock('d', 10, 0)];

    expect(lowStock(items, 10).map((item) => item.name)).toEqual(['c', 'b', 'd']);
  });

  it('valueShare toma los 4 de mayor valor y agrupa el resto en "Otros"', () => {
    const items = [
      stock('a', 1, 500),
      stock('b', 1, 200),
      stock('c', 1, 100),
      stock('d', 1, 100),
      stock('e', 1, 60),
      stock('f', 1, 40),
      stock('g', 1, 0),
    ];

    const slices = valueShare(items);

    expect(inventoryValue(items)).toBe(1000);
    expect(slices.map((slice) => slice.label)).toEqual(['a', 'b', 'c', 'd', 'Otros']);
    expect(slices[4].value).toBe(100);
    expect(slices.reduce((sum, slice) => sum + slice.share, 0)).toBeCloseTo(1);
  });

  it('valueShare sin valor en inventario no devuelve segmentos', () => {
    expect(valueShare([stock('a', 0, 0)])).toEqual([]);
  });

  it('movementsByDay suma unidades por día e incluye los días sin movimientos', () => {
    const today = new Date(2026, 9, 21, 15, 0);
    const days = movementsByDay(
      [
        movement('Entry', 10, new Date(2026, 9, 21, 9, 0)),
        movement('Exit', 4, new Date(2026, 9, 21, 11, 0)),
        movement('Entry', 5, new Date(2026, 9, 19, 8, 0)),
        movement('Entry', 99, new Date(2026, 9, 10, 8, 0)),
      ],
      today,
    );

    expect(days.length).toBe(7);
    expect(days[6]).toEqual(jasmine.objectContaining({ entries: 10, exits: 4 }));
    expect(days[4]).toEqual(jasmine.objectContaining({ entries: 5, exits: 0 }));
    expect(days.reduce((sum, day) => sum + day.entries, 0)).toBe(15);
  });
});

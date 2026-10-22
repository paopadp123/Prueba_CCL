import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

import { Movement, movementTypeLabels } from './movement.models';

/**
 * Tabla de movimientos; se usa en el historial general y en el detalle de un producto.
 */
@Component({
  selector: 'app-movement-table',
  imports: [DatePipe, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <table class="data-table">
      <thead>
        <tr>
          <th>Fecha</th>
          <th>Tipo</th>
          @if (showProduct()) {
            <th>Producto</th>
          }
          <th class="num">Cantidad</th>
          <th class="num">Stock después</th>
          <th>Motivo</th>
          <th>Usuario</th>
        </tr>
      </thead>
      <tbody>
        @for (movement of movements(); track movement.movementId) {
          <tr>
            <td data-label="Fecha">{{ movement.createdAt | date: 'short' }}</td>
            <td data-label="Tipo">
              <span class="badge" [class.badge--danger]="movement.type === 'Exit'" [class.badge--success]="movement.type === 'Entry'">
                {{ labels[movement.type] }}
              </span>
            </td>
            @if (showProduct()) {
              <td data-label="Producto">
                <a [routerLink]="['/products', movement.productId]">{{ movement.productName }}</a>
                <small class="muted"> · {{ movement.sku }}</small>
              </td>
            }
            <td data-label="Cantidad" class="num">
              {{ movement.type === 'Exit' ? '−' : '+' }}{{ movement.quantity }}
            </td>
            <td data-label="Stock después" class="num">{{ movement.stockAfter }}</td>
            <td data-label="Motivo">{{ movement.reason || '—' }}</td>
            <td data-label="Usuario">{{ movement.username }}</td>
          </tr>
        }
      </tbody>
    </table>
  `,
})
export class MovementTable {
  readonly movements = input.required<Movement[]>();
  readonly showProduct = input(true);

  protected readonly labels = movementTypeLabels;
}

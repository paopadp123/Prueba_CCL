import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';

/**
 * Página de inicio después del login, con accesos a cada sección.
 */
@Component({
  selector: 'app-home-page',
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (auth.user(); as user) {
      <header class="page-header">
        <div>
          <h1>Hola, {{ user.fullName }}</h1>
          <p class="muted">
            @if (auth.isAdmin()) {
              Como administrador puedes gestionar el catálogo de productos y registrar movimientos.
            } @else {
              Como operador puedes consultar productos y stock, y registrar entradas y salidas.
            }
          </p>
        </div>
      </header>
    }

    <section class="tiles">
      <a class="card tile" routerLink="/products">
        <strong>Productos</strong>
        <span class="muted">{{ auth.isAdmin() ? 'Crear, editar y activar productos.' : 'Consultar el catálogo.' }}</span>
      </a>
      <a class="card tile" routerLink="/movements/new" [queryParams]="{ type: 'Entry' }">
        <strong>Registrar entrada</strong>
        <span class="muted">Ingreso de unidades a bodega.</span>
      </a>
      <a class="card tile" routerLink="/movements/new" [queryParams]="{ type: 'Exit' }">
        <strong>Registrar salida</strong>
        <span class="muted">Despacho de unidades; no permite dejar stock negativo.</span>
      </a>
      <a class="card tile" routerLink="/stock">
        <strong>Stock</strong>
        <span class="muted">Unidades disponibles y valor del inventario.</span>
      </a>
      <a class="card tile" routerLink="/movements">
        <strong>Historial</strong>
        <span class="muted">Todas las entradas y salidas con su usuario.</span>
      </a>
    </section>
  `,
})
export class HomePage {
  protected readonly auth = inject(AuthService);
}

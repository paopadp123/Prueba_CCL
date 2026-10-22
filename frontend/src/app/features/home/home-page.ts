import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { AuthService } from '../../core/auth/auth.service';

/**
 * Página de inicio después del login.
 */
@Component({
  selector: 'app-home-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (auth.user(); as user) {
      <section class="card">
        <h1>Hola, {{ user.fullName }}</h1>
        <p class="muted">
          @if (auth.isAdmin()) {
            Como administrador puedes gestionar el catálogo de productos y registrar movimientos.
          } @else {
            Como operador puedes consultar productos y stock, y registrar entradas y salidas.
          }
        </p>
      </section>
    }
  `,
})
export class HomePage {
  protected readonly auth = inject(AuthService);
}

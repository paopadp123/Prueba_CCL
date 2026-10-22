import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';

import { Icon, IconName } from '../../shared/ui/icon';
import { AuthService } from '../auth/auth.service';
import { ThemePicker } from '../theme/theme-picker';
import { initialsOf } from './initials';
import { UserMenu } from './user-menu';

interface NavItem {
  path: string;
  label: string;
  icon: IconName;
  exact: boolean;
}

/**
 * Estructura de las pantallas con sesión. En escritorio muestra el menú en la barra
 * superior y el menú de usuario; en celular, un botón hamburguesa abre un panel lateral.
 */
@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, DatePipe, Icon, ThemePicker, UserMenu],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
  host: {
    '(document:keydown.escape)': 'mobileMenuOpen.set(false)',
  },
})
export class Shell {
  protected readonly auth = inject(AuthService);

  protected readonly navItems: NavItem[] = [
    { path: '/', label: 'Inicio', icon: 'home', exact: true },
    { path: '/products', label: 'Productos', icon: 'box', exact: false },
    { path: '/movements', label: 'Movimientos', icon: 'history', exact: false },
    { path: '/stock', label: 'Stock', icon: 'stock', exact: false },
  ];

  protected readonly mobileMenuOpen = signal(false);
  protected readonly initials = computed(() => initialsOf(this.auth.user()?.fullName));
  protected readonly roleLabel = computed(() => (this.auth.isAdmin() ? 'Administrador' : 'Operador'));

  constructor() {
    // Al navegar se cierra el menú lateral.
    inject(Router)
      .events.pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe(() => this.mobileMenuOpen.set(false));
  }

  protected toggleMobileMenu(): void {
    this.mobileMenuOpen.update((open) => !open);
  }
}

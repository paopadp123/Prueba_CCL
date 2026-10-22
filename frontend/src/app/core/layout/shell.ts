import { ChangeDetectionStrategy, Component, computed, ElementRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';

import { Icon, IconName } from '../../shared/ui/icon';
import { AuthService } from '../auth/auth.service';

interface NavItem {
  path: string;
  label: string;
  icon: IconName;
  exact: boolean;
}

/**
 * Estructura de las pantallas con sesión. En escritorio muestra el menú en la barra
 * superior y un menú de usuario; en celular, un botón hamburguesa abre un panel lateral.
 */
@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
  host: {
    '(document:keydown.escape)': 'closeMenus()',
    '(document:click)': 'onDocumentClick($event)',
  },
})
export class Shell {
  protected readonly auth = inject(AuthService);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  protected readonly navItems: NavItem[] = [
    { path: '/', label: 'Inicio', icon: 'home', exact: true },
    { path: '/products', label: 'Productos', icon: 'box', exact: false },
    { path: '/movements', label: 'Movimientos', icon: 'history', exact: false },
    { path: '/stock', label: 'Stock', icon: 'stock', exact: false },
  ];

  protected readonly mobileMenuOpen = signal(false);
  protected readonly userMenuOpen = signal(false);

  protected readonly initials = computed(() =>
    (this.auth.user()?.fullName ?? '')
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((word) => word[0].toUpperCase())
      .join(''),
  );

  protected readonly roleLabel = computed(() => (this.auth.isAdmin() ? 'Administrador' : 'Operador'));

  constructor() {
    // Al navegar se cierran los menús abiertos.
    inject(Router)
      .events.pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe(() => this.closeMenus());
  }

  protected toggleMobileMenu(): void {
    this.mobileMenuOpen.update((open) => !open);
  }

  protected toggleUserMenu(event: Event): void {
    event.stopPropagation();
    this.userMenuOpen.update((open) => !open);
  }

  protected closeMenus(): void {
    this.mobileMenuOpen.set(false);
    this.userMenuOpen.set(false);
  }

  protected onDocumentClick(event: Event): void {
    const menu = this.host.nativeElement.querySelector('.user-menu');
    if (this.userMenuOpen() && menu && !menu.contains(event.target as Node)) {
      this.userMenuOpen.set(false);
    }
  }
}

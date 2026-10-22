import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, ElementRef, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink } from '@angular/router';
import { filter } from 'rxjs';

import { Icon } from '../../shared/ui/icon';
import { AuthService } from '../auth/auth.service';
import { ThemePicker } from '../theme/theme-picker';
import { initialsOf } from './initials';

/** Elementos que reciben foco dentro del panel. */
const focusableSelector = 'a[href], button:not([disabled])';

/**
 * Menú de usuario de escritorio: datos de la sesión, atajos según el rol, tema y cierre de sesión.
 * Sigue el patrón "disclosure" (botón con aria-expanded que muestra un panel), con flechas para
 * recorrerlo y Escape para cerrarlo devolviendo el foco al botón.
 */
@Component({
  selector: 'app-user-menu',
  imports: [RouterLink, DatePipe, Icon, ThemePicker],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './user-menu.html',
  styleUrl: './user-menu.scss',
  host: {
    '(document:keydown.escape)': 'onEscape()',
    '(document:click)': 'onDocumentClick($event)',
  },
})
export class UserMenu {
  protected readonly auth = inject(AuthService);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  private readonly trigger = viewChild.required<ElementRef<HTMLButtonElement>>('trigger');
  private readonly panel = viewChild<ElementRef<HTMLElement>>('panel');

  protected readonly open = signal(false);
  protected readonly initials = computed(() => initialsOf(this.auth.user()?.fullName));
  protected readonly roleLabel = computed(() => (this.auth.isAdmin() ? 'Administrador' : 'Operador'));

  constructor() {
    inject(Router)
      .events.pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe(() => this.open.set(false));
  }

  protected toggle(event: Event): void {
    event.stopPropagation();
    this.open.update((open) => !open);
    if (this.open()) {
      // Se espera a que el panel exista en el DOM para enfocar su primer elemento.
      setTimeout(() => this.focusables()[0]?.focus());
    }
  }

  /**
   * Flechas, Inicio y Fin recorren los elementos del panel.
   */
  protected onKeydown(event: KeyboardEvent): void {
    const items = this.focusables();
    const index = items.indexOf(document.activeElement as HTMLElement);
    const next: Record<string, number> = {
      ArrowDown: (index + 1) % items.length,
      ArrowUp: (index - 1 + items.length) % items.length,
      Home: 0,
      End: items.length - 1,
    };

    if (event.key in next && items.length > 0) {
      event.preventDefault();
      items[next[event.key]].focus();
    }
  }

  protected onEscape(): void {
    if (this.open()) {
      this.open.set(false);
      this.trigger().nativeElement.focus();
    }
  }

  protected onDocumentClick(event: Event): void {
    if (this.open() && !this.host.nativeElement.contains(event.target as Node)) {
      this.open.set(false);
    }
  }

  private focusables(): HTMLElement[] {
    const panel = this.panel()?.nativeElement;
    return panel ? Array.from(panel.querySelectorAll<HTMLElement>(focusableSelector)) : [];
  }
}

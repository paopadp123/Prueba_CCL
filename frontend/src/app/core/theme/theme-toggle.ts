import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';

import { Icon } from '../../shared/ui/icon';
import { ThemeService } from './theme.service';

/**
 * Botón para cambiar entre modo claro y oscuro.
 * `labeled` muestra el texto (menú lateral); sin él es solo el ícono (barra superior, login).
 */
@Component({
  selector: 'app-theme-toggle',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      type="button"
      [class]="labeled() ? 'theme-toggle theme-toggle--labeled' : 'theme-toggle'"
      role="switch"
      [attr.aria-checked]="theme.isDark()"
      [attr.aria-label]="labeled() ? null : 'Modo oscuro'"
      [attr.title]="theme.isDark() ? 'Cambiar a modo claro' : 'Cambiar a modo oscuro'"
      (click)="theme.toggle()"
    >
      <app-icon [name]="theme.isDark() ? 'sun' : 'moon'" [size]="20" />
      @if (labeled()) {
        <span>{{ theme.isDark() ? 'Modo claro' : 'Modo oscuro' }}</span>
      }
    </button>
  `,
  styles: `
    .theme-toggle {
      display: inline-flex;
      align-items: center;
      gap: 0.75rem;
      min-width: 2.25rem;
      height: 2.25rem;
      padding: 0 0.5rem;
      border: 0;
      border-radius: var(--radius);
      background: none;
      color: var(--muted);
      font: inherit;
      cursor: pointer;
    }
    .theme-toggle:hover {
      background: var(--primary-soft);
      color: var(--primary);
    }
    .theme-toggle:focus-visible {
      outline: 2px solid var(--primary);
      outline-offset: 2px;
    }
    .theme-toggle--labeled {
      width: 100%;
      height: 3rem;
      padding: 0 0.75rem;
      color: var(--text);
      font-weight: 500;
    }
  `,
})
export class ThemeToggle {
  protected readonly theme = inject(ThemeService);

  readonly labeled = input(false);
}

import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { Icon, IconName } from '../../shared/ui/icon';
import { ThemePreference, ThemeService } from './theme.service';

interface Option {
  value: ThemePreference;
  label: string;
  icon: IconName;
}

/**
 * Selector segmentado Claro / Oscuro / Sistema para el menú de usuario y el menú lateral.
 */
@Component({
  selector: 'app-theme-picker',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="picker" role="radiogroup" aria-label="Tema">
      @for (option of options; track option.value) {
        <button
          type="button"
          role="radio"
          class="picker__option"
          [attr.aria-checked]="theme.mode() === option.value"
          (click)="theme.setPreference(option.value)"
        >
          <app-icon [name]="option.icon" [size]="16" />
          {{ option.label }}
        </button>
      }
    </div>
  `,
  styles: `
    .picker {
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      gap: 0.25rem;
      padding: 0.25rem;
      border-radius: var(--radius);
      background: var(--muted-soft);
    }
    .picker__option {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      gap: 0.375rem;
      min-height: 2rem;
      padding: 0 0.5rem;
      border: 0;
      border-radius: 6px;
      background: none;
      color: var(--muted);
      font: inherit;
      font-size: 0.8125rem;
      font-weight: 600;
      cursor: pointer;
    }
    .picker__option:hover {
      color: var(--text);
    }
    .picker__option[aria-checked='true'] {
      background: var(--surface);
      color: var(--primary);
      box-shadow: var(--shadow-sm);
    }
    .picker__option:focus-visible {
      outline: 2px solid var(--primary);
      outline-offset: 1px;
    }
  `,
})
export class ThemePicker {
  protected readonly theme = inject(ThemeService);

  protected readonly options: Option[] = [
    { value: 'light', label: 'Claro', icon: 'sun' },
    { value: 'dark', label: 'Oscuro', icon: 'moon' },
    { value: 'system', label: 'Sistema', icon: 'monitor' },
  ];
}

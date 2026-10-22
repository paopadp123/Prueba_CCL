import { DOCUMENT } from '@angular/common';
import { computed, effect, inject, Injectable, signal } from '@angular/core';

export type Theme = 'light' | 'dark';

/** Debe coincidir con la clave que lee el script de index.html antes de que cargue Angular. */
export const themeStorageKey = 'inventory.theme';

const themeColors: Record<Theme, string> = { light: '#2a6fd1', dark: '#171d26' };

/**
 * Tema claro u oscuro. La primera vez se usa el del sistema operativo; si el usuario
 * elige uno, se recuerda en localStorage. El tema se aplica con `data-theme` en <html>
 * y los colores salen de las variables CSS de styles.scss.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  private readonly theme = signal<Theme>(this.initialTheme());

  readonly current = this.theme.asReadonly();
  readonly isDark = computed(() => this.theme() === 'dark');

  constructor() {
    // Cada vez que cambia la signal se actualiza el DOM.
    effect(() => this.apply(this.theme()));
  }

  toggle(): void {
    this.set(this.isDark() ? 'light' : 'dark');
  }

  set(theme: Theme): void {
    this.theme.set(theme);
    try {
      localStorage.setItem(themeStorageKey, theme);
    } catch {
      // Sin almacenamiento el tema dura solo esta visita.
    }
  }

  private initialTheme(): Theme {
    const fromPage = this.document.documentElement.getAttribute('data-theme');
    if (fromPage === 'light' || fromPage === 'dark') {
      return fromPage;
    }

    try {
      const stored = localStorage.getItem(themeStorageKey);
      if (stored === 'light' || stored === 'dark') {
        return stored;
      }
    } catch {
      // Se ignora y se usa el del sistema.
    }

    return this.document.defaultView?.matchMedia?.('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }

  private apply(theme: Theme): void {
    this.document.documentElement.setAttribute('data-theme', theme);
    this.document.querySelector('meta[name="theme-color"]')?.setAttribute('content', themeColors[theme]);
  }
}

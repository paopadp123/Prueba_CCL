import { DOCUMENT } from '@angular/common';
import { computed, DestroyRef, effect, inject, Injectable, signal } from '@angular/core';

export type Theme = 'light' | 'dark';

/** Preferencia del usuario: un tema fijo o seguir al sistema operativo. */
export type ThemePreference = Theme | 'system';

/** Debe coincidir con la clave que lee el script de index.html antes de que cargue Angular. */
export const themeStorageKey = 'inventory.theme';

const themeColors: Record<Theme, string> = { light: '#2a6fd1', dark: '#171d26' };

/**
 * Tema claro u oscuro. Por defecto sigue al sistema operativo (y cambia si el sistema cambia);
 * si el usuario elige uno fijo, se recuerda en localStorage. El tema se aplica con `data-theme`
 * en <html> y los colores salen de las variables CSS de styles.scss.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  private readonly media = this.document.defaultView?.matchMedia?.('(prefers-color-scheme: dark)') ?? null;

  private readonly preference = signal<ThemePreference>(this.storedPreference());
  private readonly systemDark = signal(this.media?.matches ?? false);

  readonly mode = this.preference.asReadonly();
  readonly current = computed<Theme>(() => {
    const preference = this.preference();
    return preference === 'system' ? (this.systemDark() ? 'dark' : 'light') : preference;
  });
  readonly isDark = computed(() => this.current() === 'dark');

  constructor() {
    const onSystemChange = (event: MediaQueryListEvent): void => this.systemDark.set(event.matches);
    this.media?.addEventListener('change', onSystemChange);
    inject(DestroyRef).onDestroy(() => this.media?.removeEventListener('change', onSystemChange));

    // Cada vez que cambia el tema efectivo se actualiza el DOM.
    effect(() => this.apply(this.current()));
  }

  /** Alterna entre claro y oscuro (fija la preferencia). */
  toggle(): void {
    this.setPreference(this.isDark() ? 'light' : 'dark');
  }

  setPreference(preference: ThemePreference): void {
    this.preference.set(preference);
    try {
      if (preference === 'system') {
        localStorage.removeItem(themeStorageKey);
      } else {
        localStorage.setItem(themeStorageKey, preference);
      }
    } catch {
      // Sin almacenamiento la preferencia dura solo esta visita.
    }
  }

  private storedPreference(): ThemePreference {
    try {
      const stored = localStorage.getItem(themeStorageKey);
      if (stored === 'light' || stored === 'dark') {
        return stored;
      }
    } catch {
      // Se ignora y se sigue al sistema.
    }

    return 'system';
  }

  private apply(theme: Theme): void {
    this.document.documentElement.setAttribute('data-theme', theme);
    this.document.querySelector('meta[name="theme-color"]')?.setAttribute('content', themeColors[theme]);
  }
}

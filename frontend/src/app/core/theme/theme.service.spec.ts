import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { themeStorageKey, ThemeService } from './theme.service';

describe('ThemeService', () => {
  const root = document.documentElement;

  function create(): ThemeService {
    TestBed.configureTestingModule({ providers: [provideZonelessChangeDetection()] });
    const service = TestBed.inject(ThemeService);
    TestBed.tick();
    return service;
  }

  beforeEach(() => {
    root.removeAttribute('data-theme');
    localStorage.removeItem(themeStorageKey);
  });

  afterEach(() => {
    root.removeAttribute('data-theme');
    localStorage.removeItem(themeStorageKey);
  });

  it('respeta el tema que ya aplicó el script de index.html', () => {
    root.setAttribute('data-theme', 'dark');

    expect(create().isDark()).toBeTrue();
  });

  it('usa el tema guardado si la página aún no tiene uno', () => {
    localStorage.setItem(themeStorageKey, 'dark');

    expect(create().current()).toBe('dark');
  });

  it('toggle cambia el tema, lo aplica en <html> y lo recuerda', () => {
    root.setAttribute('data-theme', 'light');
    const service = create();

    service.toggle();
    TestBed.tick();

    expect(service.isDark()).toBeTrue();
    expect(root.getAttribute('data-theme')).toBe('dark');
    expect(localStorage.getItem(themeStorageKey)).toBe('dark');
  });
});

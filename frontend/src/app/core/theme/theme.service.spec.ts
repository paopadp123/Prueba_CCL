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

  beforeEach(() => localStorage.removeItem(themeStorageKey));

  afterEach(() => {
    root.removeAttribute('data-theme');
    localStorage.removeItem(themeStorageKey);
  });

  it('sin preferencia guardada sigue al sistema', () => {
    const service = create();
    const systemDark = matchMedia('(prefers-color-scheme: dark)').matches;

    expect(service.mode()).toBe('system');
    expect(service.isDark()).toBe(systemDark);
  });

  it('usa la preferencia guardada', () => {
    localStorage.setItem(themeStorageKey, 'dark');

    const service = create();

    expect(service.mode()).toBe('dark');
    expect(root.getAttribute('data-theme')).toBe('dark');
  });

  it('elegir un tema lo aplica en <html> y lo recuerda', () => {
    const service = create();

    service.setPreference('dark');
    TestBed.tick();

    expect(root.getAttribute('data-theme')).toBe('dark');
    expect(localStorage.getItem(themeStorageKey)).toBe('dark');
  });

  it('volver a "Sistema" borra la preferencia guardada', () => {
    localStorage.setItem(themeStorageKey, 'light');
    const service = create();

    service.setPreference('system');

    expect(service.mode()).toBe('system');
    expect(localStorage.getItem(themeStorageKey)).toBeNull();
  });

  it('toggle alterna entre claro y oscuro', () => {
    localStorage.setItem(themeStorageKey, 'light');
    const service = create();

    service.toggle();

    expect(service.current()).toBe('dark');
  });
});

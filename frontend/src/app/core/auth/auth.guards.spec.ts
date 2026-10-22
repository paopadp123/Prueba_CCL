import { provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Route, Router, UrlSegment, UrlTree } from '@angular/router';

import { authGuard, guestGuard } from './auth.guards';
import { AuthService } from './auth.service';

describe('guards de autenticación', () => {
  const isAuthenticated = signal(false);
  const route: Route = {};

  beforeEach(() => {
    isAuthenticated.set(false);
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), { provide: AuthService, useValue: { isAuthenticated } }],
    });
  });

  function run(guard: typeof authGuard, path: string[]): ReturnType<typeof authGuard> {
    const segments = path.map((part) => new UrlSegment(part, {}));
    return TestBed.runInInjectionContext(() => guard(route, segments));
  }

  it('authGuard deja pasar con sesión', () => {
    isAuthenticated.set(true);

    expect(run(authGuard, ['products'])).toBeTrue();
  });

  it('authGuard sin sesión redirige al login guardando la URL pedida', () => {
    const result = run(authGuard, ['products']) as UrlTree;

    expect(TestBed.inject(Router).serializeUrl(result)).toBe('/login?returnUrl=%2Fproducts');
  });

  it('guestGuard con sesión manda al inicio', () => {
    isAuthenticated.set(true);

    const result = run(guestGuard, ['login']) as UrlTree;

    expect(TestBed.inject(Router).serializeUrl(result)).toBe('/');
  });
});

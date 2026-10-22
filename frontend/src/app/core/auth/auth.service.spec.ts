import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';

import { apiUrl } from '../http/api-url';
import { NotificationService } from '../notifications/notification.service';
import { LoginResponse, Session } from './auth.models';
import { AuthService } from './auth.service';

const storageKey = 'inventory.session';

function loginResponse(expiresInMs: number): LoginResponse {
  return {
    accessToken: 'token-123',
    tokenType: 'Bearer',
    expiresAt: new Date(Date.now() + expiresInMs).toISOString(),
    user: { id: 1, username: 'admin', fullName: 'Administrador', role: 'Admin' },
  };
}

describe('AuthService', () => {
  let http: HttpTestingController;

  function createService(): AuthService {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
    return TestBed.inject(AuthService);
  }

  beforeEach(() => sessionStorage.clear());
  afterEach(() => http.verify());

  it('login guarda el token y expone el usuario', () => {
    const service = createService();

    service.login({ username: 'admin', password: 'x' }).subscribe();
    http.expectOne(apiUrl('/api/auth/login')).flush(loginResponse(60_000));

    expect(service.isAuthenticated()).toBeTrue();
    expect(service.token()).toBe('token-123');
    expect(service.isAdmin()).toBeTrue();
    expect(sessionStorage.getItem(storageKey)).toContain('token-123');
  });

  it('logout borra la sesión y navega al login', () => {
    const service = createService();
    const navigate = spyOn(TestBed.inject(Router), 'navigate').and.resolveTo(true);
    service.login({ username: 'admin', password: 'x' }).subscribe();
    http.expectOne(apiUrl('/api/auth/login')).flush(loginResponse(60_000));

    service.logout();

    expect(service.isAuthenticated()).toBeFalse();
    expect(sessionStorage.getItem(storageKey)).toBeNull();
    expect(navigate).toHaveBeenCalledWith(['/login']);
  });

  it('recupera una sesión vigente guardada al recargar la página', () => {
    const session: Session = { ...loginResponse(60_000) };
    sessionStorage.setItem(storageKey, JSON.stringify(session));

    const service = createService();

    expect(service.user()?.username).toBe('admin');
  });

  it('descarta una sesión guardada que ya venció', () => {
    const session: Session = { ...loginResponse(-1_000) };
    sessionStorage.setItem(storageKey, JSON.stringify(session));

    const service = createService();

    expect(service.isAuthenticated()).toBeFalse();
  });

  it('expire cierra la sesión, avisa y recuerda la URL actual', () => {
    const service = createService();
    const notify = spyOn(TestBed.inject(NotificationService), 'info');
    const navigate = spyOn(TestBed.inject(Router), 'navigate').and.resolveTo(true);
    service.login({ username: 'admin', password: 'x' }).subscribe();
    http.expectOne(apiUrl('/api/auth/login')).flush(loginResponse(60_000));

    service.expire();

    expect(service.isAuthenticated()).toBeFalse();
    expect(notify).toHaveBeenCalled();
    expect(navigate).toHaveBeenCalledWith(['/login'], jasmine.objectContaining({ queryParams: jasmine.any(Object) }));
  });
});

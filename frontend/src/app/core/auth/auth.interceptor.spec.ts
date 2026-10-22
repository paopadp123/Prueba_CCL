import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { apiUrl } from '../http/api-url';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';

describe('authInterceptor', () => {
  const token = signal<string | null>(null);
  let http: HttpClient;
  let backend: HttpTestingController;

  beforeEach(() => {
    token.set(null);
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: { token } },
      ],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => backend.verify());

  it('agrega el Bearer a las peticiones de la API cuando hay sesión', () => {
    token.set('abc');

    http.get(apiUrl('/api/stock')).subscribe();

    expect(backend.expectOne(apiUrl('/api/stock')).request.headers.get('Authorization')).toBe('Bearer abc');
  });

  it('no agrega encabezado sin sesión', () => {
    http.get(apiUrl('/api/stock')).subscribe();

    expect(backend.expectOne(apiUrl('/api/stock')).request.headers.has('Authorization')).toBeFalse();
  });

  it('nunca envía el token a otros dominios', () => {
    token.set('abc');

    http.get('https://otro-sitio.com/api/datos').subscribe();

    expect(backend.expectOne('https://otro-sitio.com/api/datos').request.headers.has('Authorization')).toBeFalse();
  });
});

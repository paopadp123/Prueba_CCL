import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { AuthService } from '../auth/auth.service';
import { NotificationService } from '../notifications/notification.service';
import { ApiError } from './api-error';
import { apiUrl } from './api-url';
import { errorInterceptor } from './error.interceptor';

describe('errorInterceptor', () => {
  let http: HttpClient;
  let backend: HttpTestingController;
  let auth: jasmine.SpyObj<AuthService>;
  let notifications: jasmine.SpyObj<NotificationService>;

  beforeEach(() => {
    auth = jasmine.createSpyObj<AuthService>('AuthService', ['expire']);
    notifications = jasmine.createSpyObj<NotificationService>('NotificationService', ['error']);
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: auth },
        { provide: NotificationService, useValue: notifications },
      ],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => backend.verify());

  function request(url: string, status: number, body: object | null = null): unknown {
    let received: unknown;
    http.get(url).subscribe({ error: (error: unknown) => (received = error) });
    backend.expectOne(url).flush(body, { status, statusText: 'Error' });
    return received;
  }

  it('un 401 de la API cierra la sesión', () => {
    request(apiUrl('/api/stock'), 401);

    expect(auth.expire).toHaveBeenCalled();
  });

  it('un 401 del login no cierra sesión ni muestra aviso: lo maneja la pantalla', () => {
    request(apiUrl('/api/auth/login'), 401, { title: 'Credenciales inválidas', detail: 'Usuario o contraseña incorrectos.' });

    expect(auth.expire).not.toHaveBeenCalled();
    expect(notifications.error).not.toHaveBeenCalled();
  });

  it('un 500 muestra aviso global con la referencia', () => {
    const error = request(apiUrl('/api/stock'), 500, {
      title: 'Error interno',
      detail: 'Ocurrió un error inesperado.',
      correlationId: 'ref-9',
    });

    expect(error).toBeInstanceOf(ApiError);
    expect(notifications.error).toHaveBeenCalledWith(jasmine.stringContaining('ref-9'));
  });

  it('un 409 no muestra aviso global y llega a la pantalla como ApiError', () => {
    const error = request(apiUrl('/api/movements/exits'), 409, {
      title: 'Stock insuficiente',
      detail: 'No alcanza el stock.',
      code: 'insufficient_stock',
    });

    expect(notifications.error).not.toHaveBeenCalled();
    expect((error as ApiError).code).toBe('insufficient_stock');
  });
});

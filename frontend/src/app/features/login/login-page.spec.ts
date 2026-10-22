import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { ApiError } from '../../core/http/api-error';
import { LoginPage } from './login-page';

describe('LoginPage', () => {
  let fixture: ComponentFixture<LoginPage>;
  let auth: jasmine.SpyObj<AuthService>;
  let navigate: jasmine.Spy;

  beforeEach(async () => {
    auth = jasmine.createSpyObj<AuthService>('AuthService', ['login']);
    await TestBed.configureTestingModule({
      imports: [LoginPage],
      providers: [provideZonelessChangeDetection(), provideRouter([]), { provide: AuthService, useValue: auth }],
    }).compileComponents();

    navigate = spyOn(TestBed.inject(Router), 'navigateByUrl').and.resolveTo(true);
    fixture = TestBed.createComponent(LoginPage);
    await fixture.whenStable();
  });

  function element(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  async function fillAndSubmit(username: string, password: string): Promise<void> {
    const inputs = element().querySelectorAll('input');
    inputs[0].value = username;
    inputs[0].dispatchEvent(new Event('input'));
    inputs[1].value = password;
    inputs[1].dispatchEvent(new Event('input'));
    element().querySelector('form')!.dispatchEvent(new Event('submit'));
    await fixture.whenStable();
  }

  it('con campos vacíos no llama a la API y muestra los mensajes', async () => {
    await fillAndSubmit('', '');

    expect(auth.login).not.toHaveBeenCalled();
    expect(element().textContent).toContain('Ingresa tu usuario.');
  });

  it('con login correcto navega al inicio', async () => {
    auth.login.and.returnValue(of({ id: 1, username: 'admin', fullName: 'Admin', role: 'Admin' }));

    await fillAndSubmit('admin', 'Admin123*');

    expect(auth.login).toHaveBeenCalledWith({ username: 'admin', password: 'Admin123*' });
    expect(navigate).toHaveBeenCalledWith('/');
  });

  it('vuelve a la URL interna pedida pero ignora URLs externas', async () => {
    auth.login.and.returnValue(of({ id: 1, username: 'admin', fullName: 'Admin', role: 'Admin' }));
    fixture.componentRef.setInput('returnUrl', 'https://malicioso.com');

    await fillAndSubmit('admin', 'Admin123*');

    expect(navigate).toHaveBeenCalledWith('/');
  });

  it('muestra el mensaje de la API si las credenciales son inválidas', async () => {
    auth.login.and.returnValue(throwError(() => new ApiError(401, 'Usuario o contraseña incorrectos.')));

    await fillAndSubmit('admin', 'mala');

    expect(element().querySelector('[role="alert"]')?.textContent).toContain('Usuario o contraseña incorrectos.');
  });
});

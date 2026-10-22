import { provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { AuthService } from '../auth/auth.service';
import { Shell } from './shell';

describe('Shell', () => {
  let fixture: ComponentFixture<Shell>;
  let auth: {
    user: ReturnType<typeof signal>;
    isAdmin: ReturnType<typeof signal>;
    expiresAt: ReturnType<typeof signal>;
    logout: jasmine.Spy;
  };

  beforeEach(async () => {
    auth = {
      user: signal({ id: 1, username: 'admin', fullName: 'Ana María Pérez', role: 'Admin' }),
      isAdmin: signal(true),
      expiresAt: signal('2026-10-21T22:45:00-05:00'),
      logout: jasmine.createSpy('logout'),
    };

    await TestBed.configureTestingModule({
      imports: [Shell],
      providers: [provideZonelessChangeDetection(), provideRouter([]), { provide: AuthService, useValue: auth }],
    }).compileComponents();

    fixture = TestBed.createComponent(Shell);
    await fixture.whenStable();
  });

  function element(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  it('muestra las iniciales del usuario', () => {
    expect(element().querySelector('.avatar')?.textContent?.trim()).toBe('AM');
  });

  it('el botón hamburguesa abre y cierra el menú lateral', async () => {
    const toggle = element().querySelector<HTMLButtonElement>('.menu-toggle')!;

    toggle.click();
    await fixture.whenStable();
    expect(element().querySelector('#mobile-menu')).not.toBeNull();
    expect(toggle.getAttribute('aria-expanded')).toBe('true');

    toggle.click();
    await fixture.whenStable();
    expect(element().querySelector('#mobile-menu')).toBeNull();
  });

  it('Escape cierra el menú lateral', async () => {
    element().querySelector<HTMLButtonElement>('.menu-toggle')!.click();
    await fixture.whenStable();

    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    await fixture.whenStable();

    expect(element().querySelector('#mobile-menu')).toBeNull();
  });

  it('cerrar sesión desde el menú lateral llama a logout', async () => {
    element().querySelector<HTMLButtonElement>('.menu-toggle')!.click();
    await fixture.whenStable();

    element().querySelector<HTMLButtonElement>('.drawer__logout')!.click();

    expect(auth.logout).toHaveBeenCalled();
  });

  it('el menú lateral incluye el selector de tema', async () => {
    element().querySelector<HTMLButtonElement>('.menu-toggle')!.click();
    await fixture.whenStable();

    expect(element().querySelectorAll('#mobile-menu [role="radio"]').length).toBe(3);
  });
});

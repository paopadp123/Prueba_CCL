import { provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { AuthService } from '../auth/auth.service';
import { UserMenu } from './user-menu';

describe('UserMenu', () => {
  let fixture: ComponentFixture<UserMenu>;
  const isAdmin = signal(true);
  const logout = jasmine.createSpy('logout');

  beforeEach(async () => {
    isAdmin.set(true);
    await TestBed.configureTestingModule({
      imports: [UserMenu],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        {
          provide: AuthService,
          useValue: {
            user: signal({ id: 1, username: 'admin', fullName: 'Ana María Pérez', role: 'Admin' }),
            isAdmin,
            expiresAt: signal('2026-10-21T22:45:00-05:00'),
            logout,
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(UserMenu);
    await fixture.whenStable();
  });

  function element(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  async function open(): Promise<HTMLElement> {
    element().querySelector<HTMLButtonElement>('.trigger')!.click();
    await fixture.whenStable();
    await new Promise((resolve) => setTimeout(resolve));
    return element().querySelector<HTMLElement>('#user-menu-panel')!;
  }

  it('muestra iniciales, vencimiento de la sesión, atajos de Admin y selector de tema', async () => {
    expect(element().querySelector('.avatar')?.textContent?.trim()).toBe('AM');

    const panel = await open();

    expect(panel.textContent).toContain('@admin');
    expect(panel.textContent).toContain('Tu sesión vence a las');
    expect(panel.textContent).toContain('Nuevo producto');
    expect(panel.querySelectorAll('[role="radio"]').length).toBe(3);
  });

  it('un operador no ve el atajo de nuevo producto', async () => {
    isAdmin.set(false);

    const panel = await open();

    expect(panel.textContent).not.toContain('Nuevo producto');
  });

  it('enfoca el primer elemento, las flechas lo recorren y Escape cierra devolviendo el foco', async () => {
    const trigger = element().querySelector<HTMLButtonElement>('.trigger')!;
    const panel = await open();
    const items = panel.querySelectorAll<HTMLElement>('a[href], button');

    expect(document.activeElement).toBe(items[0]);

    panel.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true }));
    expect(document.activeElement).toBe(items[1]);

    panel.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowUp', bubbles: true }));
    panel.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowUp', bubbles: true }));
    expect(document.activeElement).toBe(items[items.length - 1]);

    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    await fixture.whenStable();

    expect(element().querySelector('#user-menu-panel')).toBeNull();
    expect(document.activeElement).toBe(trigger);
  });

  it('un clic fuera del menú lo cierra', async () => {
    await open();

    document.body.click();
    await fixture.whenStable();

    expect(element().querySelector('#user-menu-panel')).toBeNull();
  });

  it('cerrar sesión llama a logout', async () => {
    const panel = await open();

    panel.querySelector<HTMLButtonElement>('.item--danger')!.click();

    expect(logout).toHaveBeenCalled();
  });
});

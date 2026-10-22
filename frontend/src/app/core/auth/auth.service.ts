import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { map, Observable, tap } from 'rxjs';

import { apiUrl } from '../http/api-url';
import { NotificationService } from '../notifications/notification.service';
import { LoginRequest, LoginResponse, Role, Session, User } from './auth.models';

const storageKey = 'inventory.session';

/**
 * Estado de la sesión con signals. El token se guarda en sessionStorage: sobrevive a
 * recargar la página pero se borra al cerrar la pestaña, y además vence a los 60 minutos.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly notifications = inject(NotificationService);

  private readonly session = signal<Session | null>(readStoredSession());
  private expiryTimer: ReturnType<typeof setTimeout> | undefined;

  readonly user = computed<User | null>(() => this.session()?.user ?? null);
  readonly token = computed<string | null>(() => this.session()?.accessToken ?? null);
  readonly expiresAt = computed<string | null>(() => this.session()?.expiresAt ?? null);
  readonly isAuthenticated = computed<boolean>(() => this.session() !== null);
  readonly isAdmin = computed<boolean>(() => this.user()?.role === 'Admin');

  constructor() {
    const current = this.session();
    if (current) {
      this.scheduleExpiry(current);
    }
  }

  /**
   * Inicia sesión y guarda el token. Los errores llegan como `ApiError`.
   */
  login(credentials: LoginRequest): Observable<User> {
    return this.http.post<LoginResponse>(apiUrl('/api/auth/login'), credentials).pipe(
      map(
        (response): Session => ({
          accessToken: response.accessToken,
          expiresAt: response.expiresAt,
          user: response.user,
        }),
      ),
      tap((session) => this.start(session)),
      map((session) => session.user),
    );
  }

  /**
   * Cierra la sesión por decisión del usuario.
   */
  logout(): void {
    this.clear();
    void this.router.navigate(['/login']);
  }

  /**
   * Cierra la sesión porque el token venció o la API lo rechazó (401),
   * y vuelve al login recordando dónde estaba el usuario.
   */
  expire(): void {
    if (!this.isAuthenticated()) {
      return;
    }

    this.clear();
    this.notifications.info('Tu sesión expiró. Inicia sesión nuevamente.');
    void this.router.navigate(['/login'], { queryParams: { returnUrl: this.router.url } });
  }

  hasRole(role: Role): boolean {
    return this.user()?.role === role;
  }

  private start(session: Session): void {
    this.session.set(session);
    writeStoredSession(session);
    this.scheduleExpiry(session);
  }

  private clear(): void {
    clearTimeout(this.expiryTimer);
    this.session.set(null);
    writeStoredSession(null);
  }

  private scheduleExpiry(session: Session): void {
    clearTimeout(this.expiryTimer);
    const remainingMs = Date.parse(session.expiresAt) - Date.now();
    this.expiryTimer = setTimeout(() => this.expire(), Math.max(remainingMs, 0));
  }
}

/**
 * Lee la sesión guardada y la descarta si ya venció o si el almacenamiento no está disponible.
 */
function readStoredSession(): Session | null {
  try {
    const raw = sessionStorage.getItem(storageKey);
    if (!raw) {
      return null;
    }

    const session = JSON.parse(raw) as Session;
    return Date.parse(session.expiresAt) > Date.now() ? session : null;
  } catch {
    return null;
  }
}

function writeStoredSession(session: Session | null): void {
  try {
    if (session) {
      sessionStorage.setItem(storageKey, JSON.stringify(session));
    } else {
      sessionStorage.removeItem(storageKey);
    }
  } catch {
    // Sin almacenamiento (modo privado estricto): la sesión vive solo en memoria.
  }
}

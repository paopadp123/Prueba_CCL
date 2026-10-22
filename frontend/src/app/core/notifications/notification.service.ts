import { Injectable, signal } from '@angular/core';

export type NotificationKind = 'error' | 'success' | 'info';

export interface Notification {
  id: number;
  kind: NotificationKind;
  message: string;
}

/**
 * Avisos globales (toasts). Los errores que no pertenecen a un formulario concreto
 * (sin conexión, permisos, errores del servidor, sesión expirada) se muestran aquí.
 */
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly items = signal<Notification[]>([]);
  private nextId = 1;

  readonly notifications = this.items.asReadonly();

  error(message: string): void {
    this.show('error', message, 8000);
  }

  success(message: string): void {
    this.show('success', message);
  }

  info(message: string): void {
    this.show('info', message);
  }

  dismiss(id: number): void {
    this.items.update((items) => items.filter((item) => item.id !== id));
  }

  private show(kind: NotificationKind, message: string, durationMs = 5000): void {
    const id = this.nextId++;
    this.items.update((items) => [...items, { id, kind, message }]);
    setTimeout(() => this.dismiss(id), durationMs);
  }
}

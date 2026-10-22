import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { NotificationService } from './notification.service';

/**
 * Pila de avisos globales en una esquina de la pantalla.
 */
@Component({
  selector: 'app-toasts',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="toasts" aria-live="polite">
      @for (item of notifications.notifications(); track item.id) {
        <div class="toast" [class]="'toast toast--' + item.kind" [attr.role]="item.kind === 'error' ? 'alert' : 'status'">
          <span>{{ item.message }}</span>
          <button type="button" class="toast__close" aria-label="Cerrar aviso" (click)="notifications.dismiss(item.id)">
            ×
          </button>
        </div>
      }
    </div>
  `,
  styles: `
    .toasts {
      position: fixed;
      right: 1rem;
      bottom: 1rem;
      left: 1rem;
      display: flex;
      flex-direction: column;
      align-items: flex-end;
      gap: 0.5rem;
      pointer-events: none;
      z-index: 100;
    }
    .toast {
      display: flex;
      align-items: flex-start;
      gap: 0.75rem;
      max-width: 26rem;
      padding: 0.75rem 1rem;
      border-radius: var(--radius);
      background: var(--surface);
      border-left: 4px solid var(--info);
      box-shadow: var(--shadow);
      pointer-events: auto;
    }
    .toast--error { border-left-color: var(--danger); }
    .toast--success { border-left-color: var(--success); }
    .toast__close {
      border: 0;
      background: none;
      font-size: 1.25rem;
      line-height: 1;
      color: var(--muted);
      cursor: pointer;
    }
  `,
})
export class Toasts {
  protected readonly notifications = inject(NotificationService);
}

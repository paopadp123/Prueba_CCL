import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';
import { ApiError } from '../../core/http/api-error';
import { ThemeToggle } from '../../core/theme/theme-toggle';
import { Icon } from '../../shared/ui/icon';

/**
 * Pantalla de inicio de sesión.
 */
@Component({
  selector: 'app-login-page',
  imports: [ReactiveFormsModule, Icon, ThemeToggle],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './login-page.html',
  styleUrl: './login-page.scss',
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  /** URL a la que volver después del login; llega como query param gracias a `withComponentInputBinding`. */
  readonly returnUrl = input<string>();

  protected readonly form = inject(NonNullableFormBuilder).group({
    username: ['', [Validators.required, Validators.maxLength(50)]],
    password: ['', [Validators.required, Validators.maxLength(100)]],
  });

  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly showPassword = signal(false);

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    this.auth.login(this.form.getRawValue()).subscribe({
      next: () => void this.router.navigateByUrl(safeReturnUrl(this.returnUrl())),
      error: (error: unknown) => {
        this.loading.set(false);
        this.error.set(error instanceof ApiError ? error.message : 'No se pudo iniciar sesión.');
      },
    });
  }
}

/**
 * Solo se aceptan rutas internas; evita que un enlace manipulado (`?returnUrl=https://...`)
 * lleve al usuario a otro sitio después de iniciar sesión (open redirect).
 */
function safeReturnUrl(url: string | undefined): string {
  return url && url.startsWith('/') && !url.startsWith('//') ? url : '/';
}

import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';

import { AuthService } from './auth.service';

/**
 * Deja entrar solo con sesión. Si no hay, redirige al login guardando la URL pedida.
 * Se usa con `canMatch` para que el código de la ruta protegida ni siquiera se descargue.
 */
export const authGuard: CanMatchFn = (_route, segments) => {
  if (inject(AuthService).isAuthenticated()) {
    return true;
  }

  const requested = '/' + segments.map((segment) => segment.path).join('/');
  return inject(Router).createUrlTree(['/login'], {
    queryParams: requested === '/' ? {} : { returnUrl: requested },
  });
};

/**
 * Evita volver al login cuando ya hay sesión.
 */
export const guestGuard: CanMatchFn = () =>
  inject(AuthService).isAuthenticated() ? inject(Router).createUrlTree(['/']) : true;

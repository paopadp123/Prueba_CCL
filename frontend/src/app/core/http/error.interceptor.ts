import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';

import { AuthService } from '../auth/auth.service';
import { NotificationService } from '../notifications/notification.service';
import { apiUrl } from './api-url';
import { ApiError, isGlobalError } from './api-error';

/**
 * Convierte cualquier error HTTP en `ApiError` y decide quién lo muestra:
 * - 401 fuera del login: la sesión ya no sirve, se cierra y se vuelve al login.
 * - Sin conexión, 403, 429 y 5xx: aviso global, porque no dependen del formulario.
 * - 400, 404 y 409: los muestra la pantalla que hizo la petición, junto al formulario.
 */
export const errorInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const notifications = inject(NotificationService);

  return next(request).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse)) {
        return throwError(() => error);
      }

      const apiError = ApiError.from(error);
      const isLogin = request.url === apiUrl('/api/auth/login');

      if (apiError.status === 401 && !isLogin) {
        auth.expire();
      } else if (isGlobalError(apiError.status) && !isLogin) {
        notifications.error(withReference(apiError));
      }

      return throwError(() => apiError);
    }),
  );
};

function withReference(error: ApiError): string {
  return error.status >= 500 && error.correlationId
    ? `${error.message} (referencia: ${error.correlationId})`
    : error.message;
}

import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';

import { isApiRequest } from '../http/api-url';
import { AuthService } from './auth.service';

/**
 * Agrega `Authorization: Bearer <token>` a las peticiones dirigidas a la API.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const token = inject(AuthService).token();

  if (!token || !isApiRequest(request.url)) {
    return next(request);
  }

  return next(request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
};

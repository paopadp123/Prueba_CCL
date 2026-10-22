import { HttpErrorResponse } from '@angular/common/http';

/**
 * Cuerpo de error que devuelve la API (ProblemDetails, RFC 9457) con las extensiones propias.
 */
export interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  code?: string;
  correlationId?: string;
  errors?: Record<string, string[]>;
  available?: number;
  requested?: number;
}

const defaultMessages: Record<number, string> = {
  0: 'No se pudo conectar con el servidor. Verifica tu conexión o que la API esté en ejecución.',
  400: 'Revisa los datos ingresados.',
  401: 'Tu sesión no es válida. Inicia sesión nuevamente.',
  403: 'No tienes permisos para realizar esta acción.',
  404: 'El recurso solicitado no existe.',
  409: 'La operación entra en conflicto con el estado actual.',
  429: 'Demasiadas solicitudes. Espera un momento e intenta de nuevo.',
};

const unexpectedMessage = 'Ocurrió un error inesperado. Intenta de nuevo más tarde.';

/**
 * Error de la API ya interpretado, con un mensaje listo para mostrar al usuario.
 */
export class ApiError extends Error {
  constructor(
    readonly status: number,
    message: string,
    readonly code: string | null = null,
    readonly correlationId: string | null = null,
    readonly fieldErrors: Record<string, string[]> = {},
    readonly problem: ProblemDetails | null = null,
  ) {
    super(message);
    this.name = 'ApiError';
  }

  /**
   * Convierte la respuesta HTTP fallida en un error con mensaje en español.
   * Los errores de validación de ASP.NET traen `errors` y un título en inglés,
   * por eso para ellos se usa el mensaje genérico de "revisa los datos".
   */
  static from(response: HttpErrorResponse): ApiError {
    const problem = isProblemDetails(response.error) ? response.error : null;
    const fieldErrors = problem?.errors ?? {};
    const hasFieldErrors = Object.keys(fieldErrors).length > 0;
    const message =
      (!hasFieldErrors && problem?.detail) ||
      defaultMessages[response.status] ||
      unexpectedMessage;

    return new ApiError(
      response.status,
      message,
      problem?.code ?? null,
      problem?.correlationId ?? response.headers?.get('X-Correlation-ID') ?? null,
      fieldErrors,
      problem,
    );
  }
}

function isProblemDetails(body: unknown): body is ProblemDetails {
  return typeof body === 'object' && body !== null && ('title' in body || 'detail' in body);
}

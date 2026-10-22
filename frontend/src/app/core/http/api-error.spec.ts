import { HttpErrorResponse, HttpHeaders } from '@angular/common/http';

import { ApiError } from './api-error';

describe('ApiError.from', () => {
  it('usa el detail del ProblemDetails y conserva code y correlationId', () => {
    const error = ApiError.from(
      new HttpErrorResponse({
        status: 409,
        error: {
          title: 'Stock insuficiente',
          detail: 'Stock insuficiente: disponible 3, solicitado 5.',
          code: 'insufficient_stock',
          correlationId: 'abc-123',
          available: 3,
          requested: 5,
        },
      }),
    );

    expect(error.status).toBe(409);
    expect(error.message).toBe('Stock insuficiente: disponible 3, solicitado 5.');
    expect(error.code).toBe('insufficient_stock');
    expect(error.correlationId).toBe('abc-123');
    expect(error.problem?.available).toBe(3);
  });

  it('en errores de validación usa un mensaje en español y expone los campos', () => {
    const error = ApiError.from(
      new HttpErrorResponse({
        status: 400,
        error: { title: 'One or more validation errors occurred.', errors: { Name: ['Required'] } },
      }),
    );

    expect(error.message).toBe('Revisa los datos ingresados.');
    expect(error.fieldErrors).toEqual({ Name: ['Required'] });
  });

  it('sin conexión (status 0) explica que no se pudo contactar al servidor', () => {
    const error = ApiError.from(new HttpErrorResponse({ status: 0, error: new ProgressEvent('error') }));

    expect(error.message).toContain('No se pudo conectar');
  });

  it('toma el correlationId del encabezado si el cuerpo no lo trae', () => {
    const error = ApiError.from(
      new HttpErrorResponse({ status: 502, headers: new HttpHeaders({ 'X-Correlation-ID': 'hdr-1' }) }),
    );

    expect(error.correlationId).toBe('hdr-1');
    expect(error.message).toContain('error inesperado');
  });
});

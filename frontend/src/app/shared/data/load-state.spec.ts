import { of, throwError } from 'rxjs';

import { ApiError } from '../../core/http/api-error';
import { LoadState, toLoadState } from './load-state';

describe('toLoadState', () => {
  it('emite loading y luego loaded con los datos', () => {
    const states: LoadState<number>[] = [];

    of(42).pipe(toLoadState()).subscribe((state) => states.push(state));

    expect(states).toEqual([{ status: 'loading' }, { status: 'loaded', data: 42 }]);
  });

  it('convierte un ApiError en estado de error con su mensaje', () => {
    const states: LoadState<number>[] = [];

    throwError(() => new ApiError(404, 'El recurso solicitado no existe.'))
      .pipe(toLoadState<number>())
      .subscribe((state) => states.push(state));

    expect(states[1]).toEqual({ status: 'error', message: 'El recurso solicitado no existe.' });
  });
});

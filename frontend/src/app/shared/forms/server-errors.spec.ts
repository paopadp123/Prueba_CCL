import { FormControl, FormGroup } from '@angular/forms';

import { ApiError } from '../../core/http/api-error';
import { applyServerErrors } from './server-errors';

describe('applyServerErrors', () => {
  it('asigna el mensaje de la API al control con el mismo nombre sin importar mayúsculas', () => {
    const form = new FormGroup({ name: new FormControl(''), unitPrice: new FormControl(0) });
    const error = new ApiError(400, 'Revisa los datos ingresados.', null, null, { Name: ['El nombre es obligatorio.'] });

    const applied = applyServerErrors(form, error);

    expect(applied).toBeTrue();
    expect(form.controls.name.errors).toEqual({ server: 'El nombre es obligatorio.' });
    expect(form.controls.name.touched).toBeTrue();
  });

  it('ignora campos que no existen en el formulario', () => {
    const form = new FormGroup({ name: new FormControl('') });
    const error = new ApiError(400, 'Revisa los datos ingresados.', null, null, { Otro: ['x'] });

    expect(applyServerErrors(form, error)).toBeFalse();
  });
});

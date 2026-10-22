import { AbstractControl, FormGroup } from '@angular/forms';

import { ApiError } from '../../core/http/api-error';

/**
 * Lleva los errores de validación de la API (`errors: { Name: [...] }`) a los controles
 * del formulario con el mismo nombre, como error `server`. Devuelve si asignó alguno.
 */
export function applyServerErrors(form: FormGroup, error: ApiError): boolean {
  let applied = false;

  for (const [field, messages] of Object.entries(error.fieldErrors)) {
    const name = Object.keys(form.controls).find((key) => key.toLowerCase() === field.toLowerCase());
    const control = name ? form.controls[name] : null;
    if (control && messages.length > 0) {
      setServerError(control, messages[0]);
      applied = true;
    }
  }

  return applied;
}

/**
 * Marca un control con el mensaje de la API y lo deja visible aunque el usuario no lo haya tocado.
 */
export function setServerError(control: AbstractControl, message: string): void {
  control.setErrors({ ...control.errors, server: message });
  control.markAsTouched();
}

import { ChangeDetectionStrategy, Component, computed, inject, input, OnInit, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { map, startWith } from 'rxjs';

import { ApiError } from '../../core/http/api-error';
import { NotificationService } from '../../core/notifications/notification.service';
import { LoadState, toLoadState } from '../../shared/data/load-state';
import { PagedResponse } from '../../shared/data/paged';
import { applyServerErrors, setServerError } from '../../shared/forms/server-errors';
import { Product } from '../products/product.models';
import { ProductsService } from '../products/products.service';
import { MovementType, movementTypeLabels, reasonMaxLength } from './movement.models';
import { MovementsService } from './movements.service';

/**
 * Registro de entradas y salidas. El stock que se muestra es orientativo: la validación
 * definitiva la hace la API bajo bloqueo, y si no alcanza responde 409 con lo disponible.
 */
@Component({
  selector: 'app-movement-form-page',
  imports: [ReactiveFormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './movement-form-page.html',
})
export class MovementFormPage implements OnInit {
  private readonly movements = inject(MovementsService);
  private readonly products = inject(ProductsService);
  private readonly notifications = inject(NotificationService);

  /** Query params opcionales para llegar con el tipo y el producto ya elegidos. */
  readonly type = input<string>();
  readonly productId = input<string>();

  protected readonly labels = movementTypeLabels;
  protected readonly reasonMaxLength = reasonMaxLength;
  protected readonly saving = signal(false);
  protected readonly formError = signal<string | null>(null);

  /** Stock devuelto por la API tras registrar, para no tener que recargar la lista. */
  private readonly stockAfterSave = signal<ReadonlyMap<number, number>>(new Map());

  protected readonly form = inject(NonNullableFormBuilder).group({
    type: ['Entry' as MovementType, Validators.required],
    productId: [null as number | null, Validators.required],
    quantity: [null as number | null, [Validators.required, Validators.min(1), Validators.pattern(/^[0-9]+$/)]],
    reason: ['', Validators.maxLength(reasonMaxLength)],
  });

  /** Productos activos para el selector (los inactivos no admiten movimientos). */
  protected readonly productOptions = toSignal(
    this.products
      .list({ search: '', includeInactive: false, page: 1, pageSize: 200 })
      .pipe(toLoadState()),
    { initialValue: { status: 'loading' } as LoadState<PagedResponse<Product>> },
  );

  /** Valor del formulario como signal, para que los `computed` reaccionen al escribir. */
  protected readonly formValue = toSignal(
    this.form.valueChanges.pipe(
      map(() => this.form.getRawValue()),
      startWith(this.form.getRawValue()),
    ),
    { requireSync: true },
  );

  protected readonly selectedProduct = computed<Product | null>(() => {
    const options = this.productOptions();
    const id = this.formValue().productId;
    if (options.status !== 'loaded' || id == null) {
      return null;
    }

    return options.data.items.find((product) => product.id === Number(id)) ?? null;
  });

  protected readonly availableStock = computed<number | null>(() => {
    const product = this.selectedProduct();
    return product ? (this.stockAfterSave().get(product.id) ?? product.currentStock) : null;
  });

  /** Aviso previo si la salida supera el stock conocido; la API decide igual. */
  protected readonly exceedsStock = computed(() => {
    const value = this.formValue();
    const available = this.availableStock();
    return value.type === 'Exit' && available !== null && Number(value.quantity ?? 0) > available;
  });

  ngOnInit(): void {
    const type = this.type();
    const productId = Number(this.productId());
    this.form.patchValue({
      type: type === 'Exit' ? 'Exit' : 'Entry',
      productId: Number.isInteger(productId) && productId > 0 ? productId : null,
    });
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const type = value.type;
    const productName = this.selectedProduct()?.name ?? 'Producto';

    this.saving.set(true);
    this.formError.set(null);

    this.movements
      .register(type, {
        productId: Number(value.productId),
        quantity: Number(value.quantity),
        reason: value.reason.trim() || null,
      })
      .subscribe({
        next: (result) => {
          this.saving.set(false);
          this.stockAfterSave.update((map) => new Map(map).set(result.productId, result.stockAfter));
          this.notifications.success(
            `${this.labels[type]} registrada: ${productName}. Stock actual: ${result.stockAfter}.`,
          );
          this.form.controls.quantity.reset(null);
          this.form.controls.reason.reset('');
        },
        error: (error: unknown) => {
          this.saving.set(false);
          this.showError(error);
        },
      });
  }

  private showError(error: unknown): void {
    if (!(error instanceof ApiError)) {
      this.formError.set('No se pudo registrar el movimiento.');
      return;
    }

    switch (error.code) {
      case 'insufficient_stock':
      case 'invalid_quantity':
        setServerError(this.form.controls.quantity, error.message);
        return;
      case 'inactive_product':
      case 'product_not_found':
        setServerError(this.form.controls.productId, error.message);
        return;
    }

    if (!applyServerErrors(this.form, error)) {
      this.formError.set(error.message);
    }
  }
}

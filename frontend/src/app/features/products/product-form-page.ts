import { ChangeDetectionStrategy, Component, computed, inject, input, OnInit, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { ApiError } from '../../core/http/api-error';
import { NotificationService } from '../../core/notifications/notification.service';
import { applyServerErrors, setServerError } from '../../shared/forms/server-errors';
import {
  descriptionMaxLength,
  nameMaxLength,
  Product,
  skuPattern,
  unitPriceMax,
} from './product.models';
import { ProductsService } from './products.service';

/**
 * Formulario de producto. La misma pantalla crea (`/products/new`) y edita (`/products/:id/edit`);
 * al editar, el SKU se muestra pero no se puede cambiar.
 */
@Component({
  selector: 'app-product-form-page',
  imports: [ReactiveFormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './product-form-page.html',
})
export class ProductFormPage implements OnInit {
  private readonly products = inject(ProductsService);
  private readonly notifications = inject(NotificationService);
  private readonly router = inject(Router);

  /** Id de la ruta `:id/edit`; llega solo con `withComponentInputBinding`. */
  readonly id = input<string>();

  protected readonly isEdit = computed(() => this.id() !== undefined);
  protected readonly loading = signal(false);
  protected readonly loadError = signal<string | null>(null);
  protected readonly saving = signal(false);
  protected readonly formError = signal<string | null>(null);

  protected readonly limits = { nameMaxLength, descriptionMaxLength, unitPriceMax };

  protected readonly form = inject(NonNullableFormBuilder).group({
    sku: ['', [Validators.required, Validators.pattern(skuPattern)]],
    name: ['', [Validators.required, Validators.maxLength(nameMaxLength)]],
    description: ['', [Validators.maxLength(descriptionMaxLength)]],
    unitPrice: [null as number | null, [Validators.required, Validators.min(0), Validators.max(unitPriceMax)]],
  });

  ngOnInit(): void {
    const id = this.id();
    if (id === undefined) {
      return;
    }

    this.form.controls.sku.disable();
    this.loading.set(true);
    this.products.get(Number(id)).subscribe({
      next: (product) => {
        this.form.patchValue({
          sku: product.sku,
          name: product.name,
          description: product.description ?? '',
          unitPrice: product.unitPrice,
        });
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.loading.set(false);
        this.loadError.set(error instanceof ApiError ? error.message : 'No se pudo cargar el producto.');
      },
    });
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const details = {
      name: value.name.trim(),
      description: value.description.trim() || null,
      unitPrice: value.unitPrice ?? 0,
    };

    this.saving.set(true);
    this.formError.set(null);

    const id = this.id();
    const request =
      id === undefined
        ? this.products.create({ sku: value.sku.trim().toUpperCase(), ...details })
        : this.products.update(Number(id), details);

    request.subscribe({
      next: (product: Product) => {
        this.notifications.success(id === undefined ? 'Producto creado.' : 'Producto actualizado.');
        void this.router.navigate(['/products', product.id]);
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.showError(error);
      },
    });
  }

  private showError(error: unknown): void {
    if (!(error instanceof ApiError)) {
      this.formError.set('No se pudo guardar el producto.');
      return;
    }

    if (error.code === 'duplicate_sku' || error.code === 'invalid_sku') {
      setServerError(this.form.controls.sku, error.message);
      return;
    }

    if (!applyServerErrors(this.form, error)) {
      this.formError.set(error.message);
    }
  }
}

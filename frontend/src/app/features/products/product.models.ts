export interface Product {
  id: number;
  sku: string;
  name: string;
  description: string | null;
  unitPrice: number;
  currentStock: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateProductRequest {
  sku: string;
  name: string;
  description: string | null;
  unitPrice: number;
}

export type UpdateProductRequest = Omit<CreateProductRequest, 'sku'>;

/**
 * Reglas del SKU iguales a las del dominio: empieza por letra o número,
 * luego letras, números o guiones; de 3 a 30 caracteres. Se guarda en mayúsculas.
 */
export const skuPattern = /^[A-Za-z0-9][A-Za-z0-9-]{2,29}$/;
export const nameMaxLength = 150;
export const descriptionMaxLength = 500;
export const unitPriceMax = 9_999_999_999.99;

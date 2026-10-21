-- =============================================================================
-- 01_schema.sql
-- Esquema base del sistema de inventario: usuarios, productos y movimientos.
-- Script idempotente: puede ejecutarse varias veces sin error.
-- =============================================================================

SET client_min_messages = warning;

CREATE SCHEMA IF NOT EXISTS inventory;

-- -----------------------------------------------------------------------------
-- Usuarios que se autentican en la API.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS inventory.users (
    id            BIGINT       GENERATED ALWAYS AS IDENTITY,
    username      VARCHAR(50)  NOT NULL,
    password_hash VARCHAR(100) NOT NULL,
    full_name     VARCHAR(100) NOT NULL,
    role          VARCHAR(20)  NOT NULL DEFAULT 'Operator',
    is_active     BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at    TIMESTAMPTZ  NOT NULL DEFAULT now(),

    CONSTRAINT pk_users PRIMARY KEY (id),
    CONSTRAINT ck_users_username_format CHECK (username ~ '^[a-z0-9._-]{3,50}$'),
    CONSTRAINT ck_users_role CHECK (role IN ('Admin', 'Operator')),
    CONSTRAINT ck_users_full_name_not_blank CHECK (btrim(full_name) <> '')
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_users_username
    ON inventory.users (username);

-- -----------------------------------------------------------------------------
-- Catálogo de productos con su stock vigente.
-- current_stock solo se modifica desde los procedimientos de movimientos.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS inventory.products (
    id            BIGINT        GENERATED ALWAYS AS IDENTITY,
    sku           VARCHAR(30)   NOT NULL,
    name          VARCHAR(150)  NOT NULL,
    description   VARCHAR(500),
    unit_price    NUMERIC(12,2) NOT NULL DEFAULT 0,
    current_stock INTEGER       NOT NULL DEFAULT 0,
    is_active     BOOLEAN       NOT NULL DEFAULT TRUE,
    created_at    TIMESTAMPTZ   NOT NULL DEFAULT now(),
    updated_at    TIMESTAMPTZ   NOT NULL DEFAULT now(),

    CONSTRAINT pk_products PRIMARY KEY (id),
    CONSTRAINT ck_products_sku_format CHECK (sku ~ '^[A-Z0-9][A-Z0-9-]{2,29}$'),
    CONSTRAINT ck_products_name_not_blank CHECK (btrim(name) <> ''),
    CONSTRAINT ck_products_unit_price_non_negative CHECK (unit_price >= 0),
    CONSTRAINT ck_products_current_stock_non_negative CHECK (current_stock >= 0)
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_products_sku
    ON inventory.products (sku);

CREATE INDEX IF NOT EXISTS ix_products_name
    ON inventory.products (lower(name));

-- -----------------------------------------------------------------------------
-- Libro de movimientos de inventario (entradas y salidas).
-- stock_after registra el stock resultante para trazabilidad.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS inventory.movements (
    id            BIGINT       GENERATED ALWAYS AS IDENTITY,
    product_id    BIGINT       NOT NULL,
    movement_type VARCHAR(3)   NOT NULL,
    quantity      INTEGER      NOT NULL,
    stock_after   INTEGER      NOT NULL,
    reason        VARCHAR(250),
    user_id       BIGINT       NOT NULL,
    created_at    TIMESTAMPTZ  NOT NULL DEFAULT now(),

    CONSTRAINT pk_movements PRIMARY KEY (id),
    CONSTRAINT fk_movements_product FOREIGN KEY (product_id)
        REFERENCES inventory.products (id) ON DELETE RESTRICT,
    CONSTRAINT fk_movements_user FOREIGN KEY (user_id)
        REFERENCES inventory.users (id) ON DELETE RESTRICT,
    CONSTRAINT ck_movements_type CHECK (movement_type IN ('IN', 'OUT')),
    CONSTRAINT ck_movements_quantity_positive CHECK (quantity > 0),
    CONSTRAINT ck_movements_stock_after_non_negative CHECK (stock_after >= 0)
);

CREATE INDEX IF NOT EXISTS ix_movements_product_created_at
    ON inventory.movements (product_id, created_at DESC);

CREATE INDEX IF NOT EXISTS ix_movements_user
    ON inventory.movements (user_id);

-- -----------------------------------------------------------------------------
-- Mantiene updated_at al modificar un producto.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION inventory.fn_set_updated_at()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    NEW.updated_at := now();
    RETURN NEW;
END;
$$;

CREATE OR REPLACE TRIGGER trg_products_set_updated_at
    BEFORE UPDATE ON inventory.products
    FOR EACH ROW
    EXECUTE FUNCTION inventory.fn_set_updated_at();

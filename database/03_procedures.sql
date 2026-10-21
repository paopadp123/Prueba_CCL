-- =============================================================================
-- 03_procedures.sql
-- Procedimientos de inventario: registro de entradas y salidas, y consultas de
-- stock e historial de movimientos.
--
-- Errores de negocio (SQLSTATE propios, los traduce la capa de infraestructura):
--   IN001  Producto no encontrado
--   IN002  Stock insuficiente
--   IN003  Producto inactivo
--   IN004  Usuario no encontrado o inactivo
--   IN005  Cantidad inválida
--
-- Script idempotente: puede ejecutarse varias veces sin error.
-- =============================================================================

SET client_min_messages = warning;

-- -----------------------------------------------------------------------------
-- Núcleo común de entradas y salidas.
-- Bloquea la fila del producto (FOR UPDATE) para que movimientos concurrentes
-- sobre el mismo producto se apliquen en serie y el stock nunca quede negativo.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE PROCEDURE inventory.sp_register_movement(
    IN  p_movement_type VARCHAR,
    IN  p_product_id    BIGINT,
    IN  p_quantity      INTEGER,
    IN  p_user_id       BIGINT,
    IN  p_reason        VARCHAR,
    OUT o_movement_id   BIGINT,
    OUT o_stock_after   INTEGER
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_current_stock INTEGER;
    v_is_active     BOOLEAN;
BEGIN
    IF p_movement_type NOT IN ('IN', 'OUT') THEN
        RAISE EXCEPTION 'Tipo de movimiento inválido: %', p_movement_type
            USING ERRCODE = 'invalid_parameter_value';
    END IF;

    IF p_quantity IS NULL OR p_quantity <= 0 THEN
        RAISE EXCEPTION 'La cantidad debe ser mayor que cero'
            USING ERRCODE = 'IN005';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM inventory.users WHERE id = p_user_id AND is_active) THEN
        RAISE EXCEPTION 'El usuario % no existe o está inactivo', p_user_id
            USING ERRCODE = 'IN004';
    END IF;

    SELECT current_stock, is_active
    INTO v_current_stock, v_is_active
    FROM inventory.products
    WHERE id = p_product_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'El producto % no existe', p_product_id
            USING ERRCODE = 'IN001';
    END IF;

    IF NOT v_is_active THEN
        RAISE EXCEPTION 'El producto % está inactivo', p_product_id
            USING ERRCODE = 'IN003';
    END IF;

    o_stock_after := CASE p_movement_type
        WHEN 'IN' THEN v_current_stock + p_quantity
        ELSE v_current_stock - p_quantity
    END;

    IF o_stock_after < 0 THEN
        RAISE EXCEPTION 'Stock insuficiente: disponible %, solicitado %', v_current_stock, p_quantity
            USING ERRCODE = 'IN002',
                  DETAIL = format('available=%s;requested=%s', v_current_stock, p_quantity);
    END IF;

    PERFORM set_config('app.user_id', p_user_id::TEXT, TRUE);

    UPDATE inventory.products
    SET current_stock = o_stock_after
    WHERE id = p_product_id;

    INSERT INTO inventory.movements (product_id, movement_type, quantity, stock_after, reason, user_id)
    VALUES (p_product_id, p_movement_type, p_quantity, o_stock_after, NULLIF(btrim(p_reason), ''), p_user_id)
    RETURNING id INTO o_movement_id;
END;
$$;

-- -----------------------------------------------------------------------------
-- Registra una entrada de inventario.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE PROCEDURE inventory.sp_register_entry(
    IN  p_product_id  BIGINT,
    IN  p_quantity    INTEGER,
    IN  p_user_id     BIGINT,
    IN  p_reason      VARCHAR,
    OUT o_movement_id BIGINT,
    OUT o_stock_after INTEGER
)
LANGUAGE plpgsql
AS $$
BEGIN
    CALL inventory.sp_register_movement('IN', p_product_id, p_quantity, p_user_id, p_reason,
                                        o_movement_id, o_stock_after);
END;
$$;

-- -----------------------------------------------------------------------------
-- Registra una salida de inventario; falla con IN002 si no hay stock suficiente.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE PROCEDURE inventory.sp_register_exit(
    IN  p_product_id  BIGINT,
    IN  p_quantity    INTEGER,
    IN  p_user_id     BIGINT,
    IN  p_reason      VARCHAR,
    OUT o_movement_id BIGINT,
    OUT o_stock_after INTEGER
)
LANGUAGE plpgsql
AS $$
BEGIN
    CALL inventory.sp_register_movement('OUT', p_product_id, p_quantity, p_user_id, p_reason,
                                        o_movement_id, o_stock_after);
END;
$$;

-- -----------------------------------------------------------------------------
-- Consulta paginada del stock actual.
-- p_search filtra por SKU o nombre (sin distinguir mayúsculas).
-- total_count devuelve el total de filas del filtro para paginar.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION inventory.fn_get_stock(
    p_search      VARCHAR DEFAULT NULL,
    p_only_active BOOLEAN DEFAULT TRUE,
    p_limit       INTEGER DEFAULT 50,
    p_offset      INTEGER DEFAULT 0
)
RETURNS TABLE (
    product_id    BIGINT,
    sku           VARCHAR,
    name          VARCHAR,
    unit_price    NUMERIC,
    current_stock INTEGER,
    stock_value   NUMERIC,
    is_active     BOOLEAN,
    updated_at    TIMESTAMPTZ,
    total_count   BIGINT
)
LANGUAGE sql
STABLE
AS $$
    SELECT p.id,
           p.sku,
           p.name,
           p.unit_price,
           p.current_stock,
           p.unit_price * p.current_stock,
           p.is_active,
           p.updated_at,
           count(*) OVER ()
    FROM inventory.products p
    WHERE (NOT p_only_active OR p.is_active)
      AND (p_search IS NULL
           OR p.sku ILIKE '%' || p_search || '%'
           OR p.name ILIKE '%' || p_search || '%')
    ORDER BY p.name, p.id
    LIMIT LEAST(GREATEST(p_limit, 1), 200)
    OFFSET GREATEST(p_offset, 0);
$$;

-- -----------------------------------------------------------------------------
-- Historial paginado de movimientos, opcionalmente filtrado por producto.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION inventory.fn_get_movements(
    p_product_id BIGINT  DEFAULT NULL,
    p_limit      INTEGER DEFAULT 50,
    p_offset     INTEGER DEFAULT 0
)
RETURNS TABLE (
    movement_id   BIGINT,
    product_id    BIGINT,
    sku           VARCHAR,
    product_name  VARCHAR,
    movement_type VARCHAR,
    quantity      INTEGER,
    stock_after   INTEGER,
    reason        VARCHAR,
    username      VARCHAR,
    created_at    TIMESTAMPTZ,
    total_count   BIGINT
)
LANGUAGE sql
STABLE
AS $$
    SELECT m.id,
           m.product_id,
           p.sku,
           p.name,
           m.movement_type,
           m.quantity,
           m.stock_after,
           m.reason,
           u.username,
           m.created_at,
           count(*) OVER ()
    FROM inventory.movements m
    JOIN inventory.products p ON p.id = m.product_id
    JOIN inventory.users u ON u.id = m.user_id
    WHERE p_product_id IS NULL OR m.product_id = p_product_id
    ORDER BY m.created_at DESC, m.id DESC
    LIMIT LEAST(GREATEST(p_limit, 1), 200)
    OFFSET GREATEST(p_offset, 0);
$$;

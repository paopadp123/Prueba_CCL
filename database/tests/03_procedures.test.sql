-- =============================================================================
-- 03_procedures.test.sql
-- Verifica los procedimientos de entradas, salidas y consultas. Se ejecuta
-- dentro de una transacción que se revierte al final, por lo que no deja datos.
-- =============================================================================

SET client_min_messages = notice;

BEGIN;

CREATE OR REPLACE FUNCTION pg_temp.assert_fails(statement TEXT, expected_sqlstate TEXT, description TEXT)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    BEGIN
        EXECUTE statement;
    EXCEPTION WHEN OTHERS THEN
        IF SQLSTATE <> expected_sqlstate THEN
            RAISE EXCEPTION 'FAIL %: se esperaba SQLSTATE % y se obtuvo % (%)',
                description, expected_sqlstate, SQLSTATE, SQLERRM;
        END IF;
        RAISE NOTICE 'OK   %', description;
        RETURN;
    END;
    RAISE EXCEPTION 'FAIL %: la sentencia no falló', description;
END;
$$;

CREATE OR REPLACE FUNCTION pg_temp.assert_true(condition BOOLEAN, description TEXT)
RETURNS VOID
LANGUAGE plpgsql
AS $$
BEGIN
    IF condition IS NOT TRUE THEN
        RAISE EXCEPTION 'FAIL %', description;
    END IF;
    RAISE NOTICE 'OK   %', description;
END;
$$;

DO $test$
DECLARE
    v_user_id          BIGINT;
    v_inactive_user_id BIGINT;
    v_product_id       BIGINT;
    v_inactive_id      BIGINT;
    v_movement_id      BIGINT;
    v_stock_after      INTEGER;
    v_movements_before BIGINT;
    v_total            BIGINT;
BEGIN
    INSERT INTO inventory.users (username, password_hash, full_name)
    VALUES ('proc.user', 'hash', 'Usuario de procedimientos')
    RETURNING id INTO v_user_id;

    INSERT INTO inventory.users (username, password_hash, full_name, is_active)
    VALUES ('proc.inactive', 'hash', 'Usuario inactivo', FALSE)
    RETURNING id INTO v_inactive_user_id;

    INSERT INTO inventory.products (sku, name, unit_price)
    VALUES ('PROC-001', 'Producto procedimientos', 1000)
    RETURNING id INTO v_product_id;

    INSERT INTO inventory.products (sku, name, is_active)
    VALUES ('PROC-002', 'Producto inactivo', FALSE)
    RETURNING id INTO v_inactive_id;

    CALL inventory.sp_register_entry(v_product_id, 10, v_user_id, 'Compra', v_movement_id, v_stock_after);

    PERFORM pg_temp.assert_true(v_stock_after = 10, 'entrada devuelve el stock resultante');
    PERFORM pg_temp.assert_true(
        (SELECT current_stock FROM inventory.products WHERE id = v_product_id) = 10,
        'entrada suma al stock del producto');
    PERFORM pg_temp.assert_true(
        EXISTS (SELECT 1 FROM inventory.movements
                WHERE id = v_movement_id AND movement_type = 'IN' AND quantity = 10
                  AND stock_after = 10 AND reason = 'Compra' AND user_id = v_user_id),
        'entrada registra el movimiento');
    PERFORM pg_temp.assert_true(
        EXISTS (SELECT 1 FROM inventory.audit_log
                WHERE table_name = 'movements' AND record_id = v_movement_id AND app_user_id = v_user_id),
        'entrada queda auditada con el usuario');

    CALL inventory.sp_register_exit(v_product_id, 4, v_user_id, '  ', v_movement_id, v_stock_after);

    PERFORM pg_temp.assert_true(v_stock_after = 6, 'salida descuenta del stock');
    PERFORM pg_temp.assert_true(
        (SELECT reason FROM inventory.movements WHERE id = v_movement_id) IS NULL,
        'motivo en blanco se guarda como nulo');

    CALL inventory.sp_register_exit(v_product_id, 6, v_user_id, 'Venta total', v_movement_id, v_stock_after);
    PERFORM pg_temp.assert_true(v_stock_after = 0, 'salida puede dejar el stock exactamente en cero');

    SELECT count(*) INTO v_movements_before FROM inventory.movements;

    PERFORM pg_temp.assert_fails(
        format('CALL inventory.sp_register_exit(%s, 1, %s, NULL, NULL, NULL)', v_product_id, v_user_id),
        'IN002', 'salida mayor al stock falla con IN002');

    PERFORM pg_temp.assert_true(
        (SELECT current_stock FROM inventory.products WHERE id = v_product_id) = 0
        AND (SELECT count(*) FROM inventory.movements) = v_movements_before,
        'salida rechazada no modifica stock ni movimientos');

    PERFORM pg_temp.assert_fails(
        format('CALL inventory.sp_register_entry(-1, 1, %s, NULL, NULL, NULL)', v_user_id),
        'IN001', 'producto inexistente falla con IN001');

    PERFORM pg_temp.assert_fails(
        format('CALL inventory.sp_register_entry(%s, 1, %s, NULL, NULL, NULL)', v_inactive_id, v_user_id),
        'IN003', 'producto inactivo falla con IN003');

    PERFORM pg_temp.assert_fails(
        format('CALL inventory.sp_register_entry(%s, 1, %s, NULL, NULL, NULL)', v_product_id, v_inactive_user_id),
        'IN004', 'usuario inactivo falla con IN004');

    PERFORM pg_temp.assert_fails(
        format('CALL inventory.sp_register_entry(%s, 1, -1, NULL, NULL, NULL)', v_product_id),
        'IN004', 'usuario inexistente falla con IN004');

    PERFORM pg_temp.assert_fails(
        format('CALL inventory.sp_register_entry(%s, 0, %s, NULL, NULL, NULL)', v_product_id, v_user_id),
        'IN005', 'cantidad cero falla con IN005');

    PERFORM pg_temp.assert_fails(
        format('CALL inventory.sp_register_exit(%s, -3, %s, NULL, NULL, NULL)', v_product_id, v_user_id),
        'IN005', 'cantidad negativa falla con IN005');

    PERFORM pg_temp.assert_true(
        (SELECT count(*) FROM inventory.fn_get_stock('proc-001')) = 1,
        'fn_get_stock filtra por SKU sin distinguir mayúsculas');

    PERFORM pg_temp.assert_true(
        NOT EXISTS (SELECT 1 FROM inventory.fn_get_stock('PROC-002')),
        'fn_get_stock excluye inactivos por defecto');

    PERFORM pg_temp.assert_true(
        EXISTS (SELECT 1 FROM inventory.fn_get_stock('PROC-002', FALSE)),
        'fn_get_stock incluye inactivos si se pide');

    SELECT total_count INTO v_total FROM inventory.fn_get_stock('PROC', FALSE, 1, 0) LIMIT 1;
    PERFORM pg_temp.assert_true(
        v_total = 2 AND (SELECT count(*) FROM inventory.fn_get_stock('PROC', FALSE, 1, 0)) = 1,
        'fn_get_stock pagina y devuelve el total del filtro');

    PERFORM pg_temp.assert_true(
        (SELECT array_agg(movement_type ORDER BY movement_id)
         FROM inventory.fn_get_movements(v_product_id)) = ARRAY['IN', 'OUT', 'OUT']::VARCHAR[],
        'fn_get_movements devuelve el historial del producto');

    PERFORM pg_temp.assert_true(
        (SELECT username FROM inventory.fn_get_movements(v_product_id) LIMIT 1) = 'proc.user',
        'fn_get_movements incluye el usuario que registró el movimiento');
END;
$test$;

ROLLBACK;

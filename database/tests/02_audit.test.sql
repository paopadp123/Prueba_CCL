-- =============================================================================
-- 02_audit.test.sql
-- Verifica la auditoría de cambios. Se ejecuta dentro de una transacción
-- que se revierte al final, por lo que no deja datos.
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
    test_user_id    BIGINT;
    test_product_id BIGINT;
    entry           inventory.audit_log;
    entries_before  BIGINT;
BEGIN
    INSERT INTO inventory.users (username, password_hash, full_name)
    VALUES ('audit.user', 'secret-hash', 'Usuario de auditoría')
    RETURNING id INTO test_user_id;

    SELECT * INTO entry FROM inventory.audit_log
    WHERE table_name = 'users' AND record_id = test_user_id;

    PERFORM pg_temp.assert_true(entry.operation = 'INSERT', 'INSERT de usuario queda auditado');
    PERFORM pg_temp.assert_true(NOT entry.new_data ? 'password_hash', 'password_hash no se audita');
    PERFORM pg_temp.assert_true(entry.app_user_id IS NULL, 'sin app.user_id el usuario de aplicación queda nulo');

    PERFORM set_config('app.user_id', test_user_id::TEXT, TRUE);

    INSERT INTO inventory.products (sku, name, unit_price)
    VALUES ('AUDIT-001', 'Producto auditado', 100)
    RETURNING id INTO test_product_id;

    SELECT * INTO entry FROM inventory.audit_log
    WHERE table_name = 'products' AND record_id = test_product_id AND operation = 'INSERT';

    PERFORM pg_temp.assert_true(entry.app_user_id = test_user_id, 'registra el usuario de aplicación de app.user_id');
    PERFORM pg_temp.assert_true(entry.old_data IS NULL AND entry.new_data->>'sku' = 'AUDIT-001', 'INSERT guarda solo datos nuevos');

    UPDATE inventory.products SET unit_price = 120 WHERE id = test_product_id;

    SELECT * INTO entry FROM inventory.audit_log
    WHERE table_name = 'products' AND record_id = test_product_id AND operation = 'UPDATE';

    PERFORM pg_temp.assert_true(
        (entry.old_data->>'unit_price')::NUMERIC = 100 AND (entry.new_data->>'unit_price')::NUMERIC = 120,
        'UPDATE guarda datos anteriores y nuevos');

    SELECT count(*) INTO entries_before FROM inventory.audit_log;
    UPDATE inventory.products SET unit_price = 120 WHERE id = test_product_id;
    PERFORM pg_temp.assert_true(
        (SELECT count(*) FROM inventory.audit_log) = entries_before,
        'UPDATE sin cambios reales no se audita');

    INSERT INTO inventory.movements (product_id, movement_type, quantity, stock_after, user_id)
    VALUES (test_product_id, 'IN', 3, 3, test_user_id);

    PERFORM pg_temp.assert_true(
        EXISTS (SELECT 1 FROM inventory.audit_log WHERE table_name = 'movements' AND operation = 'INSERT'),
        'INSERT de movimiento queda auditado');

    INSERT INTO inventory.products (sku, name) VALUES ('AUDIT-002', 'Producto a borrar');
    DELETE FROM inventory.products WHERE sku = 'AUDIT-002';

    SELECT * INTO entry FROM inventory.audit_log
    WHERE table_name = 'products' AND operation = 'DELETE';

    PERFORM pg_temp.assert_true(
        entry.new_data IS NULL AND entry.old_data->>'sku' = 'AUDIT-002',
        'DELETE guarda solo datos anteriores');

    PERFORM pg_temp.assert_fails(
        $q$UPDATE inventory.audit_log SET operation = 'DELETE'$q$,
        '42501', 'el registro de auditoría no se puede modificar');

    PERFORM pg_temp.assert_fails(
        $q$DELETE FROM inventory.audit_log$q$,
        '42501', 'el registro de auditoría no se puede borrar');
END;
$test$;

ROLLBACK;

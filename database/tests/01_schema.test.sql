-- =============================================================================
-- 01_schema.test.sql
-- Verifica las restricciones del esquema. Se ejecuta dentro de una transacción
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

DO $test$
BEGIN
    INSERT INTO inventory.users (username, password_hash, full_name)
    VALUES ('test.user', 'hash', 'Usuario de prueba');

    INSERT INTO inventory.products (sku, name, unit_price)
    VALUES ('TEST-001', 'Producto de prueba', 10.50);

    PERFORM pg_temp.assert_fails(
        $q$INSERT INTO inventory.users (username, password_hash, full_name) VALUES ('test.user', 'hash', 'Duplicado')$q$,
        '23505', 'username duplicado');

    PERFORM pg_temp.assert_fails(
        $q$INSERT INTO inventory.users (username, password_hash, full_name) VALUES ('Mayus', 'hash', 'Nombre')$q$,
        '23514', 'username con formato inválido');

    PERFORM pg_temp.assert_fails(
        $q$INSERT INTO inventory.users (username, password_hash, full_name, role) VALUES ('otro', 'hash', 'Nombre', 'Root')$q$,
        '23514', 'rol inválido');

    PERFORM pg_temp.assert_fails(
        $q$INSERT INTO inventory.products (sku, name) VALUES ('TEST-001', 'Duplicado')$q$,
        '23505', 'SKU duplicado');

    PERFORM pg_temp.assert_fails(
        $q$INSERT INTO inventory.products (sku, name) VALUES ('ab-1', 'Minúsculas')$q$,
        '23514', 'SKU con formato inválido');

    PERFORM pg_temp.assert_fails(
        $q$INSERT INTO inventory.products (sku, name) VALUES ('TEST-002', '   ')$q$,
        '23514', 'nombre de producto vacío');

    PERFORM pg_temp.assert_fails(
        $q$INSERT INTO inventory.products (sku, name, unit_price) VALUES ('TEST-003', 'Precio', -1)$q$,
        '23514', 'precio negativo');

    PERFORM pg_temp.assert_fails(
        $q$UPDATE inventory.products SET current_stock = -1 WHERE sku = 'TEST-001'$q$,
        '23514', 'stock negativo');

    PERFORM pg_temp.assert_fails(
        $q$INSERT INTO inventory.movements (product_id, movement_type, quantity, stock_after, user_id)
           SELECT p.id, 'IN', 0, 0, u.id FROM inventory.products p, inventory.users u$q$,
        '23514', 'cantidad de movimiento en cero');

    PERFORM pg_temp.assert_fails(
        $q$INSERT INTO inventory.movements (product_id, movement_type, quantity, stock_after, user_id)
           SELECT p.id, 'ADJ', 1, 1, u.id FROM inventory.products p, inventory.users u$q$,
        '23514', 'tipo de movimiento inválido');

    PERFORM pg_temp.assert_fails(
        $q$INSERT INTO inventory.movements (product_id, movement_type, quantity, stock_after, user_id)
           SELECT -1, 'IN', 1, 1, u.id FROM inventory.users u$q$,
        '23503', 'movimiento con producto inexistente');

    INSERT INTO inventory.movements (product_id, movement_type, quantity, stock_after, user_id)
    SELECT p.id, 'IN', 5, 5, u.id FROM inventory.products p, inventory.users u;

    PERFORM pg_temp.assert_fails(
        $q$DELETE FROM inventory.products WHERE sku = 'TEST-001'$q$,
        '23503', 'borrar producto con movimientos');
END;
$test$;

DO $$
DECLARE
    after_update TIMESTAMPTZ;
BEGIN
    UPDATE inventory.products SET updated_at = now() - INTERVAL '1 day' WHERE sku = 'TEST-001';
    UPDATE inventory.products SET name = 'Renombrado' WHERE sku = 'TEST-001'
    RETURNING updated_at INTO after_update;
    IF after_update IS DISTINCT FROM now() THEN
        RAISE EXCEPTION 'FAIL updated_at no se actualizó (%)', after_update;
    END IF;
    RAISE NOTICE 'OK   updated_at se actualiza al modificar';
END;
$$;

ROLLBACK;

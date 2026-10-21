-- =============================================================================
-- 04_seed.sql
-- Datos iniciales para desarrollo y demostración.
-- Usuario admin / contraseña Admin123* (hash BCrypt compatible con BCrypt.Net).
-- El stock inicial se carga con sp_register_entry para que quede trazado en
-- movimientos y auditoría como cualquier otro ingreso.
-- Script idempotente: puede ejecutarse varias veces sin error.
-- =============================================================================

SET client_min_messages = warning;

CREATE EXTENSION IF NOT EXISTS pgcrypto;

INSERT INTO inventory.users (username, password_hash, full_name, role)
VALUES
    ('admin',    crypt('Admin123*', gen_salt('bf', 11)), 'Administrador', 'Admin'),
    ('operador', crypt('Operador123*', gen_salt('bf', 11)), 'Operador de bodega', 'Operator')
ON CONFLICT (username) DO NOTHING;

INSERT INTO inventory.products (sku, name, description, unit_price)
VALUES
    ('LAP-001', 'Portátil 14 pulgadas', 'Portátil de oficina, 16 GB RAM, 512 GB SSD', 3200000),
    ('MON-001', 'Monitor 24 pulgadas', 'Monitor IPS Full HD', 750000),
    ('TEC-001', 'Teclado mecánico', 'Teclado mecánico en español', 280000),
    ('MOU-001', 'Mouse inalámbrico', 'Mouse óptico inalámbrico', 95000),
    ('AUD-001', 'Audífonos con micrófono', 'Audífonos USB para videollamadas', 160000),
    ('CAB-001', 'Cable HDMI 2 m', 'Cable HDMI 2.0', 25000)
ON CONFLICT (sku) DO NOTHING;

DO $$
DECLARE
    v_admin_id    BIGINT;
    v_product     RECORD;
    v_movement_id BIGINT;
    v_stock_after INTEGER;
BEGIN
    SELECT id INTO v_admin_id FROM inventory.users WHERE username = 'admin';

    FOR v_product IN
        SELECT p.id, s.initial_stock
        FROM inventory.products p
        JOIN (VALUES ('LAP-001', 10), ('MON-001', 15), ('TEC-001', 25),
                     ('MOU-001', 40), ('AUD-001', 20), ('CAB-001', 60)) AS s (sku, initial_stock)
          ON s.sku = p.sku
        WHERE NOT EXISTS (SELECT 1 FROM inventory.movements m WHERE m.product_id = p.id)
    LOOP
        CALL inventory.sp_register_entry(v_product.id, v_product.initial_stock, v_admin_id,
                                         'Inventario inicial', v_movement_id, v_stock_after);
    END LOOP;
END;
$$;

-- =============================================================================
-- 02_audit.sql
-- Auditoría de cambios: registra en inventory.audit_log cada INSERT, UPDATE y
-- DELETE sobre las tablas de negocio, con los datos anteriores y nuevos en JSONB.
-- El usuario de la aplicación se toma de la variable de sesión app.user_id,
-- que fijan los procedimientos almacenados con SET LOCAL.
-- Script idempotente: puede ejecutarse varias veces sin error.
-- =============================================================================

SET client_min_messages = warning;

-- -----------------------------------------------------------------------------
-- Registro de auditoría (solo inserciones).
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS inventory.audit_log (
    id          BIGINT       GENERATED ALWAYS AS IDENTITY,
    table_name  VARCHAR(63)  NOT NULL,
    operation   VARCHAR(6)   NOT NULL,
    record_id   BIGINT       NOT NULL,
    old_data    JSONB,
    new_data    JSONB,
    app_user_id BIGINT,
    db_user     VARCHAR(63)  NOT NULL DEFAULT current_user,
    changed_at  TIMESTAMPTZ  NOT NULL DEFAULT clock_timestamp(),

    CONSTRAINT pk_audit_log PRIMARY KEY (id),
    CONSTRAINT ck_audit_log_operation CHECK (operation IN ('INSERT', 'UPDATE', 'DELETE'))
);

CREATE INDEX IF NOT EXISTS ix_audit_log_table_record
    ON inventory.audit_log (table_name, record_id);

CREATE INDEX IF NOT EXISTS ix_audit_log_changed_at
    ON inventory.audit_log (changed_at DESC);

-- -----------------------------------------------------------------------------
-- Trigger genérico de auditoría.
-- Excluye password_hash y omite UPDATE sin cambios reales (ignorando updated_at).
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION inventory.fn_audit_row_change()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
DECLARE
    excluded_keys CONSTANT TEXT[] := ARRAY['password_hash'];
    old_row       JSONB;
    new_row       JSONB;
BEGIN
    IF TG_OP IN ('UPDATE', 'DELETE') THEN
        old_row := to_jsonb(OLD) - excluded_keys;
    END IF;

    IF TG_OP IN ('INSERT', 'UPDATE') THEN
        new_row := to_jsonb(NEW) - excluded_keys;
    END IF;

    IF TG_OP = 'UPDATE' AND (old_row - 'updated_at') = (new_row - 'updated_at') THEN
        RETURN NULL;
    END IF;

    INSERT INTO inventory.audit_log (table_name, operation, record_id, old_data, new_data, app_user_id)
    VALUES (
        TG_TABLE_NAME,
        TG_OP,
        (COALESCE(new_row, old_row)->>'id')::BIGINT,
        old_row,
        new_row,
        NULLIF(current_setting('app.user_id', TRUE), '')::BIGINT
    );

    RETURN NULL;
END;
$$;

CREATE OR REPLACE TRIGGER trg_users_audit
    AFTER INSERT OR UPDATE OR DELETE ON inventory.users
    FOR EACH ROW
    EXECUTE FUNCTION inventory.fn_audit_row_change();

CREATE OR REPLACE TRIGGER trg_products_audit
    AFTER INSERT OR UPDATE OR DELETE ON inventory.products
    FOR EACH ROW
    EXECUTE FUNCTION inventory.fn_audit_row_change();

CREATE OR REPLACE TRIGGER trg_movements_audit
    AFTER INSERT OR UPDATE OR DELETE ON inventory.movements
    FOR EACH ROW
    EXECUTE FUNCTION inventory.fn_audit_row_change();

-- -----------------------------------------------------------------------------
-- Impide modificar o borrar registros de auditoría.
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION inventory.fn_prevent_audit_change()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'El registro de auditoría no se puede modificar ni eliminar'
        USING ERRCODE = 'insufficient_privilege';
END;
$$;

CREATE OR REPLACE TRIGGER trg_audit_log_read_only
    BEFORE UPDATE OR DELETE ON inventory.audit_log
    FOR EACH ROW
    EXECUTE FUNCTION inventory.fn_prevent_audit_change();

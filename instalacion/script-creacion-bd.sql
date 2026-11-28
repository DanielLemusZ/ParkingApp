CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL,
    CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id)
);

START TRANSACTION;
CREATE TABLE sedes (
    id uuid NOT NULL,
    nombre character varying(100) NOT NULL,
    zona_horaria character varying(64) NOT NULL,
    CONSTRAINT pk_sedes PRIMARY KEY (id)
);

INSERT INTO sedes (id, nombre, zona_horaria)
VALUES ('019a8000-0000-7000-8000-000000000001', 'Sede principal UAM', 'America/Bogota');

INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
VALUES ('20261114161012_Inicial', '10.0.15');

COMMIT;

START TRANSACTION;
CREATE TABLE eventos_de_acceso (
    id uuid NOT NULL,
    tipo character varying(10) NOT NULL,
    placa character varying(10) NOT NULL,
    sede_id uuid NOT NULL,
    carril integer NOT NULL,
    confianza integer NOT NULL,
    fecha_hora timestamptz NOT NULL,
    CONSTRAINT pk_eventos_de_acceso PRIMARY KEY (id),
    CONSTRAINT ck_eventos_de_acceso_carril CHECK (carril > 0),
    CONSTRAINT ck_eventos_de_acceso_confianza CHECK (confianza BETWEEN 0 AND 100),
    CONSTRAINT fk_eventos_de_acceso_sedes_sede_id FOREIGN KEY (sede_id) REFERENCES sedes (id) ON DELETE RESTRICT
);

CREATE INDEX ix_eventos_de_acceso_placa_fecha_hora ON eventos_de_acceso (placa, fecha_hora);

CREATE INDEX ix_eventos_de_acceso_sede_id ON eventos_de_acceso (sede_id);

INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
VALUES ('20261116185424_AgregarEventosDeAcceso', '10.0.15');

COMMIT;

START TRANSACTION;
ALTER TABLE eventos_de_acceso ADD tipo_de_vehiculo character varying(20) NOT NULL DEFAULT 'NoClasificado';

UPDATE eventos_de_acceso
SET tipo_de_vehiculo = CASE
    WHEN placa ~ '^[A-Z]{3}[0-9]{3}$' THEN 'Carro'
    WHEN placa ~ '^[A-Z]{3}[0-9]{2}[A-Z]$' THEN 'Moto'
    ELSE 'NoClasificado'
END;

INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
VALUES ('20261117120000_AgregarTipoDeVehiculo', '10.0.15');

COMMIT;

START TRANSACTION;
CREATE TABLE tarifas (
    id uuid NOT NULL,
    tipo_de_vehiculo character varying(20) NOT NULL,
    franja character varying(10) NOT NULL,
    valor numeric(12,2) NOT NULL,
    vigente_desde date NOT NULL,
    vigente_hasta date,
    CONSTRAINT pk_tarifas PRIMARY KEY (id),
    CONSTRAINT ck_tarifas_valor_no_negativo CHECK (valor >= 0),
    CONSTRAINT ck_tarifas_vigencia CHECK (vigente_hasta IS NULL OR vigente_hasta >= vigente_desde)
);

CREATE UNIQUE INDEX ix_tarifas_tipo_de_vehiculo_franja_vigente_desde ON tarifas (tipo_de_vehiculo, franja, vigente_desde);

CREATE INDEX ix_tarifas_tipo_de_vehiculo_franja_vigente_hasta ON tarifas (tipo_de_vehiculo, franja, vigente_hasta);

INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
VALUES ('20261118100000_AgregarTarifas', '10.0.15');

COMMIT;

START TRANSACTION;
INSERT INTO tarifas (id, tipo_de_vehiculo, franja, valor, vigente_desde, vigente_hasta)
VALUES ('019a9000-0000-7000-8000-000000000001', 'Carro', 'Diurna', 8000.0, DATE '2026-11-01', NULL);
INSERT INTO tarifas (id, tipo_de_vehiculo, franja, valor, vigente_desde, vigente_hasta)
VALUES ('019a9000-0000-7000-8000-000000000002', 'Carro', 'Nocturna', 5000.0, DATE '2026-11-01', NULL);
INSERT INTO tarifas (id, tipo_de_vehiculo, franja, valor, vigente_desde, vigente_hasta)
VALUES ('019a9000-0000-7000-8000-000000000003', 'Moto', 'Diurna', 0.0, DATE '2026-11-01', NULL);
INSERT INTO tarifas (id, tipo_de_vehiculo, franja, valor, vigente_desde, vigente_hasta)
VALUES ('019a9000-0000-7000-8000-000000000004', 'Moto', 'Nocturna', 0.0, DATE '2026-11-01', NULL);

INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
VALUES ('20261118120000_SembrarTarifasIniciales', '10.0.15');

COMMIT;

START TRANSACTION;
ALTER TABLE sedes ADD cupo_carros integer NOT NULL DEFAULT 0;

ALTER TABLE sedes ADD cupo_motos integer NOT NULL DEFAULT 0;

ALTER TABLE sedes ADD inicio_franja_diurna time without time zone NOT NULL DEFAULT TIME '00:00:00';

ALTER TABLE sedes ADD inicio_franja_nocturna time without time zone NOT NULL DEFAULT TIME '00:00:00';

ALTER TABLE sedes ADD umbral_confianza integer NOT NULL DEFAULT 0;

UPDATE sedes SET cupo_carros = 50, cupo_motos = 50, inicio_franja_diurna = TIME '06:00:00', inicio_franja_nocturna = TIME '18:00:00', umbral_confianza = 80
WHERE id = '019a8000-0000-7000-8000-000000000001';

ALTER TABLE sedes ADD CONSTRAINT ck_sedes_cupos_no_negativos CHECK (cupo_carros >= 0 AND cupo_motos >= 0);

ALTER TABLE sedes ADD CONSTRAINT ck_sedes_franjas_distintas CHECK (inicio_franja_diurna <> inicio_franja_nocturna);

ALTER TABLE sedes ADD CONSTRAINT ck_sedes_umbral_confianza CHECK (umbral_confianza BETWEEN 0 AND 100);

INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
VALUES ('20261119160519_ConfigurarFranjasCuposUmbral', '10.0.15');

COMMIT;

START TRANSACTION;
CREATE TABLE cobros (
    id uuid NOT NULL,
    placa character varying(10) NOT NULL,
    tipo_de_vehiculo character varying(20) NOT NULL,
    sede_id uuid NOT NULL,
    dia_operativo date NOT NULL,
    franja character varying(10) NOT NULL,
    tarifa_id uuid NOT NULL,
    valor numeric(12,2) NOT NULL,
    estado character varying(10) NOT NULL,
    fecha_hora timestamptz NOT NULL,
    CONSTRAINT pk_cobros PRIMARY KEY (id),
    CONSTRAINT ck_cobros_valor_positivo CHECK (valor > 0),
    CONSTRAINT fk_cobros_sedes_sede_id FOREIGN KEY (sede_id) REFERENCES sedes (id) ON DELETE RESTRICT,
    CONSTRAINT fk_cobros_tarifas_tarifa_id FOREIGN KEY (tarifa_id) REFERENCES tarifas (id) ON DELETE RESTRICT
);

CREATE INDEX ix_cobros_placa_sede_id_dia_operativo ON cobros (placa, sede_id, dia_operativo);

CREATE INDEX ix_cobros_sede_id ON cobros (sede_id);

CREATE INDEX ix_cobros_tarifa_id ON cobros (tarifa_id);

INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
VALUES ('20261119204642_AgregarCobros', '10.0.15');

COMMIT;

START TRANSACTION;
DROP INDEX ix_cobros_placa_sede_id_dia_operativo;

CREATE UNIQUE INDEX ux_cobros_placa_sede_dia ON cobros (placa, sede_id, dia_operativo);

INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
VALUES ('20261120034513_CobroUnicoPorDia', '10.0.15');

COMMIT;

START TRANSACTION;
CREATE TABLE pagos (
    id uuid NOT NULL,
    cobro_id uuid NOT NULL,
    valor numeric(12,2) NOT NULL,
    medio character varying(20) NOT NULL,
    operador character varying(100) NOT NULL,
    referencia uuid NOT NULL,
    fecha_hora timestamptz NOT NULL,
    CONSTRAINT pk_pagos PRIMARY KEY (id),
    CONSTRAINT ck_pagos_valor_positivo CHECK (valor > 0),
    CONSTRAINT fk_pagos_cobros_cobro_id FOREIGN KEY (cobro_id) REFERENCES cobros (id) ON DELETE RESTRICT
);

CREATE UNIQUE INDEX ux_pagos_cobro ON pagos (cobro_id);

CREATE UNIQUE INDEX ux_pagos_referencia ON pagos (referencia);

INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
VALUES ('20261120210642_AgregarPagos', '10.0.15');

COMMIT;

START TRANSACTION;
CREATE TABLE asp_net_roles (
    id uuid NOT NULL,
    name character varying(256),
    normalized_name character varying(256),
    concurrency_stamp text,
    CONSTRAINT pk_asp_net_roles PRIMARY KEY (id)
);

CREATE TABLE asp_net_users (
    id uuid NOT NULL,
    user_name character varying(256),
    normalized_user_name character varying(256),
    email character varying(256),
    normalized_email character varying(256),
    email_confirmed boolean NOT NULL,
    password_hash text,
    security_stamp text,
    concurrency_stamp text,
    phone_number text,
    phone_number_confirmed boolean NOT NULL,
    two_factor_enabled boolean NOT NULL,
    lockout_end timestamptz,
    lockout_enabled boolean NOT NULL,
    access_failed_count integer NOT NULL,
    CONSTRAINT pk_asp_net_users PRIMARY KEY (id)
);

CREATE TABLE asp_net_role_claims (
    id integer GENERATED BY DEFAULT AS IDENTITY,
    role_id uuid NOT NULL,
    claim_type text,
    claim_value text,
    CONSTRAINT pk_asp_net_role_claims PRIMARY KEY (id),
    CONSTRAINT fk_asp_net_role_claims_asp_net_roles_role_id FOREIGN KEY (role_id) REFERENCES asp_net_roles (id) ON DELETE CASCADE
);

CREATE TABLE asp_net_user_claims (
    id integer GENERATED BY DEFAULT AS IDENTITY,
    user_id uuid NOT NULL,
    claim_type text,
    claim_value text,
    CONSTRAINT pk_asp_net_user_claims PRIMARY KEY (id),
    CONSTRAINT fk_asp_net_user_claims_asp_net_users_user_id FOREIGN KEY (user_id) REFERENCES asp_net_users (id) ON DELETE CASCADE
);

CREATE TABLE asp_net_user_logins (
    login_provider text NOT NULL,
    provider_key text NOT NULL,
    provider_display_name text,
    user_id uuid NOT NULL,
    CONSTRAINT pk_asp_net_user_logins PRIMARY KEY (login_provider, provider_key),
    CONSTRAINT fk_asp_net_user_logins_asp_net_users_user_id FOREIGN KEY (user_id) REFERENCES asp_net_users (id) ON DELETE CASCADE
);

CREATE TABLE asp_net_user_roles (
    user_id uuid NOT NULL,
    role_id uuid NOT NULL,
    CONSTRAINT pk_asp_net_user_roles PRIMARY KEY (user_id, role_id),
    CONSTRAINT fk_asp_net_user_roles_asp_net_roles_role_id FOREIGN KEY (role_id) REFERENCES asp_net_roles (id) ON DELETE CASCADE,
    CONSTRAINT fk_asp_net_user_roles_asp_net_users_user_id FOREIGN KEY (user_id) REFERENCES asp_net_users (id) ON DELETE CASCADE
);

CREATE TABLE asp_net_user_tokens (
    user_id uuid NOT NULL,
    login_provider text NOT NULL,
    name text NOT NULL,
    value text,
    CONSTRAINT pk_asp_net_user_tokens PRIMARY KEY (user_id, login_provider, name),
    CONSTRAINT fk_asp_net_user_tokens_asp_net_users_user_id FOREIGN KEY (user_id) REFERENCES asp_net_users (id) ON DELETE CASCADE
);

INSERT INTO asp_net_roles (id, concurrency_stamp, name, normalized_name)
VALUES ('6f1d2c3a-0a4b-4c1e-9b7a-1f2e3d4c5b01', 'b3e1a6f2-5c47-4d0e-8a91-0c2f7d6e5a01', 'Operador', 'OPERADOR');
INSERT INTO asp_net_roles (id, concurrency_stamp, name, normalized_name)
VALUES ('6f1d2c3a-0a4b-4c1e-9b7a-1f2e3d4c5b02', 'b3e1a6f2-5c47-4d0e-8a91-0c2f7d6e5a02', 'Administrador', 'ADMINISTRADOR');

CREATE INDEX ix_asp_net_role_claims_role_id ON asp_net_role_claims (role_id);

CREATE UNIQUE INDEX "RoleNameIndex" ON asp_net_roles (normalized_name);

CREATE INDEX ix_asp_net_user_claims_user_id ON asp_net_user_claims (user_id);

CREATE INDEX ix_asp_net_user_logins_user_id ON asp_net_user_logins (user_id);

CREATE INDEX ix_asp_net_user_roles_role_id ON asp_net_user_roles (role_id);

CREATE INDEX "EmailIndex" ON asp_net_users (normalized_email);

CREATE UNIQUE INDEX "UserNameIndex" ON asp_net_users (normalized_user_name);

INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
VALUES ('20261121042059_AgregarIdentidad', '10.0.15');

COMMIT;

START TRANSACTION;
ALTER TABLE eventos_de_acceso ADD validado_por character varying(256);

CREATE TABLE lecturas_pendientes (
    id uuid NOT NULL,
    tipo character varying(10) NOT NULL,
    motivo character varying(30) NOT NULL,
    placa_leida character varying(10) NOT NULL,
    sede_id uuid NOT NULL,
    carril integer NOT NULL,
    confianza integer NOT NULL,
    fecha_hora timestamptz NOT NULL,
    estado character varying(10) NOT NULL,
    placa_validada character varying(10),
    validada_por character varying(256),
    fecha_de_validacion timestamptz,
    resultado text,
    CONSTRAINT pk_lecturas_pendientes PRIMARY KEY (id),
    CONSTRAINT ck_lecturas_pendientes_carril CHECK (carril > 0),
    CONSTRAINT ck_lecturas_pendientes_confianza CHECK (confianza BETWEEN 0 AND 100),
    CONSTRAINT ck_lecturas_pendientes_validacion CHECK (estado = 'Pendiente' OR (placa_validada IS NOT NULL AND validada_por IS NOT NULL AND fecha_de_validacion IS NOT NULL)),
    CONSTRAINT fk_lecturas_pendientes_sedes_sede_id FOREIGN KEY (sede_id) REFERENCES sedes (id) ON DELETE RESTRICT
);

CREATE INDEX ix_lecturas_pendientes_sede_id_estado_fecha_hora ON lecturas_pendientes (sede_id, estado, fecha_hora);

INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
VALUES ('20261122070824_AgregarLecturaManual', '10.0.15');

COMMIT;

START TRANSACTION;
ALTER TABLE cobros ADD fecha_hora_de_ingreso timestamptz NOT NULL DEFAULT TIMESTAMPTZ '-infinity';

ALTER TABLE cobros ADD motivo character varying(20) NOT NULL DEFAULT 'Ingreso';

UPDATE cobros SET fecha_hora_de_ingreso = fecha_hora;
ALTER TABLE cobros ALTER COLUMN fecha_hora_de_ingreso DROP DEFAULT;
ALTER TABLE cobros ALTER COLUMN motivo DROP DEFAULT;

INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
VALUES ('20261124021247_AgregarMotivoDeCobro', '10.0.15');

COMMIT;

START TRANSACTION;
ALTER TABLE pagos ADD transaccion_de_la_pasarela character varying(100);

INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
VALUES ('20261124191607_AgregarPagoConPasarela', '10.0.15');

COMMIT;


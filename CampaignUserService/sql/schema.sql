-- =============================================================================
-- CampaignUserService - manual bootstrap script for Supabase PostgreSQL
--
-- This script is an EXACT mirror of the EF Core model (see
-- src/CampaignUserService.Infrastructure/Persistence/Configurations/*.cs).
-- It exists as a fast, dependency-free way to provision the schema directly
-- from the Supabase SQL Editor, without needing the .NET SDK installed.
--
-- The normal/preferred path is still EF Core migrations:
--   dotnet ef migrations add InitialCreate \
--     --project src/CampaignUserService.Infrastructure \
--     --startup-project src/CampaignUserService.Api
--   dotnet ef database update \
--     --project src/CampaignUserService.Infrastructure \
--     --startup-project src/CampaignUserService.Api
--
-- Running THIS script instead is equivalent for a fresh database and lets
-- the API start immediately (set Database:AutoMigrateAndSeed=false in that
-- case, since there won't be an EF migrations history to apply).
-- =============================================================================

CREATE SCHEMA IF NOT EXISTS campaign_user;

-- gen_random_uuid() lives in pgcrypto; enabled by default on Supabase, but
-- declared here explicitly so the script is self-contained on any Postgres.
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- ----------------------------------------------------------------------------
-- roles
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS campaign_user.roles (
    id              uuid PRIMARY KEY,
    name            varchar(30)   NOT NULL,
    description     varchar(500)  NOT NULL,
    created_at_utc  timestamptz   NOT NULL DEFAULT now(),
    updated_at_utc  timestamptz   NULL,
    deleted_at_utc  timestamptz   NULL,
    is_deleted      boolean       NOT NULL DEFAULT false
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_roles_name ON campaign_user.roles (name);

-- ----------------------------------------------------------------------------
-- users
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS campaign_user.users (
    id                   uuid PRIMARY KEY,
    first_name           varchar(100)  NOT NULL,
    last_name            varchar(100)  NOT NULL,
    email                varchar(256)  NOT NULL,
    password_hash        varchar(500)  NOT NULL,
    phone_number         varchar(20)   NULL,
    cpf                  varchar(11)   NULL,
    photo_url            varchar(2048) NULL,
    birth_date           date          NULL,
    status               varchar(20)   NOT NULL,
    email_confirmed      boolean       NOT NULL DEFAULT false,
    last_login_at_utc    timestamptz   NULL,
    access_failed_count  integer       NOT NULL DEFAULT 0,
    created_at_utc       timestamptz   NOT NULL DEFAULT now(),
    updated_at_utc       timestamptz   NULL,
    deleted_at_utc       timestamptz   NULL,
    is_deleted           boolean       NOT NULL DEFAULT false
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_users_email ON campaign_user.users (email);
CREATE UNIQUE INDEX IF NOT EXISTS ix_users_cpf ON campaign_user.users (cpf) WHERE cpf IS NOT NULL;

-- ----------------------------------------------------------------------------
-- user_roles
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS campaign_user.user_roles (
    id              uuid PRIMARY KEY,
    user_id         uuid NOT NULL REFERENCES campaign_user.users (id) ON DELETE CASCADE,
    role_id         uuid NOT NULL REFERENCES campaign_user.roles (id) ON DELETE RESTRICT,
    created_at_utc  timestamptz NOT NULL DEFAULT now(),
    updated_at_utc  timestamptz NULL,
    deleted_at_utc  timestamptz NULL,
    is_deleted      boolean NOT NULL DEFAULT false
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_user_roles_user_role ON campaign_user.user_roles (user_id, role_id);

-- ----------------------------------------------------------------------------
-- refresh_tokens
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS campaign_user.refresh_tokens (
    id                       uuid PRIMARY KEY,
    user_id                  uuid NOT NULL REFERENCES campaign_user.users (id) ON DELETE CASCADE,
    token_hash               varchar(500) NOT NULL,
    expires_at_utc           timestamptz NOT NULL,
    revoked_at_utc           timestamptz NULL,
    revoked_by_ip            varchar(64) NULL,
    replaced_by_token_hash   varchar(500) NULL,
    created_by_ip            varchar(64) NOT NULL,
    user_agent               varchar(512) NULL,
    created_at_utc           timestamptz NOT NULL DEFAULT now(),
    updated_at_utc           timestamptz NULL,
    deleted_at_utc           timestamptz NULL,
    is_deleted               boolean NOT NULL DEFAULT false
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_refresh_tokens_token_hash ON campaign_user.refresh_tokens (token_hash);
CREATE INDEX IF NOT EXISTS ix_refresh_tokens_user_id ON campaign_user.refresh_tokens (user_id);

-- ----------------------------------------------------------------------------
-- password_reset_tokens
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS campaign_user.password_reset_tokens (
    id              uuid PRIMARY KEY,
    user_id         uuid NOT NULL REFERENCES campaign_user.users (id) ON DELETE CASCADE,
    token_hash      varchar(500) NOT NULL,
    expires_at_utc  timestamptz NOT NULL,
    used_at_utc     timestamptz NULL,
    created_at_utc  timestamptz NOT NULL DEFAULT now(),
    updated_at_utc  timestamptz NULL,
    deleted_at_utc  timestamptz NULL,
    is_deleted      boolean NOT NULL DEFAULT false
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_password_reset_tokens_token_hash ON campaign_user.password_reset_tokens (token_hash);
CREATE INDEX IF NOT EXISTS ix_password_reset_tokens_user_id ON campaign_user.password_reset_tokens (user_id);

-- ----------------------------------------------------------------------------
-- audit_logs
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS campaign_user.audit_logs (
    id               uuid PRIMARY KEY,
    user_id          uuid NULL REFERENCES campaign_user.users (id) ON DELETE SET NULL,
    action           varchar(50) NOT NULL,
    description      varchar(1000) NOT NULL,
    ip_address       varchar(64) NULL,
    user_agent       varchar(512) NULL,
    occurred_at_utc  timestamptz NOT NULL,
    created_at_utc   timestamptz NOT NULL DEFAULT now(),
    updated_at_utc   timestamptz NULL,
    deleted_at_utc   timestamptz NULL,
    is_deleted       boolean NOT NULL DEFAULT false
);

CREATE INDEX IF NOT EXISTS ix_audit_logs_user_id ON campaign_user.audit_logs (user_id);
CREATE INDEX IF NOT EXISTS ix_audit_logs_occurred_at_utc ON campaign_user.audit_logs (occurred_at_utc);

-- ----------------------------------------------------------------------------
-- EF Core migrations history table: deliberately NOT created here.
--
-- EF Core's HistoryRepository always uses columns named "MigrationId" and
-- "ProductVersion" (PascalCase, case-sensitive/quoted) - hand-writing this
-- table with any other column naming (e.g. snake_case, to match the rest of
-- this script) causes MigrateAsync() to fail at startup with
-- 'column "MigrationId" does not exist'. Since there are no real EF Core
-- migrations in this codebase yet, MigrateAsync() first checks whether the
-- history table exists at all and safely no-ops if it doesn't - so simply
-- omitting it here is both correct and the path of least surprise. If real
-- migrations are added later, `dotnet ef database update` will create this
-- table itself with the exact schema EF expects.
-- ----------------------------------------------------------------------------

-- ----------------------------------------------------------------------------
-- Seed: the two system roles. Idempotent (safe to re-run).
-- ----------------------------------------------------------------------------
INSERT INTO campaign_user.roles (id, name, description, created_at_utc)
SELECT gen_random_uuid(), 'Doador', 'Doador: pode se cadastrar, autenticar e gerenciar o próprio perfil.', now()
WHERE NOT EXISTS (SELECT 1 FROM campaign_user.roles WHERE name = 'Doador');

INSERT INTO campaign_user.roles (id, name, description, created_at_utc)
SELECT gen_random_uuid(), 'GestorOng', 'GestorOng: administra usuários, roles e campanhas da organização.', now()
WHERE NOT EXISTS (SELECT 1 FROM campaign_user.roles WHERE name = 'GestorOng');

-- The initial GestorOng administrator account is intentionally NOT created
-- here (it requires a BCrypt password hash, which must be generated by the
-- application). It is created automatically on first API startup from the
-- AdminSeed:Email / AdminSeed:Password configuration - see README.md.

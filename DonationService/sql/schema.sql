-- DonationService PostgreSQL bootstrap script (Supabase-compatible).
--
-- Hand-written to mirror exactly what EF Core's model configuration
-- (DonationDbContext + Configurations/*.cs) describes, since no `dotnet ef`
-- SDK was available to generate migrations in this environment. Run this
-- once against your Supabase Postgres database (SQL Editor, or `psql -f
-- sql/schema.sql "$DONATIONSERVICE_DB_CONNECTION"`) before starting
-- DonationService.Api for the first time.
--
-- Tables:
--   donation_service.donations            - write-side donation request record
--   donation_service.donation_histories   - append-only status-transition audit trail
--   donation_service.donation_events      - business-level log of raised integration events
--   donation_service.inbox_state / outbox_message / outbox_state
--                                          - MassTransit's own Entity Framework Bus Outbox tables
--
-- All donation *documents* used for reads live in MongoDB - this schema
-- covers only what DonationService itself owns transactionally in Postgres.

CREATE SCHEMA IF NOT EXISTS donation_service;

CREATE TABLE IF NOT EXISTS donation_service.donations (
    id               uuid            NOT NULL PRIMARY KEY,
    campaign_id      uuid            NOT NULL,
    user_id          uuid            NOT NULL,
    user_name        varchar(200)    NOT NULL,
    user_email       varchar(320)    NOT NULL,
    value            numeric(18,2)   NOT NULL,
    currency         varchar(10)     NOT NULL,
    payment_method   varchar(20)     NOT NULL,
    donation_date    timestamptz     NOT NULL,
    status           varchar(20)     NOT NULL,
    correlation_id   uuid            NOT NULL,
    event_id         uuid            NOT NULL,
    created_at_utc   timestamptz     NOT NULL DEFAULT now(),
    updated_at_utc   timestamptz     NULL
);

CREATE INDEX IF NOT EXISTS ix_donations_campaign_id ON donation_service.donations (campaign_id);
CREATE INDEX IF NOT EXISTS ix_donations_user_id ON donation_service.donations (user_id);
CREATE UNIQUE INDEX IF NOT EXISTS ux_donations_event_id ON donation_service.donations (event_id);
CREATE INDEX IF NOT EXISTS ix_donations_correlation_id ON donation_service.donations (correlation_id);

CREATE TABLE IF NOT EXISTS donation_service.donation_histories (
    id               uuid            NOT NULL PRIMARY KEY,
    donation_id      uuid            NOT NULL REFERENCES donation_service.donations (id) ON DELETE CASCADE,
    previous_status  varchar(20)     NOT NULL,
    new_status       varchar(20)     NOT NULL,
    description      varchar(500)    NOT NULL,
    occurred_at_utc  timestamptz     NOT NULL,
    created_at_utc   timestamptz     NOT NULL DEFAULT now(),
    updated_at_utc   timestamptz     NULL
);

CREATE INDEX IF NOT EXISTS ix_donation_histories_donation_id ON donation_service.donation_histories (donation_id);

CREATE TABLE IF NOT EXISTS donation_service.donation_events (
    id               uuid            NOT NULL PRIMARY KEY,
    donation_id      uuid            NOT NULL REFERENCES donation_service.donations (id) ON DELETE CASCADE,
    event_id         uuid            NOT NULL,
    correlation_id   uuid            NOT NULL,
    event_type       varchar(300)    NOT NULL,
    payload_json     jsonb           NOT NULL,
    occurred_at_utc  timestamptz     NOT NULL,
    created_at_utc   timestamptz     NOT NULL DEFAULT now(),
    updated_at_utc   timestamptz     NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_donation_events_event_id ON donation_service.donation_events (event_id);
CREATE INDEX IF NOT EXISTS ix_donation_events_donation_id ON donation_service.donation_events (donation_id);

-- MassTransit Entity Framework Bus Outbox tables (AddInboxStateEntity /
-- AddOutboxMessageEntity / AddOutboxStateEntity). Column names/types below
-- follow MassTransit 8.x's default EF Core mapping.

CREATE TABLE IF NOT EXISTS donation_service.inbox_state (
    id                  bigserial       NOT NULL PRIMARY KEY,
    message_id          uuid            NOT NULL,
    consumer_id         uuid            NOT NULL,
    lock_id             uuid            NOT NULL,
    row_version         bytea           NULL,
    received             timestamptz    NOT NULL,
    receive_count       integer         NOT NULL,
    expiration_time     timestamptz     NULL,
    consumed             timestamptz    NULL,
    delivered            timestamptz    NULL,
    last_sequence_number bigint         NULL,
    CONSTRAINT ux_inbox_state_message_consumer UNIQUE (message_id, consumer_id)
);

CREATE TABLE IF NOT EXISTS donation_service.outbox_message (
    sequence_number       bigserial     NOT NULL PRIMARY KEY,
    enqueue_time          timestamptz   NULL,
    sent_time             timestamptz   NOT NULL,
    headers               text          NULL,
    properties             text         NULL,
    inbox_message_id       uuid         NULL,
    inbox_consumer_id       uuid        NULL,
    outbox_id               uuid        NULL,
    message_id              uuid        NOT NULL,
    content_type             varchar(256) NOT NULL,
    message_type              text      NOT NULL,
    body                       text     NOT NULL,
    conversation_id             uuid    NULL,
    correlation_id               uuid   NULL,
    initiator_id                  uuid  NULL,
    request_id                     uuid NULL,
    source_address                  varchar(256) NULL,
    destination_address              varchar(256) NULL,
    response_address                  varchar(256) NULL,
    fault_address                      varchar(256) NULL,
    expiration_time                     timestamptz NULL
);

CREATE INDEX IF NOT EXISTS ix_outbox_message_inbox ON donation_service.outbox_message (inbox_message_id, inbox_consumer_id, sequence_number);
CREATE INDEX IF NOT EXISTS ix_outbox_message_outbox ON donation_service.outbox_message (outbox_id, sequence_number);

CREATE TABLE IF NOT EXISTS donation_service.outbox_state (
    outbox_id            uuid          NOT NULL PRIMARY KEY,
    lock_id               uuid         NOT NULL,
    row_version            bytea       NULL,
    created                 timestamptz NOT NULL,
    delivered                timestamptz NULL,
    last_sequence_number       bigint  NULL
);

-- Convenience view for GestorOng-facing tooling/BI, exposing the write-side
-- audit trail without needing direct table access.
CREATE OR REPLACE VIEW donation_service.v_donation_audit_trail AS
SELECT
    d.id              AS donation_id,
    d.campaign_id,
    d.user_id,
    d.user_name,
    d.value,
    d.currency,
    d.status,
    h.previous_status,
    h.new_status,
    h.description,
    h.occurred_at_utc
FROM donation_service.donations d
JOIN donation_service.donation_histories h ON h.donation_id = d.id
ORDER BY d.id, h.occurred_at_utc;

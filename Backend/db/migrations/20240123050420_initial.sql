-- migrate:up
create table "typing_bundle" (
    "id" bigserial primary key not null,
    "submitted_at" timestamp not null,
    "text" varchar(5000) not null,
    "profile_id" varchar(500) not null,
    "started_typing_at" timestamp not null,
    "finished_typing_at" timestamp not null,
    "client_timezone" varchar(100) not null,
    "client_timezone_offset" smallint not null,
    "events" json not null
);

create index "typing_bundle_profile_id_idx" on "typing_bundle" ("profile_id");
create index "typing_bundle_submitted_at_idx" on "typing_bundle" ("submitted_at");

-- migrate:down
drop table "typing_bundle";

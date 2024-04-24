-- migrate:up
alter table "typing_bundle"
add "is_archived" boolean not null default false;

create index "typing_bundle_is_archived_idx" on "typing_bundle" ("is_archived");

-- migrate:down
alter table "typing_bundle"
drop column "is_archived";

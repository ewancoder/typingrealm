-- migrate:up
create table "text_metadata" (
    "typing_bundle_id" bigserial primary key not null,
    "theme" varchar(500) null,
    "for_training" varchar(10) null
);

-- migrate:down
drop table "text_metadata";

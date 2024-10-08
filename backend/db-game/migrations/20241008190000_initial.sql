-- migrate:up
create table "character" (
    "id" varchar(50) primary key not null,
    "name" varchar(100) not null,
    "profile_id" varchar(500) not null,

    "level" integer not null,
    "experience" bigint not null
);

create index "character_profile_id_idx" on "character" ("profile_id");

-- migrate:down
drop table "character";

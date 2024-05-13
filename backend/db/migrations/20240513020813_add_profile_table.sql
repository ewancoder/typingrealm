-- migrate:up
create table "profile" (
    "profile_id"  varchar(500) not null,
    "goal_characters" integer
);

create unique index "profile_profile_id_idx" on "profile" ("profile_id");

-- migrate:down

drop table "profile";

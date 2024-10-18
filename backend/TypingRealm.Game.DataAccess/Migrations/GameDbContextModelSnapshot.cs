﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using TypingRealm.Game.DataAccess;

#nullable disable

namespace TypingRealm.Game.DataAccess.Migrations
{
    [DbContext(typeof(GameDbContext))]
    partial class GameDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("TypingRealm.Game.DataAccess.Asset", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("id");

                    b.Property<string>("FilePath")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("file_path");

                    b.Property<string>("LocationId")
                        .HasColumnType("character varying(50)")
                        .HasColumnName("location_id");

                    b.Property<long?>("LocationRouteId")
                        .HasColumnType("bigint")
                        .HasColumnName("location_route_id");

                    b.Property<string>("Path")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)")
                        .HasColumnName("path");

                    b.Property<int>("Type")
                        .HasColumnType("integer")
                        .HasColumnName("type");

                    b.HasKey("Id")
                        .HasName("pk_asset");

                    b.HasIndex("LocationId")
                        .HasDatabaseName("ix_asset_location_id");

                    b.HasIndex("LocationRouteId")
                        .HasDatabaseName("ix_asset_location_route_id");

                    b.HasIndex("Path")
                        .HasDatabaseName("ix_asset_path");

                    b.ToTable("asset", (string)null);
                });

            modelBuilder.Entity("TypingRealm.Game.DataAccess.Character", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("id");

                    b.Property<long>("Experience")
                        .HasColumnType("bigint")
                        .HasColumnName("experience");

                    b.Property<int>("Level")
                        .HasColumnType("integer")
                        .HasColumnName("level");

                    b.Property<string>("LocationId")
                        .IsRequired()
                        .HasColumnType("character varying(50)")
                        .HasColumnName("location_id");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("name");

                    b.Property<string>("ProfileId")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)")
                        .HasColumnName("profile_id");

                    b.HasKey("Id")
                        .HasName("pk_character");

                    b.HasIndex("LocationId")
                        .HasDatabaseName("ix_character_location_id");

                    b.HasIndex("ProfileId")
                        .HasDatabaseName("ix_character_profile_id");

                    b.ToTable("character", (string)null);
                });

            modelBuilder.Entity("TypingRealm.Game.DataAccess.Location", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("id");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(10000)
                        .HasColumnType("character varying(10000)")
                        .HasColumnName("description");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("name");

                    b.Property<string>("Path")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)")
                        .HasColumnName("path");

                    b.HasKey("Id")
                        .HasName("pk_location");

                    b.HasIndex("Path")
                        .HasDatabaseName("ix_location_path");

                    b.ToTable("location", (string)null);
                });

            modelBuilder.Entity("TypingRealm.Game.DataAccess.LocationRoute", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(10000)
                        .HasColumnType("character varying(10000)")
                        .HasColumnName("description");

                    b.Property<long>("DistanceMarks")
                        .HasColumnType("bigint")
                        .HasColumnName("distance_marks");

                    b.Property<string>("FromLocationId")
                        .IsRequired()
                        .HasColumnType("character varying(50)")
                        .HasColumnName("from_location_id");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("name");

                    b.Property<string>("ToLocationId")
                        .IsRequired()
                        .HasColumnType("character varying(50)")
                        .HasColumnName("to_location_id");

                    b.HasKey("Id")
                        .HasName("pk_location_route");

                    b.HasIndex("FromLocationId")
                        .HasDatabaseName("ix_location_route_from_location_id");

                    b.HasIndex("ToLocationId")
                        .HasDatabaseName("ix_location_route_to_location_id");

                    b.ToTable("location_route", (string)null);
                });

            modelBuilder.Entity("TypingRealm.Game.DataAccess.Asset", b =>
                {
                    b.HasOne("TypingRealm.Game.DataAccess.Location", null)
                        .WithMany("Assets")
                        .HasForeignKey("LocationId")
                        .HasConstraintName("fk_asset_location_location_id");

                    b.HasOne("TypingRealm.Game.DataAccess.LocationRoute", null)
                        .WithMany("Assets")
                        .HasForeignKey("LocationRouteId")
                        .HasConstraintName("fk_asset_location_route_location_route_id");
                });

            modelBuilder.Entity("TypingRealm.Game.DataAccess.Character", b =>
                {
                    b.HasOne("TypingRealm.Game.DataAccess.Location", "Location")
                        .WithMany()
                        .HasForeignKey("LocationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_character_location_location_id");

                    b.OwnsOne("TypingRealm.Game.DataAccess.MovementProgress", "MovementProgress", b1 =>
                        {
                            b1.Property<string>("CharacterId")
                                .HasColumnType("character varying(50)")
                                .HasColumnName("id");

                            b1.Property<long>("DistanceMarks")
                                .HasColumnType("bigint")
                                .HasColumnName("movement_progress_distance_marks");

                            b1.Property<long>("LocationRouteId")
                                .HasColumnType("bigint")
                                .HasColumnName("movement_progress_location_route_id");

                            b1.HasKey("CharacterId");

                            b1.HasIndex("LocationRouteId")
                                .HasDatabaseName("ix_character_movement_progress_location_route_id");

                            b1.ToTable("character");

                            b1.WithOwner()
                                .HasForeignKey("CharacterId")
                                .HasConstraintName("fk_character_character_id");

                            b1.HasOne("TypingRealm.Game.DataAccess.LocationRoute", "LocationRoute")
                                .WithMany()
                                .HasForeignKey("LocationRouteId")
                                .OnDelete(DeleteBehavior.Cascade)
                                .IsRequired()
                                .HasConstraintName("fk_character_location_route_movement_progress_location_route_id");

                            b1.Navigation("LocationRoute");
                        });

                    b.Navigation("Location");

                    b.Navigation("MovementProgress");
                });

            modelBuilder.Entity("TypingRealm.Game.DataAccess.LocationRoute", b =>
                {
                    b.HasOne("TypingRealm.Game.DataAccess.Location", "FromLocation")
                        .WithMany("Routes")
                        .HasForeignKey("FromLocationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_location_route_location_from_location_id");

                    b.HasOne("TypingRealm.Game.DataAccess.Location", "ToLocation")
                        .WithMany("InverseRoutes")
                        .HasForeignKey("ToLocationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_location_route_location_to_location_id");

                    b.Navigation("FromLocation");

                    b.Navigation("ToLocation");
                });

            modelBuilder.Entity("TypingRealm.Game.DataAccess.Location", b =>
                {
                    b.Navigation("Assets");

                    b.Navigation("InverseRoutes");

                    b.Navigation("Routes");
                });

            modelBuilder.Entity("TypingRealm.Game.DataAccess.LocationRoute", b =>
                {
                    b.Navigation("Assets");
                });
#pragma warning restore 612, 618
        }
    }
}

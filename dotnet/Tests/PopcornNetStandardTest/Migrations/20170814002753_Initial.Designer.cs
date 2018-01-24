using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using PopcornNetStandardTest.Models;

namespace PopcornNetStandardTest.Migrations
{
    [DbContext(typeof(TestModelContext))]
    [Migration("20170814002753_Initial")]
    partial class Initial
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.1.2");

            modelBuilder.Entity("PopcornNetStandardTest.Models.Credential", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<Guid>("DefinitionId");

                    b.Property<Guid>("EnvironmentId");

                    b.HasKey("Id");

                    b.HasIndex("DefinitionId");

                    b.HasIndex("EnvironmentId");

                    b.ToTable("Credentials");
                });

            modelBuilder.Entity("PopcornNetStandardTest.Models.CredentialDefinition", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<Guid>("CredentialTypeId");

                    b.Property<string>("DisplayName");

                    b.Property<string>("Name");

                    b.Property<Guid>("ProjectId");

                    b.HasKey("Id");

                    b.HasIndex("CredentialTypeId");

                    b.HasIndex("ProjectId");

                    b.ToTable("CredentialDefinitions");
                });

            modelBuilder.Entity("PopcornNetStandardTest.Models.CredentialKeyValue", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<Guid>("CredentialId");

                    b.Property<string>("Key");

                    b.Property<string>("Value");

                    b.HasKey("Id");

                    b.HasIndex("CredentialId");

                    b.ToTable("CredentialKeyValues");
                });

            modelBuilder.Entity("PopcornNetStandardTest.Models.CredentialType", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Name");

                    b.Property<string>("RequiredValues");

                    b.HasKey("Id");

                    b.ToTable("CredentialTypes");
                });

            modelBuilder.Entity("PopcornNetStandardTest.Models.Environment", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("AdditionalNotifications");

                    b.Property<string>("BaseUrl");

                    b.Property<bool>("EmailOnError");

                    b.Property<string>("Name");

                    b.Property<Guid>("ProjectId");

                    b.HasKey("Id");

                    b.HasIndex("ProjectId");

                    b.ToTable("Environments");
                });

            modelBuilder.Entity("PopcornNetStandardTest.Models.Project", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Description");

                    b.Property<string>("Name");

                    b.HasKey("Id");

                    b.ToTable("Projects");
                });

            modelBuilder.Entity("PopcornNetStandardTest.Models.Section", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Name");

                    b.Property<Guid>("ProjectId");

                    b.HasKey("Id");

                    b.HasIndex("ProjectId");

                    b.ToTable("Sections");
                });

            modelBuilder.Entity("PopcornNetStandardTest.Models.Credential", b =>
                {
                    b.HasOne("PopcornNetStandardTest.Models.CredentialDefinition", "Definition")
                        .WithMany()
                        .HasForeignKey("DefinitionId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("PopcornNetStandardTest.Models.Environment", "Environment")
                        .WithMany("Credentials")
                        .HasForeignKey("EnvironmentId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("PopcornNetStandardTest.Models.CredentialDefinition", b =>
                {
                    b.HasOne("PopcornNetStandardTest.Models.CredentialType", "Type")
                        .WithMany()
                        .HasForeignKey("CredentialTypeId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("PopcornNetStandardTest.Models.Project", "Project")
                        .WithMany("CredentialDefinitions")
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("PopcornNetStandardTest.Models.CredentialKeyValue", b =>
                {
                    b.HasOne("PopcornNetStandardTest.Models.Credential")
                        .WithMany("Values")
                        .HasForeignKey("CredentialId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("PopcornNetStandardTest.Models.Environment", b =>
                {
                    b.HasOne("PopcornNetStandardTest.Models.Project", "Project")
                        .WithMany("Environments")
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("PopcornNetStandardTest.Models.Section", b =>
                {
                    b.HasOne("PopcornNetStandardTest.Models.Project", "Project")
                        .WithMany("Sections")
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
        }
    }
}

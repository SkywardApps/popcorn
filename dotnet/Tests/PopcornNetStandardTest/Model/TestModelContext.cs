using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace PopcornNetStandardTest.Models
{
    /// <summary>
    /// The database context for onsite dashboard
    /// </summary>
    public class TestModelContext : DbContext
    {
        #region Setup

        public TestModelContext() : base(ConfigureOptions().Options)
        { }

        public TestModelContext(DbContextOptions<TestModelContext> options) : base(options)
        { }
        
        public static DbContextOptionsBuilder<TestModelContext> ConfigureOptions()
        {
            var builder = new DbContextOptionsBuilder<TestModelContext>();
            
            builder.UseSqlite(
                "Data Source=PopcornNetStandardTest.db"
            );

            return builder;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
        #endregion

        #region DbSets
        
        public virtual DbSet<Models.Environment> Environments { get; set; }
        public virtual DbSet<Section> Sections { get; set; }
        
        public virtual DbSet<CredentialType> CredentialTypes { get; set; }
        public virtual DbSet<CredentialDefinition> CredentialDefinitions { get; set; }
        public virtual DbSet<Credential> Credentials { get; set; }
        public virtual DbSet<CredentialKeyValue> CredentialKeyValues { get; set; }
        public virtual DbSet<Project> Projects { get; set; }
        #endregion
    }
}
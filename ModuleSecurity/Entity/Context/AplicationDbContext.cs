using Dapper;
using Entity.Model.Security;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Entity.Context
{
    internal class AplicationDbContext
    {
        protected readonly IConfiguration configuration;
        public AplicationDbContext(DbContextOptions<AplicationDbContext> options, IConfiguration configuration) : base(options)
        {
            configuration = configuration;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            base.OnModelCreating(modelBuilder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.EnableSensitiveDataLogging();
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
        }

        public override int SaveChanges()
        {
            EnsureAudit();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSucces, CancellationToken cancellationToken = default)
        {
            EnsureAudit();
            return base.SaveChangesAsync(acceptAllChangesOnSucces,
                            cancellationToken);
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string text, object parameters = null, int? timeout = null, CommandType? type = null)
        {
            using var command = new DapperEFCoreCommand(this, text, parameters, timeout, type, CancellationToken.None);
            var connection = Database.GetDbConnection();
            return await connection.QueryAsync<T>(command.Definition);
        }

        public async Task<T> QueryFirstOrDefaultAsync<T>(string text, object parameters = null, int? timeout = null, CommandType? type = null)
        {
            using var command = new DapperEFCoreCommand(this, text, parameters, timeout, type, CancellationToken.None);
            var connection = Database.GetDbConnection();
            return await connection.QueryFirstOrDefaultAsync<T>(command.Definition);
        }

        private void EnsureAudit()
        {
            ChangeTracker.DetectChanges();
        }

        //Security
        public DbSet<Model.Security.Module> Modules => Set<Model.Security.Module>();
        public DbSet<Person> Persons => Set<Person>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<RoleView> RoleViews => Set<RoleView>();
        public DbSet<User> Users => Set<User>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<View> Views => Set<View>();


        public readonly struct DapperEFCoreCommand : IDisposable
        {
            public DapperEFCoreCommand(DbContext context, string text, object parameters, int? timeout, CommandType? type, CancellationToken ct)
            {
                var transaction = context.Database.CurrentTransaction?.GetDbTransaction();
                var commandType = type ?? CommandType.Text;
                var commandTimeout = timeout ?? context.Database.GetCommandTimeout() ?? 30;

                Definition = new CommandDefinition(
                    text,
                    parameters,
                    transaction,
                    commandTimeout,
                    commandType,
                    cancellationToken: ct
                    ); ;
            }

            public CommandDefinition Definition { get; }

            public void Dispose() { }
        }
    }
}
    }
}

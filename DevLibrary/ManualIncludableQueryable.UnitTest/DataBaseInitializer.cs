using ManualIncludableQueryable.UnitTest.TestDatabase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManualIncludableQueryable.UnitTest
{
    public static class DataBaseInitializer
    {
        public static void InitializeDatabase(bool isUseInMemoryDb)
        {
            var serviceCollection = new ServiceCollection();

            var configuration = GetConfiguration();

            if (isUseInMemoryDb)
            {
                RegisterDatabaseInMemory(configuration, serviceCollection);
            }
            else
            {
                RegisterDatabase(configuration, serviceCollection);
            }

            var servicePrvider = serviceCollection.BuildServiceProvider();

            InitializeDatabase(servicePrvider);
        }

        public static void InitializeDatabase(IServiceProvider serviceProvider)
        {
            //For now just create new db, no db migration applied

            var dbContextOptions = serviceProvider.GetService<DbContextOptions<MyDbContext>>();

            using (var dbContext = new MyDbContext(dbContextOptions))
            {
                InitializeDatabase(dbContext);
            }
        }

        public static void InitializeDatabase(MyDbContext dbContext)
        {
            if (dbContext.Database.IsRelational())
            {
                var relationalDatabaseCreator = dbContext.GetService<IDatabaseCreator>() as RelationalDatabaseCreator;

                if (!relationalDatabaseCreator.Exists())
                {
                    relationalDatabaseCreator.Create();
                }
            }

            var isNewCreated = dbContext.Database.EnsureCreated();
        }

        public static void RegisterDatabase(IConfiguration configuration, IServiceCollection services)
        {
            var dlDbConnectionString = configuration[AppsettingsKeyConstants.DbSetting.DbConnectionString];
            services.AddDbContext<MyDbContext>(options => options.UseSqlServer(dlDbConnectionString));

        }

        public static void RegisterDatabaseInMemory(IConfiguration configuration, IServiceCollection services)
        {
            var dlDbConnectionString = configuration[AppsettingsKeyConstants.DbSetting.DbConnectionString];
            var dlDbName = dlDbConnectionString.GetDbNameFromConnectionString();

            services.AddDbContext<MyDbContext>(options => options.UseInMemoryDatabase(dlDbName));
        }

        private static IConfiguration GetConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();

            IConfiguration configuration = builder.Build();

            return configuration;
        }
    }
}

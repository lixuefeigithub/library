using ManualIncludableQueryable.UnitTest.TestDatabase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManualIncludableQueryable.UnitTest
{
    public static class UnitTestInitializer
    {
        public static IConfiguration InitConfiguration()
        {
            var config = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json", optional: true)
               .AddEnvironmentVariables()
               .Build();

            return config;
        }

        public static DbContextOptions<MyDbContext> GetDbContextOptions(bool isUseInMemoryDb,
            string dbConnectionString)
        {
            DbContextOptionsBuilder<MyDbContext> optionsBuilder = new DbContextOptionsBuilder<MyDbContext>();

            if (isUseInMemoryDb)
            {
                //Just hard code for api host
                var dbName = "SampleProjectDb";

                optionsBuilder.UseInMemoryDatabase(dbName);
            }
            else
            {
                optionsBuilder.UseSqlServer(dbConnectionString);
            }

            return optionsBuilder.Options;
        }

        public static MyDbContext InitializeDatabase(bool isUseInMemoryDb, string dbConnectionString)
        {
            var dbContextOptions = GetDbContextOptions(isUseInMemoryDb, dbConnectionString);

            var dbContext = new MyDbContext(dbContextOptions);

            DataBaseInitializer.InitializeDatabase(dbContext);

            return dbContext;
        }
    }
}

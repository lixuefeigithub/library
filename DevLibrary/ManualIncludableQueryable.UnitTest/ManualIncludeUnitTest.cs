using LinqLibrary;
using ManualIncludableQueryable.UnitTest.TestDatabase;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace ManualIncludableQueryable.UnitTest
{
    [TestClass]
    public partial class ManualIncludeUnitTest
    {
        private static MyDbContext _dbContext;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            //we don't insert any data, just query, so initialize onetime for all cases should be enough

            var configuration = UnitTestInitializer.InitConfiguration();
            var dbConnectionString = configuration[AppsettingsKeyConstants.DbSetting.DbConnectionString];

            //It does not make sense to test in memory db for manual include
            var dbContext = UnitTestInitializer.InitializeDatabase(false, dbConnectionString);

            //Insert many data for query
            DatabaseSeeder.SeedDateBaseForManualIncludeUnitTest(dbContext);

            _dbContext = dbContext;
        }



        [TestInitialize]
        public void TestInitialize()
        {

        }

        #region include simple test

        [TestMethod]
        public void Test001OneToManyForSingleEntity()
        {
            try
            {
                var product = _dbContext.GetAllQueryableProducts().FirstOrDefault();
                var loadResult = product.LoadNavigationCollection(x => x.ProductPrices, _dbContext);

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                if (product.ProductPrices.Count == 0)
                {
                    Assert.Fail("No Many entities load");
                }

                TwoCollectionCompare(loadResult, product.ProductPrices, x => x.ProductPriceId);

                try
                {
                    _dbContext.AttachRange(product);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                var changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var product = _dbContext.GetAllQueryableProducts(isTracking: true).FirstOrDefault();

                product.LoadNavigationCollection(x => x.ProductPrices, _dbContext, isTracking: true);

                var trackedEntitiesActual = _dbContext.ChangeTracker.Entries().Count();

                var trackedEntitiesExpected = 1 + product.ProductPrices.Count;

                if (trackedEntitiesActual != trackedEntitiesExpected)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        [TestMethod]
        public void Test002OneToManyForMultipleEntities()
        {
            try
            {
                var products = _dbContext.GetAllQueryableProducts()
                    .OrderBy(x => x.ProductId)
                    .Take(100)
                    .ToList();

                var loadResult = products.LoadNavigationCollection(x => x.ProductPrices, _dbContext);

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                var allProductPrices = products.SelectMany(x => x.ProductPrices);

                if (!allProductPrices.Any())
                {
                    Assert.Fail("No Many entities load");
                }

                if (allProductPrices.Any(x => x.Product == null))
                {
                    Assert.Fail("Error");
                }

                TwoCollectionCompare(loadResult, allProductPrices, x => x.ProductPriceId);

                try
                {
                    _dbContext.AttachRange(products);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                var changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var products = _dbContext.GetAllQueryableProducts(isTracking: true)
                    .OrderBy(x => x.ProductId)
                    .Take(100)
                    .ToList();

                products.LoadNavigationCollection(x => x.ProductPrices, _dbContext, isTracking: true);

                var trackedEntitiesActual = _dbContext.ChangeTracker.Entries().Count();

                var trackedEntitiesExpected = products.Count + products.SelectMany(x => x.ProductPrices).Count();

                if (trackedEntitiesActual != trackedEntitiesExpected)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        [TestMethod]
        public void Test003OneToManyForQueryale()
        {
            try
            {
                var productQuery = _dbContext.GetAllQueryableProducts()
                    .OrderBy(x => x.ProductId)
                    .Take(100);

                var products = productQuery
                    .IncludeManually(x => x.ProductPrices, _dbContext)
                    .ToList();

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                var allProductPrices = products.SelectMany(x => x.ProductPrices);

                if (!allProductPrices.Any())
                {
                    Assert.Fail("No Many entities load");
                }

                if (allProductPrices.Any(x => x.Product == null))
                {
                    Assert.Fail("Error");
                }

                try
                {
                    _dbContext.AttachRange(products);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                var changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }

                _dbContext.DetachAllEntities();

                var products2 = productQuery.ToList();

                TwoCollectionCompare(products2, products, x => x.ProductId);

                var products2_ProductPrices_LoadResult = products2.LoadNavigationCollection(x => x.ProductPrices, _dbContext);

                var realAllProductPrices = products2.SelectMany(x => x.ProductPrices);

                TwoCollectionCompare(realAllProductPrices, allProductPrices, x => x.ProductPriceId);
                TwoCollectionCompare(realAllProductPrices, products2_ProductPrices_LoadResult, x => x.ProductPriceId);
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var productQuery = _dbContext.GetAllQueryableProducts(isTracking: true);

                var products = productQuery
                    .OrderBy(x => x.ProductId)
                    .Take(100)
                    .IncludeManually(x => x.ProductPrices, _dbContext)
                    .ToList();

                var trackedEntitiesActual = _dbContext.ChangeTracker.Entries().Count();

                var trackedEntitiesExpected = products.Count + products.SelectMany(x => x.ProductPrices).Count();

                if (trackedEntitiesActual != trackedEntitiesExpected)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        [TestMethod]
        public void Test004OneToManyForQueryaleNullableFk()
        {
            try
            {
                var clientUserQuery = _dbContext.GetAllQueryableClientUsers()
                    .OrderBy(x => x.ClientUserId)
                    .Take(100);

                var clientUsers = clientUserQuery
                    .IncludeManually(x => x.ContactEmails, _dbContext)
                    .ToList();

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                var allContactEmails = clientUsers.SelectMany(x => x.ContactEmails);

                if (!allContactEmails.Any())
                {
                    Assert.Fail("No Many entities load");
                }

                if (allContactEmails.Any(x => x.ClientUser == null))
                {
                    Assert.Fail("Error");
                }

                try
                {
                    _dbContext.AttachRange(clientUsers);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                var changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }

                _dbContext.DetachAllEntities();

                var clientUsers2 = clientUserQuery.ToList();

                TwoCollectionCompare(clientUsers2, clientUsers, x => x.ClientUserId);

                var clientUsers2_ContactEmails_LoadResult = clientUsers2.LoadNavigationCollection(x => x.ContactEmails, _dbContext);

                var realAllContactEmails = clientUsers2.SelectMany(x => x.ContactEmails);

                TwoCollectionCompare(realAllContactEmails, allContactEmails, x => x.ContactEmailId);
                TwoCollectionCompare(realAllContactEmails, clientUsers2_ContactEmails_LoadResult, x => x.ContactEmailId);
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var clientUserQuery = _dbContext.GetAllQueryableClientUsers(isTracking: true);

                var clientUsers = clientUserQuery
                    .OrderBy(x => x.ClientUserId)
                    .Take(100)
                    .IncludeManually(x => x.ContactEmails, _dbContext)
                    .ToList();

                var trackedEntitiesActual = _dbContext.ChangeTracker.Entries().Count();

                var trackedEntitiesExpected = clientUsers.Count + clientUsers.SelectMany(x => x.ContactEmails).Count();

                if (trackedEntitiesActual != trackedEntitiesExpected)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        [TestMethod]
        public void Test005OneToManyForQueryaleNullableFkContactPhoneNumbers()
        {
            try
            {
                var clientUserQuery = _dbContext.GetAllQueryableClientUsers()
                    .OrderBy(x => x.ClientUserId)
                    .Take(100);

                var clientUsers = clientUserQuery
                    .IncludeManually(x => x.ContactPhoneNumbers, _dbContext, isInvokeDistinctInMemory: true)
                    .ToList();

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                var allContactPhoneNumbers = clientUsers.SelectMany(x => x.ContactPhoneNumbers);

                if (!allContactPhoneNumbers.Any())
                {
                    Assert.Fail("No Many entities load");
                }

                if (allContactPhoneNumbers.Any(x => x.ClientUser == null))
                {
                    Assert.Fail("Error");
                }

                try
                {
                    _dbContext.AttachRange(clientUsers);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                var changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }

                _dbContext.DetachAllEntities();

                var clientUsers2 = clientUserQuery.ToList();

                TwoCollectionCompare(clientUsers2, clientUsers, x => x.ClientUserId);

                var clientUsers2_ContactPhoneNumbers_LoadResult = clientUsers2.LoadNavigationCollection(x => x.ContactPhoneNumbers, _dbContext);

                var realAllContactPhoneNumbers = clientUsers2.SelectMany(x => x.ContactPhoneNumbers);

                TwoCollectionCompare(realAllContactPhoneNumbers, allContactPhoneNumbers, x => x.ContactPhoneNumberId);
                TwoCollectionCompare(realAllContactPhoneNumbers, clientUsers2_ContactPhoneNumbers_LoadResult, x => x.ContactPhoneNumberId);
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var clientUserQuery = _dbContext.GetAllQueryableClientUsers(isTracking: true);

                var clientUsers = clientUserQuery
                    .OrderBy(x => x.ClientUserId)
                    .Take(100)
                    .IncludeManually(x => x.ContactPhoneNumbers, _dbContext, isInvokeDistinctInMemory: true)
                    .ToList();

                var trackedEntitiesActual = _dbContext.ChangeTracker.Entries().Count();

                var trackedEntitiesExpected = clientUsers.Count + clientUsers.SelectMany(x => x.ContactPhoneNumbers).Count();

                if (trackedEntitiesActual != trackedEntitiesExpected)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        [TestMethod]
        public void Test006OneToManyForQueryaleFirstOrDefault()
        {
            try
            {
                var productQuery = _dbContext.GetAllQueryableProducts()
                    .OrderBy(x => x.ProductId)
                    .Take(100);

                var products1 = productQuery
                    .IncludeManually(x => x.ProductPrices, _dbContext)
                    .FirstOrDefault();

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                var products2 = productQuery
                    .IncludeManually(x => x.ProductPrices, _dbContext)
                    .FirstOrDefault(x => x.ProductId > products1.ProductId);

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                var allProductPrices = products1.ProductPrices.Concat(products2.ProductPrices);

                if (!allProductPrices.Any())
                {
                    Assert.Fail("No Many entities load");
                }

                if (allProductPrices.Any(x => x.Product == null))
                {
                    Assert.Fail("Error");
                }

                try
                {
                    _dbContext.AttachRange(products1);
                    _dbContext.AttachRange(products2);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                var changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }

                _dbContext.DetachAllEntities();

                var product1_2 = productQuery.FirstOrDefault();
                var product2_2 = productQuery.FirstOrDefault(x => x.ProductId > product1_2.ProductId);

                TwoElementCompare(products1, product1_2, x => x.ProductId);
                TwoElementCompare(products2, product2_2, x => x.ProductId);

                var product1_2_ProductPrices_LoadResult = product1_2.LoadNavigationCollection(x => x.ProductPrices, _dbContext);
                var product2_2_ProductPrices_LoadResult = product2_2.LoadNavigationCollection(x => x.ProductPrices, _dbContext);

                TwoCollectionCompare(product1_2.ProductPrices, product1_2_ProductPrices_LoadResult, x => x.ProductPriceId);
                TwoCollectionCompare(product2_2.ProductPrices, product2_2_ProductPrices_LoadResult, x => x.ProductPriceId);

                var realAllProductPrices = product1_2.ProductPrices.Concat(product2_2.ProductPrices);
                TwoCollectionCompare(realAllProductPrices, allProductPrices, x => x.ProductPriceId);
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var productQuery = _dbContext.GetAllQueryableProducts(isTracking: true)
                    .OrderBy(x => x.ProductId)
                    .Take(100);

                var product1 = productQuery
                    .IncludeManually(x => x.ProductPrices, _dbContext)
                    .FirstOrDefault();

                var product2 = productQuery
                    .IncludeManually(x => x.ProductPrices, _dbContext)
                    .FirstOrDefault(x => x.ProductId > product1.ProductId);

                var trackedEntitiesActual = _dbContext.ChangeTracker.Entries().Count();

                var trackedEntitiesExpected = 2 + product1.ProductPrices.Count() + product2.ProductPrices.Count();

                if (trackedEntitiesActual != trackedEntitiesExpected)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        [TestMethod]
        public void Test007OneToManyForQueryalePaging()
        {
            try
            {
                var productQuery = _dbContext.GetAllQueryableProducts()
                    .OrderBy(x => x.ProductId);

                var products_p1 = productQuery
                    .Page(1, 50)
                    .IncludeManually(x => x.ProductPrices, _dbContext)
                    .ToList();

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                var products_p2 = productQuery
                    .Page(2, 50)
                    .IncludeManually(x => x.ProductPrices, _dbContext)
                    .ToList();

                var allProductPrices1 = products_p1.SelectMany(x => x.ProductPrices);
                var allProductPrices2 = products_p2.SelectMany(x => x.ProductPrices);

                var allProductPrices = allProductPrices1.Concat(allProductPrices2);

                if (!allProductPrices.Any())
                {
                    Assert.Fail("No Many entities load");
                }

                if (allProductPrices.Any(x => x.Product == null))
                {
                    Assert.Fail("Error");
                }

                try
                {
                    _dbContext.AttachRange(products_p1);
                    _dbContext.AttachRange(products_p2);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                var changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }

                _dbContext.DetachAllEntities();

                var products2_p1 = productQuery.Page(1, 50).ToList();
                var products2_p2 = productQuery.Page(2, 50).ToList();

                TwoCollectionCompare(products_p1, products2_p1, x => x.ProductId);
                TwoCollectionCompare(products_p2, products2_p2, x => x.ProductId);

                var products2_p1_ProductPrices_LoadResult = products2_p1.LoadNavigationCollection(x => x.ProductPrices, _dbContext);
                var products2_p2_ProductPrices_LoadResult = products2_p2.LoadNavigationCollection(x => x.ProductPrices, _dbContext);

                TwoCollectionCompare(products2_p1.SelectMany(x => x.ProductPrices), products2_p1_ProductPrices_LoadResult, x => x.ProductPriceId);
                TwoCollectionCompare(products2_p2.SelectMany(x => x.ProductPrices), products2_p2_ProductPrices_LoadResult, x => x.ProductPriceId);

                var realAllProductPrices = products2_p1.SelectMany(x => x.ProductPrices).Concat(products2_p2.SelectMany(x => x.ProductPrices));

                TwoCollectionCompare(realAllProductPrices, allProductPrices, x => x.ProductPriceId);
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var productQuery = _dbContext.GetAllQueryableProducts(isTracking: true)
                    .OrderBy(x => x.ProductId);

                var products_p1 = productQuery
                   .Page(1, 50)
                   .IncludeManually(x => x.ProductPrices, _dbContext)
                   .ToList();

                var products_p2 = productQuery
                    .Page(2, 50)
                    .IncludeManually(x => x.ProductPrices, _dbContext)
                    .ToList();

                var trackedEntitiesActual = _dbContext.ChangeTracker.Entries().Count();

                var trackedEntitiesExpected = products_p1.Count
                  + products_p2.Count
                  + products_p1.SelectMany(x => x.ProductPrices).Count()
                  + products_p2.SelectMany(x => x.ProductPrices).Count();

                if (trackedEntitiesActual != trackedEntitiesExpected)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        [TestMethod]
        public void Test008OneToManyForReplaceQueryable()
        {
            try
            {
                var productQuery = _dbContext.GetAllQueryableProducts();

                var products_p1 = productQuery
                    .IncludeManually(x => x.ProductPrices, _dbContext)
                    .OrderBy(x => x.ProductId)
                    .Page(1, 50)
                    .ToList();

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                var products_p2 = productQuery
                    .IncludeManually(x => x.ProductPrices, _dbContext)
                    .OrderBy(x => x.ProductId)
                    .Page(2, 50)
                    .ToList();

                var allProductPrices1 = products_p1.SelectMany(x => x.ProductPrices);
                var allProductPrices2 = products_p2.SelectMany(x => x.ProductPrices);

                var allProductPrices = allProductPrices1.Concat(allProductPrices2);

                if (!allProductPrices.Any())
                {
                    Assert.Fail("No Many entities load");
                }

                if (allProductPrices.Any(x => x.Product == null))
                {
                    Assert.Fail("Error");
                }

                try
                {
                    _dbContext.AttachRange(products_p1);
                    _dbContext.AttachRange(products_p2);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                var changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }

                _dbContext.DetachAllEntities();

                var products2_p1 = productQuery
                    .OrderBy(x => x.ProductId)
                    .Page(1, 50)
                    .ToList();

                var products2_p2 = productQuery
                    .OrderBy(x => x.ProductId)
                    .Page(2, 50)
                    .ToList();

                TwoCollectionCompare(products_p1, products2_p1, x => x.ProductId);
                TwoCollectionCompare(products_p2, products2_p2, x => x.ProductId);

                var products2_p1_productPrices_LoadResult = products2_p1.LoadNavigationCollection(x => x.ProductPrices, _dbContext);
                var products2_p2_productPrices_LoadResult = products2_p2.LoadNavigationCollection(x => x.ProductPrices, _dbContext);

                TwoCollectionCompare(products2_p1.SelectMany(x => x.ProductPrices), products2_p1_productPrices_LoadResult, x => x.ProductPriceId);
                TwoCollectionCompare(products2_p2.SelectMany(x => x.ProductPrices), products2_p2_productPrices_LoadResult, x => x.ProductPriceId);

                var realAllProductPrices = products2_p1.SelectMany(x => x.ProductPrices).Concat(products2_p2.SelectMany(x => x.ProductPrices));

                TwoCollectionCompare(realAllProductPrices, allProductPrices, x => x.ProductPriceId);
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var productQuery = _dbContext.GetAllQueryableProducts(isTracking: true);

                var products_p1 = productQuery
                    .IncludeManually(x => x.ProductPrices, _dbContext)
                    .OrderBy(x => x.ProductId)
                    .Page(1, 50)
                    .ToList();

                var products_p2 = productQuery
                    .IncludeManually(x => x.ProductPrices, _dbContext)
                    .OrderBy(x => x.ProductId)
                    .Page(2, 50)
                    .ToList();

                var trackedEntitiesActual = _dbContext.ChangeTracker.Entries().Count();

                var trackedEntitiesExpected = products_p1.Count
                    + products_p2.Count
                    + products_p1.SelectMany(x => x.ProductPrices).Count()
                    + products_p2.SelectMany(x => x.ProductPrices).Count();

                if (trackedEntitiesActual != trackedEntitiesExpected)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        [TestMethod]
        public void Test009ManyToOneForSingleEntity()
        {
            try
            {
                var productPrice = _dbContext.GetAllQueryableProductPrices().FirstOrDefault();
                var loadResult = productPrice.LoadNavigation(x => x.Product, _dbContext);

                if (productPrice.Product == null)
                {
                    Assert.Fail("error");
                }

                TwoElementCompare(loadResult, productPrice.Product, x => x.ProductId);

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                try
                {
                    _dbContext.AttachRange(productPrice);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                var changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var productPrice = _dbContext.GetAllQueryableProductPrices().FirstOrDefault();
                productPrice.LoadNavigation(x => x.Product, _dbContext);
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var productPrice = _dbContext.GetAllQueryableProductPrices(isTracking: true).FirstOrDefault();
                productPrice.LoadNavigation(x => x.Product, _dbContext, isTracking: true);

                if (_dbContext.Entry(productPrice.Product).State != EntityState.Unchanged)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        [TestMethod]
        public void Test010ManyToOneForMultipleEntities()
        {
            try
            {
                var productPrices = _dbContext.GetAllQueryableProductPrices()
                    .OrderBy(x => x.ProductPriceId)
                    .Take(1000)
                    .ToList();

                var loadResult = productPrices.LoadNavigation(x => x.Product, _dbContext);

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                if (productPrices.Any(u => u.Product == null))
                {
                    Assert.Fail("Error");
                }

                TwoCollectionCompare(productPrices.Select(x => x.Product).DistinctBy(x => x.ProductId), loadResult, x => x.ProductId);

                try
                {
                    _dbContext.AttachRange(productPrices);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                var changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var productPrices = _dbContext.GetAllQueryableProductPrices(isTracking: true)
                    .OrderBy(x => x.ProductPriceId)
                    .Take(1000)
                    .ToList();

                productPrices.LoadNavigation(x => x.Product, _dbContext, isTracking: true);

                var trackedEntitiesActual = _dbContext.ChangeTracker.Entries().Count();

                var trackedEntitiesExpected = productPrices.Count + productPrices.Select(x => x.Product).DistinctBy(x => x.ProductId).Count();

                if (trackedEntitiesActual != trackedEntitiesExpected)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        [TestMethod]
        public void Test011ManyToOneForQueryable()
        {
            try
            {
                var productPiceQuery = _dbContext.GetAllQueryableProductPrices()
                    .OrderBy(x => x.ProductPriceId)
                    .Take(1000);

                var productPrices = productPiceQuery
                    .IncludeManually(x => x.Product, _dbContext)
                    .ToList();

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                if (productPrices.Any(u => u.Product == null))
                {
                    Assert.Fail("Error");
                }

                var allProducts = productPrices.Select(x => x.Product).DistinctBy(x => x.ProductId);

                try
                {
                    _dbContext.AttachRange(productPrices);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                var changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }

                _dbContext.DetachAllEntities();

                var productPrices2 = productPiceQuery.ToList();
                TwoCollectionCompare(productPrices, productPrices2, x => x.ProductPriceId);

                var productPrices2_Product_LoadResult = productPrices2.LoadNavigation(x => x.Product, _dbContext);
                var allProducts2 = productPrices2.Select(x => x.Product).DistinctBy(x => x.ProductId);
                TwoCollectionCompare(allProducts, allProducts2, x => x.ProductId);
                TwoCollectionCompare(allProducts, productPrices2_Product_LoadResult, x => x.ProductId);
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var productPrices = _dbContext.GetAllQueryableProductPrices(isTracking: true)
                    .OrderBy(x => x.ProductPriceId)
                    .Take(1000)
                    .IncludeManually(x => x.Product, _dbContext)
                    .ToList();

                var trackedEntitiesActual = _dbContext.ChangeTracker.Entries().Count();

                var trackedEntitiesExpected = productPrices.Count + productPrices.Select(x => x.Product).DistinctBy(x => x.ProductId).Count();

                if (trackedEntitiesActual != trackedEntitiesExpected)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        [TestMethod]
        public void Test012ManyToOneForQueryableFirstOrDefault()
        {
            try
            {
                var productPricesQuery = _dbContext.GetAllQueryableProductPrices()
                    .OrderBy(x => x.ProductPriceId)
                    .Take(1000);

                var productPrice1 = productPricesQuery
                    .IncludeManually(x => x.Product, _dbContext)
                    .FirstOrDefault();

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                var productPrice2 = productPricesQuery
                    .IncludeManually(x => x.Product, _dbContext)
                    .FirstOrDefault(x => x.ProductPriceId > productPrice1.ProductId);

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                if (productPrice1.Product == null)
                {
                    Assert.Fail("Error");
                }

                if (productPrice2.Product == null)
                {
                    Assert.Fail("Error");
                }

                try
                {
                    _dbContext.AttachRange(productPrice1);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                var changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }

                _dbContext.DetachAllEntities();

                try
                {
                    _dbContext.AttachRange(productPrice2);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }

                _dbContext.DetachAllEntities();

                var productPrice1_2 = productPricesQuery
                    .FirstOrDefault();

                var productPrice2_2 = productPricesQuery
                    .FirstOrDefault(x => x.ProductPriceId > productPrice1.ProductId);

                TwoElementCompare(productPrice1, productPrice1_2, x => x.ProductPriceId);
                TwoElementCompare(productPrice2, productPrice2_2, x => x.ProductPriceId);

                var productPrice1_2_Product_LoadResult = productPrice1_2.LoadNavigation(x => x.Product, _dbContext);
                var productPrice2_2_Product_LoadResult = productPrice2_2.LoadNavigation(x => x.Product, _dbContext);
                TwoElementCompare(productPrice1.Product, productPrice1_2.Product, x => x.ProductId);
                TwoElementCompare(productPrice2.Product, productPrice2_2.Product, x => x.ProductId);
                TwoElementCompare(productPrice1.Product, productPrice1_2_Product_LoadResult, x => x.ProductId);
                TwoElementCompare(productPrice2.Product, productPrice2_2_Product_LoadResult, x => x.ProductId);
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var productPricesQuery = _dbContext.GetAllQueryableProductPrices(isTracking: true)
                    .OrderBy(x => x.ProductPriceId)
                    .Take(1000);

                var productPrice1 = productPricesQuery
                    .IncludeManually(x => x.Product, _dbContext)
                    .FirstOrDefault();

                var productPrice2 = productPricesQuery
                    .IncludeManually(x => x.Product, _dbContext)
                    .FirstOrDefault(x => x.ProductPriceId > productPrice1.ProductId);

                var trackedEntitiesActual = _dbContext.ChangeTracker.Entries().Count();

                var trackedEntitiesExpected = 2 + (productPrice1.ProductId == productPrice2.ProductId ? 1 : 2);

                if (trackedEntitiesActual != trackedEntitiesExpected)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        [TestMethod]
        public void Test013ManyToOneForQueryablePaging()
        {
            try
            {
                var productPriceQuery = _dbContext.GetAllQueryableProductPrices()
                    .OrderBy(x => x.ProductPriceId);

                var productPrices_p1 = productPriceQuery
                    .Page(1, 100)
                    .IncludeManually(x => x.Product, _dbContext)
                    .ToArray();

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                var productPrices_p2 = productPriceQuery
                    .Page(2, 100)
                    .IncludeManually(x => x.Product, _dbContext)
                    .ToList();

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                if (productPrices_p1.Any(u => u.Product == null))
                {
                    Assert.Fail("Error");
                }

                if (productPrices_p2.Any(u => u.Product == null))
                {
                    Assert.Fail("Error");
                }

                var allProducts = productPrices_p1.Select(x => x.Product).Concat(productPrices_p2.Select(x => x.Product)).DistinctBy(x => x.ProductId);

                try
                {
                    _dbContext.AttachRange(productPrices_p1);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                var changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }

                _dbContext.DetachAllEntities();

                try
                {
                    _dbContext.AttachRange(productPrices_p2);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }

                _dbContext.DetachAllEntities();

                var productPrices_p1_2 = productPriceQuery
                    .Page(1, 100)
                    .ToArray();

                var productPrices_p2_2 = productPriceQuery
                    .Page(2, 100)
                    .ToList();

                TwoCollectionCompare(productPrices_p1, productPrices_p1_2, x => x.ProductPriceId);
                TwoCollectionCompare(productPrices_p2, productPrices_p2_2, x => x.ProductPriceId);

                var productPrices_p1_2_Product_LoadResult = productPrices_p1_2.LoadNavigation(x => x.Product, _dbContext);
                var productPrices_p2_2_Product_LoadResult = productPrices_p2_2.LoadNavigation(x => x.Product, _dbContext);
                var allProducts2 = productPrices_p1_2.Select(x => x.Product).Concat(productPrices_p2_2.Select(x => x.Product)).DistinctBy(x => x.ProductId);
                TwoCollectionCompare(allProducts, allProducts2, x => x.ProductId);
                TwoCollectionCompare(productPrices_p1_2.Select(x => x.Product).DistinctBy(x => x.ProductId), productPrices_p1_2_Product_LoadResult, x => x.ProductId);
                TwoCollectionCompare(productPrices_p2_2.Select(x => x.Product).DistinctBy(x => x.ProductId), productPrices_p2_2_Product_LoadResult, x => x.ProductId);
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var productPriceQuery = _dbContext.GetAllQueryableProductPrices(isTracking: true)
                    .OrderBy(x => x.ProductPriceId);

                var productPrices_p1 = productPriceQuery
                    .Page(1, 100)
                    .IncludeManually(x => x.Product, _dbContext)
                    .ToArray();

                var productPrices_p2 = productPriceQuery
                    .Page(2, 100)
                    .IncludeManually(x => x.Product, _dbContext)
                    .ToList();

                var trackedEntitiesActual = _dbContext.ChangeTracker.Entries().Count();

                var trackedEntitiesExpected = productPrices_p1.Count()
                    + productPrices_p2.Count()
                    + productPrices_p1.Concat(productPrices_p2).Select(x => x.Product).DistinctBy(x => x.ProductId).Count();

                if (trackedEntitiesActual != trackedEntitiesExpected)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        [TestMethod]
        public void Test014ManyToOneReplaceQueryable()
        {
            try
            {
                var productPriceQuery = _dbContext.GetAllQueryableProductPrices();

                var productPrices_p1 = productPriceQuery
                    .IncludeManually(x => x.Product, _dbContext)
                    .OrderBy(x => x.ProductPriceId)
                    .Page(1, 100)
                    .ToArray();

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                var productPrices_p2 = productPriceQuery
                    .IncludeManually(x => x.Product, _dbContext)
                    .OrderBy(x => x.ProductPriceId)
                    .Page(2, 100)
                    .ToArray();

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                if (productPrices_p1.Any(u => u.Product == null))
                {
                    Assert.Fail("Error");
                }

                if (productPrices_p2.Any(u => u.Product == null))
                {
                    Assert.Fail("Error");
                }

                var allProducts = productPrices_p1.Select(x => x.Product).Concat(productPrices_p2.Select(x => x.Product)).DistinctBy(x => x.ProductId);

                try
                {
                    _dbContext.AttachRange(productPrices_p1);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                var changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }

                _dbContext.DetachAllEntities();

                try
                {
                    _dbContext.AttachRange(productPrices_p2);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }

                _dbContext.DetachAllEntities();

                var productPrices_p1_2 = productPriceQuery
                    .OrderBy(x => x.ProductPriceId)
                    .Page(1, 100)
                    .ToArray();

                var productPrices_p2_2 = productPriceQuery
                    .OrderBy(x => x.ProductPriceId)
                    .Page(2, 100)
                    .ToList();

                TwoCollectionCompare(productPrices_p1, productPrices_p1_2, x => x.ProductPriceId);
                TwoCollectionCompare(productPrices_p2, productPrices_p2_2, x => x.ProductPriceId);

                var productPrices_p1_2_Product_LoadResult = productPrices_p1_2.LoadNavigation(x => x.Product, _dbContext);
                var productPrices_p2_2_Product_LoadResult = productPrices_p2_2.LoadNavigation(x => x.Product, _dbContext);
                var allProducts2 = productPrices_p1_2.Select(x => x.Product).Concat(productPrices_p2_2.Select(x => x.Product)).DistinctBy(x => x.ProductId);
                TwoCollectionCompare(allProducts, allProducts2, x => x.ProductId);
                TwoCollectionCompare(productPrices_p1_2.Select(x => x.Product).DistinctBy(x => x.ProductId), productPrices_p1_2_Product_LoadResult, x => x.ProductId);
                TwoCollectionCompare(productPrices_p2_2.Select(x => x.Product).DistinctBy(x => x.ProductId), productPrices_p2_2_Product_LoadResult, x => x.ProductId);
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var productPriceQuery = _dbContext.GetAllQueryableProductPrices(isTracking: true)
                    .OrderBy(x => x.ProductPriceId);

                var productPrices_p1 = productPriceQuery
                    .IncludeManually(x => x.Product, _dbContext)
                    .OrderBy(x => x.ProductPriceId)
                    .Page(1, 100)
                    .ToArray();

                var productPrices_p2 = productPriceQuery
                   .IncludeManually(x => x.Product, _dbContext)
                   .OrderBy(x => x.ProductPriceId)
                   .Page(2, 100)
                   .ToArray();

                var trackedEntitiesActual = _dbContext.ChangeTracker.Entries().Count();

                var trackedEntitiesExpected = productPrices_p1.Count()
                    + productPrices_p2.Count()
                    + productPrices_p1.Concat(productPrices_p2).Select(x => x.Product).DistinctBy(x => x.ProductId).Count();

                if (trackedEntitiesActual != trackedEntitiesExpected)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        [TestMethod]
        public void Test015OneToManyUniqueForSingleEntity()
        {
            try
            {
                var clientUsersQuery = _dbContext.GetAllQueryableClientUsers()
                    .Where(x => x.ClientUserProfile != null)
                    .OrderBy(x => x.ClientUserId)
                    .Take(1000);

                var clientUser = clientUsersQuery
                    .FirstOrDefault();

                var loadResult = clientUser.LoadNavigation(x => x.ClientUserProfile, _dbContext);

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                if (clientUser.ClientUserProfile == null)
                {
                    Assert.Fail("No Many entities load");
                }

                TwoElementCompare(clientUser.ClientUserProfile, loadResult, x => x.ClientUserProfileId);

                if (clientUser.ClientUserProfile.ClientUser == null)
                {
                    Assert.Fail("Error");
                }

                try
                {
                    _dbContext.AttachRange(clientUser);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                var changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var clientUsersQuery = _dbContext.GetAllQueryableClientUsers(isTracking: true)
                    .Where(x => x.ClientUserProfile != null)
                    .OrderBy(x => x.ClientUserId)
                    .Take(1000);

                var clientUser = clientUsersQuery
                    .FirstOrDefault();

                clientUser.LoadNavigation(x => x.ClientUserProfile, _dbContext, isTracking: true);

                var trackedEntitiesActual = _dbContext.ChangeTracker.Entries().Count();

                var trackedEntitiesExpected = 2;

                if (trackedEntitiesActual != trackedEntitiesExpected)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        [TestMethod]
        public void Test016OneToManyUniqueForMultipleEntities()
        {
            try
            {
                var clientUsersQuery = _dbContext.GetAllQueryableClientUsers()
                    .OrderBy(x => x.ClientUserId)
                    .Take(1000);

                var clientUsers = clientUsersQuery
                    .ToList();

                var clientUsers_ClientUserProfile_LoadResult = clientUsers.LoadNavigation(x => x.ClientUserProfile, _dbContext);

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                var allClientUserProfile = clientUsers
                    .Where(x => x.ClientUserProfile != null)
                    .Select(x => x.ClientUserProfile);

                if (!allClientUserProfile.Any())
                {
                    Assert.Fail("No Many entities load");
                }

                if (allClientUserProfile.Any(x => x.ClientUser == null))
                {
                    Assert.Fail("Error");
                }

                TwoCollectionCompare(allClientUserProfile, clientUsers_ClientUserProfile_LoadResult, x => x.ClientUserProfileId);

                try
                {
                    _dbContext.AttachRange(clientUsers);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                var changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var clientUsersQuery = _dbContext.GetAllQueryableClientUsers(isTracking: true)
                    .OrderBy(x => x.ClientUserId)
                    .Take(1000);

                var clientUsers = clientUsersQuery
                    .ToList();

                clientUsers.LoadNavigation(x => x.ClientUserProfile, _dbContext, isTracking: true);

                var trackedEntitiesActual = _dbContext.ChangeTracker.Entries().Count();

                var trackedEntitiesExpected = clientUsers.Count
                    + clientUsers.Where(x => x.ClientUserProfile != null).Select(x => x.ClientUserProfile).Count();

                if (trackedEntitiesActual != trackedEntitiesExpected)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        [TestMethod]
        public void Test017OneToManyUniqueForQueryale()
        {
            try
            {
                var clientUsersQuery = _dbContext.GetAllQueryableClientUsers()
                    .OrderBy(x => x.ClientUserId)
                    .Take(1000);

                var clientUsers = clientUsersQuery
                    .IncludeManually(x => x.ClientUserProfile, _dbContext)
                    .ToList();

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                var allClientUserProfile = clientUsers
                    .Where(x => x.ClientUserProfile != null)
                    .Select(x => x.ClientUserProfile);

                if (!allClientUserProfile.Any())
                {
                    Assert.Fail("No Many entities load");
                }

                if (allClientUserProfile.Any(x => x.ClientUser == null))
                {
                    Assert.Fail("Error");
                }

                try
                {
                    _dbContext.AttachRange(clientUsers);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                var changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }

                _dbContext.DetachAllEntities();

                var clientUsers2 = clientUsersQuery.ToList();

                TwoCollectionCompare(clientUsers, clientUsers2, x => x.ClientUserId);

                var clientUsers2_ClientUserProfile_LoadResult = clientUsers2.LoadNavigation(x => x.ClientUserProfile, _dbContext);

                var realAllClientUserProfile = clientUsers2
                   .Where(x => x.ClientUserProfile != null)
                   .Select(x => x.ClientUserProfile);

                TwoCollectionCompare(allClientUserProfile, realAllClientUserProfile, x => x.ClientUserProfileId);
                TwoCollectionCompare(clientUsers2_ClientUserProfile_LoadResult, realAllClientUserProfile, x => x.ClientUserProfileId);
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var clientUsersQuery = _dbContext.GetAllQueryableClientUsers(isTracking: true)
                    .OrderBy(x => x.ClientUserId)
                    .Take(1000);

                var clientUsers = clientUsersQuery
                    .IncludeManually(x => x.ClientUserProfile, _dbContext)
                    .ToList();

                var trackedEntitiesActual = _dbContext.ChangeTracker.Entries().Count();

                var trackedEntitiesExpected = clientUsers.Count
                    + clientUsers.Where(x => x.ClientUserProfile != null).Select(x => x.ClientUserProfile).Count();

                if (trackedEntitiesActual != trackedEntitiesExpected)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        [TestMethod]
        public void Test018OneToManyUniqueForQueryableFirstOrDefault()
        {
            try
            {
                var clientUsersQuery = _dbContext.GetAllQueryableClientUsers()
                    .Where(x => x.ClientUserProfile != null)
                    .OrderBy(x => x.ClientUserId)
                    .Take(1000);

                var clientUser1 = clientUsersQuery
                    .IncludeManually(x => x.ClientUserProfile, _dbContext)
                    .FirstOrDefault();

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                var clientUser2 = clientUsersQuery
                    .IncludeManually(x => x.ClientUserProfile, _dbContext)
                    .FirstOrDefault(x => x.ClientUserId > clientUser1.ClientUserId);

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                if (clientUser1.ClientUserProfile == null)
                {
                    Assert.Fail("Error");
                }

                if (clientUser2.ClientUserProfile == null)
                {
                    Assert.Fail("Error");
                }

                if (clientUser1.ClientUserProfile.ClientUser == null)
                {
                    Assert.Fail("Error");
                }

                if (clientUser2.ClientUserProfile.ClientUser == null)
                {
                    Assert.Fail("Error");
                }

                try
                {
                    _dbContext.AttachRange(clientUser1);
                    _dbContext.AttachRange(clientUser2);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                var changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }

                _dbContext.DetachAllEntities();

                var clientUser1_2 = clientUsersQuery
                    .FirstOrDefault();

                var clientUser2_2 = clientUsersQuery
                    .FirstOrDefault(x => x.ClientUserId > clientUser1_2.ClientUserId);

                TwoElementCompare(clientUser1, clientUser1_2, x => x.ClientUserId);
                TwoElementCompare(clientUser2, clientUser2_2, x => x.ClientUserId);

                var clientUser1_2_ClientUserProfile_LoadResult = clientUser1_2.LoadNavigation(x => x.ClientUserProfile, _dbContext);
                var clientUser2_2_ClientUserProfile_LoadResult = clientUser2_2.LoadNavigation(x => x.ClientUserProfile, _dbContext);
                TwoElementCompare(clientUser1.ClientUserProfile, clientUser1_2.ClientUserProfile, x => x.ClientUserProfileId);
                TwoElementCompare(clientUser2.ClientUserProfile, clientUser2_2.ClientUserProfile, x => x.ClientUserProfileId);
                TwoElementCompare(clientUser1.ClientUserProfile, clientUser1_2_ClientUserProfile_LoadResult, x => x.ClientUserProfileId);
                TwoElementCompare(clientUser2.ClientUserProfile, clientUser2_2_ClientUserProfile_LoadResult, x => x.ClientUserProfileId);
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var clientUsersQuery = _dbContext.GetAllQueryableClientUsers(isTracking: true)
                   .Where(x => x.ClientUserProfile != null)
                  .OrderBy(x => x.ClientUserId)
                  .Take(1000);

                var clientUser1 = clientUsersQuery
                    .IncludeManually(x => x.ClientUserProfile, _dbContext)
                    .FirstOrDefault();

                var clientUser2 = clientUsersQuery
                    .IncludeManually(x => x.ClientUserProfile, _dbContext)
                    .FirstOrDefault(x => x.ClientUserId > clientUser1.ClientUserId);

                var trackedEntitiesActual = _dbContext.ChangeTracker.Entries().Count();

                var trackedEntitiesExpected = 2 + 2;

                if (trackedEntitiesActual != trackedEntitiesExpected)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        [TestMethod]
        public void Test019OneToManyUniqueForQueryablePaging()
        {
            try
            {
                var clientUsersQuery = _dbContext.GetAllQueryableClientUsers()
                    .OrderBy(x => x.ClientUserId);

                var clientUser_p1 = clientUsersQuery
                    .Page(1, 500)
                    .IncludeManually(x => x.ClientUserProfile, _dbContext)
                    .ToArray();

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                var clientUser_p2 = clientUsersQuery
                    .Page(2, 500)
                    .IncludeManually(x => x.ClientUserProfile, _dbContext)
                    .ToList();

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                var allClientUserProfile_p1 = clientUser_p1
                    .Where(x => x.ClientUserProfile != null)
                    .Select(x => x.ClientUserProfile);

                var allClientUserProfile_p2 = clientUser_p2
                   .Where(x => x.ClientUserProfile != null)
                   .Select(x => x.ClientUserProfile);

                var allClientUserProfile = allClientUserProfile_p1.Concat(allClientUserProfile_p2);

                if (!allClientUserProfile.Any())
                {
                    Assert.Fail("No Many entities load");
                }

                if (allClientUserProfile.Any(x => x.ClientUser == null))
                {
                    Assert.Fail("Error");
                }

                try
                {
                    _dbContext.AttachRange(clientUser_p1);
                    _dbContext.AttachRange(clientUser_p2);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                var changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }

                _dbContext.DetachAllEntities();

                var clientUser_p1_2 = clientUsersQuery
                    .Page(1, 500)
                    .ToArray();

                var clientUser_p2_2 = clientUsersQuery
                    .Page(2, 500)
                    .ToList();

                TwoCollectionCompare(clientUser_p1, clientUser_p1_2, x => x.ClientUserId);
                TwoCollectionCompare(clientUser_p2, clientUser_p2_2, x => x.ClientUserId);

                var clientUser_p1_2_ClientUserProfile_LoadResult = clientUser_p1_2.LoadNavigation(x => x.ClientUserProfile, _dbContext);
                var clientUser_p2_2_ClientUserProfile_LoadResult = clientUser_p2_2.LoadNavigation(x => x.ClientUserProfile, _dbContext);

                var allClientUserProfile_p1_2 = clientUser_p1_2
                   .Where(x => x.ClientUserProfile != null)
                   .Select(x => x.ClientUserProfile);

                var allClientUserProfile_p2_2 = clientUser_p2_2
                   .Where(x => x.ClientUserProfile != null)
                   .Select(x => x.ClientUserProfile);

                var allClientUserProfile_2 = allClientUserProfile_p1_2.Concat(allClientUserProfile_p2_2);

                TwoCollectionCompare(allClientUserProfile_p1, allClientUserProfile_p1_2, x => x.ClientUserProfileId);
                TwoCollectionCompare(allClientUserProfile_p2, allClientUserProfile_p2_2, x => x.ClientUserProfileId);
                TwoCollectionCompare(allClientUserProfile, allClientUserProfile_2, x => x.ClientUserProfileId);
                TwoCollectionCompare(allClientUserProfile_p1_2, clientUser_p1_2_ClientUserProfile_LoadResult, x => x.ClientUserProfileId);
                TwoCollectionCompare(allClientUserProfile_p2_2, clientUser_p2_2_ClientUserProfile_LoadResult, x => x.ClientUserProfileId);
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var clientUsersQuery = _dbContext.GetAllQueryableClientUsers(isTracking: true)
                    .OrderBy(x => x.ClientUserId);

                var clientUser_p1 = clientUsersQuery
                    .Page(1, 500)
                    .IncludeManually(x => x.ClientUserProfile, _dbContext)
                    .ToArray();

                var clientUser_p2 = clientUsersQuery
                    .Page(2, 500)
                    .IncludeManually(x => x.ClientUserProfile, _dbContext)
                    .ToList();

                var trackedEntitiesActual = _dbContext.ChangeTracker.Entries().Count();

                var trackedEntitiesExpected = clientUser_p1.Count()
                    + clientUser_p2.Count()
                    + clientUser_p1.Concat(clientUser_p2).Where(x => x.ClientUserProfile != null).Select(x => x.ClientUserProfile).DistinctBy(x => x.ClientUserProfileId).Count();

                if (trackedEntitiesActual != trackedEntitiesExpected)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        [TestMethod]
        public void Test020OneToManyUniqueReplaceQueryable()
        {
            try
            {
                var clientUsersQuery = _dbContext.GetAllQueryableClientUsers()
                    .OrderBy(x => x.ClientUserId);

                var clientUsers_p1 = clientUsersQuery
                    .IncludeManually(x => x.ClientUserProfile, _dbContext)
                    .Where(x => x.ClientUserProfile != null)
                    .OrderBy(x => x.ClientUserId)
                    .Page(1, 10)
                    .ToArray();

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                var clientUsers_p2 = clientUsersQuery
                    .IncludeManually(x => x.ClientUserProfile, _dbContext)
                    .Where(x => x.ClientUserProfile != null)
                    .OrderBy(x => x.ClientUserId)
                    .Page(2, 10)
                    .ToArray();

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                var allClientUserProfile_p1 = clientUsers_p1
                    .Where(x => x.ClientUserProfile != null)
                    .Select(x => x.ClientUserProfile);

                var allClientUserProfile_p2 = clientUsers_p2
                   .Where(x => x.ClientUserProfile != null)
                   .Select(x => x.ClientUserProfile);

                var allClientUserProfile = allClientUserProfile_p1.Concat(allClientUserProfile_p2);

                if (!allClientUserProfile.Any())
                {
                    Assert.Fail("No Many entities load");
                }

                if (allClientUserProfile.Any(x => x.ClientUser == null))
                {
                    Assert.Fail("Error");
                }

                try
                {
                    _dbContext.AttachRange(clientUsers_p1);
                    _dbContext.AttachRange(clientUsers_p2);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                var changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }

                _dbContext.DetachAllEntities();

                var clientUsers_p1_2 = clientUsersQuery
                    .Where(x => x.ClientUserProfile != null)
                    .OrderBy(x => x.ClientUserId)
                    .Page(1, 10)
                    .ToArray();

                var clientUsers_p2_2 = clientUsersQuery
                    .Where(x => x.ClientUserProfile != null)
                    .OrderBy(x => x.ClientUserId)
                    .Page(2, 10)
                    .ToList();

                TwoCollectionCompare(clientUsers_p1, clientUsers_p1_2, x => x.ClientUserId);
                TwoCollectionCompare(clientUsers_p2, clientUsers_p2_2, x => x.ClientUserId);

                var clientUsers_p1_2_ClientUserProfile_LoadResult = clientUsers_p1_2.LoadNavigation(x => x.ClientUserProfile, _dbContext);
                var clientUsers_p2_2_ClientUserProfile_LoadResult = clientUsers_p2_2.LoadNavigation(x => x.ClientUserProfile, _dbContext);

                var allClientUserProfile_p1_2 = clientUsers_p1_2
                   .Where(x => x.ClientUserProfile != null)
                   .Select(x => x.ClientUserProfile);

                var allClientUserProfile_p2_2 = clientUsers_p2_2
                   .Where(x => x.ClientUserProfile != null)
                   .Select(x => x.ClientUserProfile);

                var allClientUserProfile_2 = allClientUserProfile_p1_2.Concat(allClientUserProfile_p2_2);

                TwoCollectionCompare(allClientUserProfile_p1, allClientUserProfile_p1_2, x => x.ClientUserProfileId);
                TwoCollectionCompare(allClientUserProfile_p2, allClientUserProfile_p2_2, x => x.ClientUserProfileId);
                TwoCollectionCompare(clientUsers_p1_2_ClientUserProfile_LoadResult, allClientUserProfile_p1_2, x => x.ClientUserProfileId);
                TwoCollectionCompare(clientUsers_p2_2_ClientUserProfile_LoadResult, allClientUserProfile_p2_2, x => x.ClientUserProfileId);
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var clientUsersQuery = _dbContext.GetAllQueryableClientUsers(isTracking: true)
                    .OrderBy(x => x.ClientUserId);

                var clientUsers_p1 = clientUsersQuery
                    .IncludeManually(x => x.ClientUserProfile, _dbContext)
                    .Where(x => x.ClientUserProfile != null)
                    .OrderBy(x => x.ClientUserId)
                    .Page(1, 10)
                    .ToArray();

                var clientUsers_p2 = clientUsersQuery
                    .IncludeManually(x => x.ClientUserProfile, _dbContext)
                    .Where(x => x.ClientUserProfile != null)
                    .OrderBy(x => x.ClientUserId)
                    .Page(2, 10)
                    .ToArray();

                var trackedEntitiesActual = _dbContext.ChangeTracker.Entries().Count();

                var trackedEntitiesExpected = clientUsers_p1.Count()
                    + clientUsers_p2.Count()
                    + clientUsers_p1.Concat(clientUsers_p2).Where(x => x.ClientUserProfile != null).Select(x => x.ClientUserProfile).DistinctBy(x => x.ClientUserProfileId).Count();

                if (trackedEntitiesActual != trackedEntitiesExpected)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        [TestMethod]
        public void Test021OneToOneForMultipleEntities()
        {
            try
            {
                var clientUsersQuery = _dbContext.GetAllQueryableClientUsers()
                    .OrderBy(x => x.ClientUserId)
                    .Take(100);

                var clientUsers = clientUsersQuery
                    .ToList();

                var loadResult = clientUsers.LoadNavigation(x => x.IdentityCardBlobStorageItem, _dbContext, isOneToOne: true);

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                if (clientUsers.Any(x => x.IdentityCardBlobStorageItem == null))
                {
                    Assert.Fail("Error");
                }

                var allStorageItems = clientUsers.Select(x => x.IdentityCardBlobStorageItem);

                TwoCollectionCompare(allStorageItems, loadResult, x => x.BlobStorageItemId);

                try
                {
                    _dbContext.AttachRange(clientUsers);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                var changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var clientUsersQuery = _dbContext.GetAllQueryableClientUsers(isTracking: true)
                    .OrderBy(x => x.ClientUserId)
                    .Take(100);

                var clientUsers = clientUsersQuery
                    .ToList();

                clientUsers.LoadNavigation(x => x.IdentityCardBlobStorageItem, _dbContext, isTracking: true, isOneToOne: true);

                var trackedEntitiesActual = _dbContext.ChangeTracker.Entries().Count();

                var trackedEntitiesExpected = clientUsers.Count
                    + clientUsers.Select(x => x.IdentityCardBlobStorageItem).Count();

                if (trackedEntitiesActual != trackedEntitiesExpected)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        [TestMethod]
        public void Test022OneToOneForQueryale()
        {
            try
            {
                var clientUsersQuery = _dbContext.GetAllQueryableClientUsers()
                    .OrderBy(x => x.ClientUserId)
                    .Take(100);

                var clientUsers = clientUsersQuery
                    .IncludeManually(x => x.IdentityCardBlobStorageItem, _dbContext, isOneToOne: true)
                    .ToList();

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                if (clientUsers.Any(x => x.IdentityCardBlobStorageItem == null))
                {
                    Assert.Fail("Error");
                }

                var allStorageItems = clientUsers.Select(x => x.IdentityCardBlobStorageItem);

                try
                {
                    _dbContext.AttachRange(clientUsers);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                var changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }

                _dbContext.DetachAllEntities();

                var clientUsers2 = clientUsersQuery.ToList();

                TwoCollectionCompare(clientUsers, clientUsers2, x => x.ClientUserId);

                var clientUsers2_IdentityCardBlobStorageItem_LoadResult = clientUsers2.LoadNavigation(x => x.IdentityCardBlobStorageItem, _dbContext, isOneToOne: true);

                var realAllStorageItems = clientUsers2.Select(x => x.IdentityCardBlobStorageItem);

                TwoCollectionCompare(allStorageItems, realAllStorageItems, x => x.BlobStorageItemId);
                TwoCollectionCompare(realAllStorageItems, clientUsers2_IdentityCardBlobStorageItem_LoadResult, x => x.BlobStorageItemId);
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var clientUsersQuery = _dbContext.GetAllQueryableClientUsers(isTracking: true)
                    .OrderBy(x => x.ClientUserId)
                    .Take(100);

                var clientUsers = clientUsersQuery
                    .IncludeManually(x => x.IdentityCardBlobStorageItem, _dbContext, isOneToOne: true)
                    .ToList();

                var trackedEntitiesActual = _dbContext.ChangeTracker.Entries().Count();

                var trackedEntitiesExpected = clientUsers.Count
                    + clientUsers.Select(x => x.IdentityCardBlobStorageItem).Count();

                if (trackedEntitiesActual != trackedEntitiesExpected)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        [TestMethod]
        public void Test023OneToOneForQueryableFirstOrDefault()
        {
            try
            {
                var clientUsersQuery = _dbContext.GetAllQueryableClientUsers()
                   .OrderBy(x => x.ClientUserId)
                   .Take(1000);

                var clientUser1 = clientUsersQuery
                    .IncludeManually(x => x.IdentityCardBlobStorageItem, _dbContext, isOneToOne: true)
                    .FirstOrDefault();

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                var clientUser2 = clientUsersQuery
                    .IncludeManually(x => x.IdentityCardBlobStorageItem, _dbContext, isOneToOne: true)
                    .FirstOrDefault(x => x.ClientUserId > clientUser1.ClientUserId);

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                if (clientUser1.IdentityCardBlobStorageItem == null)
                {
                    Assert.Fail("Error");
                }

                if (clientUser2.IdentityCardBlobStorageItem == null)
                {
                    Assert.Fail("Error");
                }

                try
                {
                    _dbContext.AttachRange(clientUser1);
                    _dbContext.AttachRange(clientUser2);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                var changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }

                _dbContext.DetachAllEntities();

                var clientUser1_2 = clientUsersQuery
                    .FirstOrDefault();

                var clientUser2_2 = clientUsersQuery
                    .FirstOrDefault(x => x.ClientUserId > clientUser1_2.ClientUserId);

                TwoElementCompare(clientUser1, clientUser1_2, x => x.ClientUserId);
                TwoElementCompare(clientUser2, clientUser2_2, x => x.ClientUserId);

                var clientUser1_2_IdentityCardBlobStorageItem_Loadresult = clientUser1_2.LoadNavigation(x => x.IdentityCardBlobStorageItem, _dbContext);
                var clientUser2_2_IdentityCardBlobStorageItem_Loadresult = clientUser2_2.LoadNavigation(x => x.IdentityCardBlobStorageItem, _dbContext);
                TwoElementCompare(clientUser1.IdentityCardBlobStorageItem, clientUser1_2.IdentityCardBlobStorageItem, x => x.BlobStorageItemId);
                TwoElementCompare(clientUser2.IdentityCardBlobStorageItem, clientUser2_2.IdentityCardBlobStorageItem, x => x.BlobStorageItemId);
                TwoElementCompare(clientUser1_2.IdentityCardBlobStorageItem, clientUser1_2_IdentityCardBlobStorageItem_Loadresult, x => x.BlobStorageItemId);
                TwoElementCompare(clientUser2_2.IdentityCardBlobStorageItem, clientUser2_2_IdentityCardBlobStorageItem_Loadresult, x => x.BlobStorageItemId);
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var clientUsersQuery = _dbContext.GetAllQueryableClientUsers(isTracking: true)
                   .Where(x => x.ClientUserProfile != null)
                  .OrderBy(x => x.ClientUserId)
                  .Take(1000);

                var clientUser1 = clientUsersQuery
                    .IncludeManually(x => x.ClientUserProfile, _dbContext)
                    .FirstOrDefault();

                var clientUser2 = clientUsersQuery
                    .IncludeManually(x => x.ClientUserProfile, _dbContext)
                    .FirstOrDefault(x => x.ClientUserId > clientUser1.ClientUserId);

                var trackedEntitiesActual = _dbContext.ChangeTracker.Entries().Count();

                var trackedEntitiesExpected = 2 + 2;

                if (trackedEntitiesActual != trackedEntitiesExpected)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        [TestMethod]
        public void Test024OneToManyPerformanceQueryable()
        {
            var productQuery = _dbContext.GetAllQueryableProducts();

            for (int i = 0; i <= 3; i++)
            {
                long resultForManualIncludeQuery = 0;

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                #region manual include query 

                stopwatch.Restart();

                var productsFromManualIncludeQuery = productQuery
                    .OrderBy(x => x.ProductId)
                    .Take(100)
                    .IncludeManually(x => x.ProductPrices, _dbContext)
                    .ToList();

                stopwatch.Stop();
                resultForManualIncludeQuery = stopwatch.ElapsedMilliseconds;

                #endregion

                Console.WriteLine("Result for one to many:");
                Console.WriteLine($"Manual Include query: [] {resultForManualIncludeQuery}");
            }
        }

        [TestMethod]
        public void Test025ManyToOnePerformanceQueryable()
        {
            var productPriceQuery = _dbContext.GetAllQueryableProductPrices();

            for (int i = 0; i <= 3; i++)
            {
                long resultForManualIncludeQuery = 0;

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                #region manual include query 

                stopwatch.Restart();

                var productPricesFromManualIncludeQuery = productPriceQuery
                    .OrderBy(x => x.ProductPriceId)
                    .Take(1000)
                    .IncludeManually(x => x.Product, _dbContext)
                    .ToList();

                stopwatch.Stop();
                resultForManualIncludeQuery = stopwatch.ElapsedMilliseconds;

                #endregion

                Console.WriteLine("Result for Many to one:");
                Console.WriteLine($"Manual Include query: [] {resultForManualIncludeQuery}");
            }
        }

        [TestMethod]
        public void Test026OneToOnePerformanceQueryable()
        {
            var productQuery = _dbContext.GetAllQueryableProducts();

            for (int i = 0; i <= 3; i++)
            {
                long resultForManualIncludeQuery = 0;

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                #region manual include query 

                stopwatch.Restart();

                var productsFromManualIncludeQuery = productQuery
                    .OrderBy(x => x.ProductId)
                    .Take(500)
                    .IncludeManually(x => x.Image, _dbContext, isOneToOne: true)
                    .IncludeManually(x => x.ProductLogo, isOneToOne: true)
                    .IncludeManually(x => x.License, isOneToOne: true)
                    .IncludeManually(x => x.ProductMerchantProvider)
                    .ToList();

                stopwatch.Stop();
                resultForManualIncludeQuery = stopwatch.ElapsedMilliseconds;

                #endregion

                Console.WriteLine("Result for Many to one:");
                Console.WriteLine($"Manual Include query: [] {resultForManualIncludeQuery}");
            }
        }

        [TestMethod]
        [DataRow(true)]
        public void Test027OneToOnePerformanceQueryable2(bool isRunEFIncludeTesting = true)
        {
            var productQuery = _dbContext.GetAllQueryableProducts();

            for (int i = 0; i <= 3; i++)
            {
                long resultForManualIncludeQuery = 0;
                long resultForOriginalInclude = 0;

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                #region original include

                if (isRunEFIncludeTesting)
                {
                    stopwatch.Restart();

                    var productsFromOriginalInclude = productQuery
                        .Include(x => x.ProductLogo)
                        .Include(x => x.Image)
                        .Include(x => x.License)
                        .Include(x => x.ProductMerchantProvider)
                        .Take(500)
                        .ToList();

                    stopwatch.Stop();
                    resultForOriginalInclude = stopwatch.ElapsedMilliseconds;
                }

                #endregion

                #region manual include query 

                stopwatch.Restart();

                var productsFromManualIncludeQuery = productQuery
                    .OrderBy(x => x.ProductId)
                    .Take(500)
                    .IncludeManually(x => x.Image, _dbContext, isOneToOne: true)
                    .IncludeManually(x => x.ProductLogo, isOneToOne: true)
                    .IncludeManually(x => x.License, isOneToOne: true)
                    .IncludeManually(x => x.ProductMerchantProvider)
                    .ToList();

                stopwatch.Stop();
                resultForManualIncludeQuery = stopwatch.ElapsedMilliseconds;

                #endregion

                Console.WriteLine("Result for Many to one:");
                Console.WriteLine($"Manual Include query: [] {resultForManualIncludeQuery}; Auto Include query: [] {resultForOriginalInclude}");
            }
        }

        [TestMethod]
        [DataRow(false)]
        public void Test028OneToManyPerformanceAll(bool isRunEFIncludeTesting = false)
        {
            var productQuery = _dbContext.GetAllQueryableProducts();

            for (int i = 0; i <= 3; i++)
            {
                long resultForOriginalInclude = 0;
                long resultForManualIncludeGeneric = 0;
                long resultForManualIncludeQuery = 0;
                long resultForManualManualInclude = 0;

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                #region original include

                if (isRunEFIncludeTesting)
                {
                    stopwatch.Restart();

                    var productsFromOriginalInclude = productQuery
                        .OrderBy(x => x.ProductId)
                        .Take(100)
                        .Include(x => x.ProductPrices)
                        .ToList();

                    stopwatch.Stop();
                    resultForOriginalInclude = stopwatch.ElapsedMilliseconds;
                }

                #endregion

                #region manual include query 

                stopwatch.Restart();

                var productsFromManualIncludeQuery = productQuery
                    .OrderBy(x => x.ProductId)
                    .Take(100)
                    .IncludeManually(x => x.ProductPrices, _dbContext)
                    .ToList();

                stopwatch.Stop();
                resultForManualIncludeQuery = stopwatch.ElapsedMilliseconds;

                #endregion

                #region manual include generic 

                stopwatch.Restart();

                var productsFromManualIncludeGeneric = productQuery
                    .OrderBy(x => x.ProductId)
                    .Take(100)
                    .ToList();

                productsFromManualIncludeGeneric.LoadNavigationCollection(x => x.ProductPrices, _dbContext);

                stopwatch.Stop();
                resultForManualIncludeGeneric = stopwatch.ElapsedMilliseconds;

                #endregion

                #region manual manual include

                stopwatch.Restart();

                var productsFromManualManualInclude = productQuery
                    .OrderBy(x => x.ProductId)
                    .Take(100)
                    .ToList();
                ManuallyLoadProductProductPrices(productsFromManualManualInclude, false);

                stopwatch.Stop();
                resultForManualManualInclude = stopwatch.ElapsedMilliseconds;

                #endregion

                Console.WriteLine("Result for one to many:");
                Console.WriteLine($"Auto Include: [] {resultForOriginalInclude};\tManual Include query: [] {resultForManualIncludeQuery};\tManual Include generic: [] {resultForManualIncludeGeneric};\tManual Include manual: [] {resultForManualManualInclude};");
            }
        }

        [TestMethod]
        [DataRow(false)]
        public void Test029ManyToOnePerformanceAll(bool isRunEFIncludeTesting = false)
        {
            var productPriceQuery = _dbContext.GetAllQueryableProductPrices();

            for (int i = 0; i <= 3; i++)
            {
                long resultForOriginalInclude = 0;
                long resultForManualIncludeGeneric = 0;
                long resultForManualIncludeQuery = 0;
                long resultForManualManualInclude = 0;

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                #region original include

                if (isRunEFIncludeTesting)
                {
                    stopwatch.Restart();

                    var productPricesFromOriginalInclude = productPriceQuery
                        .OrderBy(x => x.ProductPriceId)
                        .Take(1000)
                        .Include(x => x.Product)
                        .ToList();

                    stopwatch.Stop();
                    resultForOriginalInclude = stopwatch.ElapsedMilliseconds;
                }

                #endregion

                #region manual include query 

                stopwatch.Restart();

                var productPricesFromManualIncludeQuery = productPriceQuery
                    .OrderBy(x => x.ProductPriceId)
                    .Take(1000)
                    .IncludeManually(x => x.Product, _dbContext)
                    .ToList();

                stopwatch.Stop();
                resultForManualIncludeQuery = stopwatch.ElapsedMilliseconds;

                #endregion

                #region manual include generic 

                stopwatch.Restart();

                var productPricesFromManualIncludeGeneric = productPriceQuery
                    .OrderBy(x => x.ProductPriceId)
                    .Take(1000)
                    .ToList();

                productPricesFromManualIncludeGeneric.LoadNavigation(x => x.Product, _dbContext);

                stopwatch.Stop();
                resultForManualIncludeGeneric = stopwatch.ElapsedMilliseconds;

                #endregion

                #region manual manual include

                stopwatch.Restart();

                var productPricesFromManualManualInclude = productPriceQuery
                    .OrderBy(x => x.ProductPriceId)
                    .Take(1000)
                    .ToList();
                ManuallyLoadProductPricesProduct(productPricesFromManualManualInclude, false, false);

                stopwatch.Stop();
                resultForManualManualInclude = stopwatch.ElapsedMilliseconds;

                #endregion

                Console.WriteLine("Result for Many to one:");
                Console.WriteLine($"Auto Include: [] {resultForOriginalInclude};\tManual Include query: [] {resultForManualIncludeQuery};\tManual Include generic: [] {resultForManualIncludeGeneric};\tManual Include manual: [] {resultForManualManualInclude};");
            }
        }

        #endregion

        #region Then include test

        [TestMethod]
        public void Test030ThenIncludeOnceOneToManyOneToMany()
        {
            try
            {
                var clientUserQuery = _dbContext.GetAllQueryableClientUsers()
                    .OrderBy(x => x.ClientUserId)
                    .Take(100);

                var clientUsers = clientUserQuery
                    .IncludeManually(x => x.Orders, _dbContext)
                    .ThenIncludeManually(x => x.OrderProducts)
                    .ToList();

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                var allOrders = clientUsers.SelectMany(x => x.Orders);

                if (!allOrders.Any())
                {
                    Assert.Fail("No Many entities load");
                }

                if (allOrders.Any(x => x.ClientUser == null))
                {
                    Assert.Fail("Error");
                }

                var allOrderProducts = allOrders.SelectMany(x => x.OrderProducts);

                if (!allOrderProducts.Any())
                {
                    Assert.Fail("No Many entities then include load");
                }

                if (allOrderProducts.Any(x => x.Order == null))
                {
                    Assert.Fail("Error");
                }

                try
                {
                    _dbContext.AttachRange(clientUsers);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                var changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }

                _dbContext.DetachAllEntities();

                var clientUsers2 = clientUserQuery.ToList();

                TwoCollectionCompare(clientUsers2, clientUsers, x => x.ClientUserId);

                var clientUsers2_Orders_LoadResult = clientUsers2.LoadNavigationCollection(x => x.Orders, _dbContext);

                var realAllOrders = clientUsers2.SelectMany(x => x.Orders);

                TwoCollectionCompare(realAllOrders, allOrders, x => x.OrderId);
                TwoCollectionCompare(realAllOrders, clientUsers2_Orders_LoadResult, x => x.OrderId);

                var realAllOrders_OrderProducts_LoadResult = realAllOrders.LoadNavigationCollection(x => x.OrderProducts, _dbContext);

                var realAllOrderProducts = realAllOrders.SelectMany(x => x.OrderProducts);

                TwoCollectionCompare(realAllOrderProducts, allOrderProducts, x => x.OrderId, x => x.ProductId);
                TwoCollectionCompare(realAllOrderProducts, realAllOrders_OrderProducts_LoadResult, x => x.OrderId, x => x.ProductId);
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var clientUserQuery = _dbContext.GetAllQueryableClientUsers(isTracking: true);

                var clientUsers = clientUserQuery
                    .OrderBy(x => x.ClientUserId)
                    .Take(100)
                    .IncludeManually(x => x.Orders, _dbContext)
                    .ThenIncludeManually(x => x.OrderProducts)
                    .ToList();

                var trackedEntitiesActual = _dbContext.ChangeTracker.Entries().Count();

                var trackedEntitiesExpected = clientUsers.Count
                    + clientUsers.SelectMany(x => x.Orders).Count()
                    + clientUsers.SelectMany(x => x.Orders).SelectMany(x => x.OrderProducts).Count();

                if (trackedEntitiesActual != trackedEntitiesExpected)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        [TestMethod]
        public void Test031ThenIncludeOnceOneToManyManyToOne()
        {
            try
            {
                var productQuery = _dbContext.GetAllQueryableProducts()
                    .Where(x => x.OrderProducts.Any())
                    .OrderBy(x => x.ProductId)
                    .Take(100);

                var products = productQuery
                    .IncludeManually(x => x.OrderProducts, _dbContext)
                    .ThenIncludeManually(x => x.Order)
                    .ToList();

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                var allOrderProducts = products.SelectMany(x => x.OrderProducts);

                if (!allOrderProducts.Any())
                {
                    Assert.Fail("No Many entities load");
                }

                if (allOrderProducts.Any(x => x.Product == null))
                {
                    Assert.Fail("Error");
                }

                if (allOrderProducts.Any(u => u.Order == null))
                {
                    Assert.Fail("Error");
                }

                var allOrders = allOrderProducts.Select(x => x.Order).DistinctBy(x => x.OrderId);

                try
                {
                    _dbContext.AttachRange(products);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                var changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }

                _dbContext.DetachAllEntities();

                var products2 = productQuery.ToList();

                TwoCollectionCompare(products2, products, x => x.ProductId);

                var products2_OrderProducts_LoadResult = products2.LoadNavigationCollection(x => x.OrderProducts, _dbContext);

                var realAllOrderProducts = products2.SelectMany(x => x.OrderProducts);

                TwoCollectionCompare(realAllOrderProducts, allOrderProducts, x => x.OrderId, x => x.ProductId);
                TwoCollectionCompare(realAllOrderProducts, products2_OrderProducts_LoadResult, x => x.OrderId, x => x.ProductId);

                var realAllOrderProducts_Order_LoadResult = realAllOrderProducts.LoadNavigation(x => x.Order, _dbContext);

                var realAllOrders = realAllOrderProducts.Select(x => x.Order).DistinctBy(x => x.OrderId);

                TwoCollectionCompare(realAllOrders, allOrders, x => x.OrderId);
                TwoCollectionCompare(realAllOrders, realAllOrderProducts_Order_LoadResult, x => x.OrderId);
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var productQuery = _dbContext.GetAllQueryableProducts(isTracking: true)
                    .Where(x => x.OrderProducts.Any());

                var products = productQuery
                    .OrderBy(x => x.ProductId)
                    .Take(100)
                    .IncludeManually(x => x.OrderProducts, _dbContext)
                    .ThenIncludeManually(x => x.Order)
                    .ToList();

                var trackedEntitiesActual = _dbContext.ChangeTracker.Entries().Count();

                var trackedEntitiesExpected = products.Count
                    + products.SelectMany(x => x.OrderProducts).Count()
                    + products.SelectMany(x => x.OrderProducts).Select(x => x.Order).DistinctBy(x => x.OrderId).Count();

                if (trackedEntitiesActual != trackedEntitiesExpected)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        [TestMethod]
        public void Test032ThenIncludeOnceOneToManyOneToManyUnique()
        {
            var clientBusinessIdsWithClientUsersNoProfile = _dbContext.GetAllQueryableClientBusinesses()
                 .Where(x => x.ClientUsers.Any(u => u.ClientUserProfile == null))
                .Select(x => x.ClientBusinessId)
                .OrderBy(x => x)
                .Take(500)
                .ToList();

            var clientBusinessIdsWithClientUsersHasProfile = _dbContext.GetAllQueryableClientBusinesses()
                .Where(x => x.ClientUsers.Any(u => u.ClientUserProfile != null))
                .Select(x => x.ClientBusinessId)
                .OrderBy(x => x)
                .Take(500)
                .ToList();

            var allClientBusinessIds = clientBusinessIdsWithClientUsersNoProfile.Concat(clientBusinessIdsWithClientUsersHasProfile).ToList();

            try
            {
                var clientBusinessQuery = _dbContext.GetAllQueryableClientBusinesses()
                    .Where(x => allClientBusinessIds.Contains(x.ClientBusinessId))
                    .OrderBy(x => x.ClientBusinessId)
                    .Take(1000);

                var clientBusinesses = clientBusinessQuery
                    .IncludeManually(x => x.ClientUsers, _dbContext)
                    .ThenIncludeManually(x => x.ClientUserProfile)
                    .ToList();

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                var allClientUsers = clientBusinesses.SelectMany(x => x.ClientUsers);

                if (!allClientUsers.Any())
                {
                    Assert.Fail("No Many entities load");
                }

                if (allClientUsers.Any(x => x.ClientBusiness == null))
                {
                    Assert.Fail("Error");
                }

                var allClientUserProfiles = allClientUsers.Where(x => x.ClientUserProfile != null).Select(x => x.ClientUserProfile);

                if (!allClientUserProfiles.Any())
                {
                    Assert.Fail("No Many entities then include load");
                }

                if (allClientUserProfiles.Any(x => x.ClientUser == null))
                {
                    Assert.Fail("Error");
                }

                try
                {
                    _dbContext.AttachRange(clientBusinesses);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                var changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }

                _dbContext.DetachAllEntities();

                var clientBusinesses2 = clientBusinessQuery.ToList();

                TwoCollectionCompare(clientBusinesses, clientBusinesses2, x => x.ClientBusinessId);

                var clientBusinesses2_ClientUsers_LoadResult = clientBusinesses2.LoadNavigationCollection(x => x.ClientUsers, _dbContext);

                var allClientUsers2 = clientBusinesses2.SelectMany(x => x.ClientUsers);

                TwoCollectionCompare(allClientUsers, allClientUsers2, x => x.ClientUserId);
                TwoCollectionCompare(allClientUsers2, clientBusinesses2_ClientUsers_LoadResult, x => x.ClientUserId);

                var allClientUsers2_ClientUserProfile_LoadResult = allClientUsers2.LoadNavigation(x => x.ClientUserProfile, _dbContext);

                var allClientUserProfiles2 = allClientUsers2.Where(x => x.ClientUserProfile != null).Select(x => x.ClientUserProfile);

                TwoCollectionCompare(allClientUserProfiles, allClientUserProfiles2, x => x.ClientUserProfileId);
                TwoCollectionCompare(allClientUserProfiles, allClientUsers2_ClientUserProfile_LoadResult, x => x.ClientUserProfileId);
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var clientBusinessQuery = _dbContext.GetAllQueryableClientBusinesses(isTracking: true)
                    .Where(x => allClientBusinessIds.Contains(x.ClientBusinessId))
                    .OrderBy(x => x.ClientBusinessId)
                    .Take(1000);

                var clientBusinesses = clientBusinessQuery
                    .IncludeManually(x => x.ClientUsers, _dbContext)
                    .ThenIncludeManually(x => x.ClientUserProfile)
                    .ToList();

                var trackedEntitiesActual = _dbContext.ChangeTracker.Entries().Count();

                var trackedEntitiesExpected = clientBusinesses.Count
                    + clientBusinesses.SelectMany(x => x.ClientUsers).Count()
                    + clientBusinesses.SelectMany(x => x.ClientUsers).Where(x => x.ClientUserProfile != null).Select(x => x.ClientUserProfile).Count();

                if (trackedEntitiesActual != trackedEntitiesExpected)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        [TestMethod]
        public void Test033ThenIncludeOnceManyToOneOneToMany()
        {
            try
            {
                var clientUserProfilesWithOrderQuery = _dbContext.GetAllQueryableClientUserProfiles()
                    .Where(x => x.ClientUser.Orders.Any())
                    .OrderBy(x => x.ClientUserProfileId)
                    .Take(1000);

                var clientUserProfiles = clientUserProfilesWithOrderQuery
                    .IncludeManually(x => x.ClientUser, _dbContext)
                    .ThenIncludeManually(x => x.Orders)
                    .ToList();

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                if (clientUserProfiles.Any(u => u.ClientUser == null))
                {
                    Assert.Fail("Error");
                }

                var allClientUser = clientUserProfiles.Select(x => x.ClientUser).DistinctBy(x => x.ClientUserId);

                var allOrders = allClientUser.SelectMany(x => x.Orders);

                if (!allOrders.Any())
                {
                    Assert.Fail("No Many entities load");
                }

                if (allOrders.Any(x => x.ClientUser == null))
                {
                    Assert.Fail("Error");
                }

                try
                {
                    _dbContext.AttachRange(clientUserProfiles);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                var changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }

                _dbContext.DetachAllEntities();

                var clientUserProfiles2 = clientUserProfilesWithOrderQuery.ToList();

                TwoCollectionCompare(clientUserProfiles, clientUserProfiles2, x => x.ClientUserProfileId);

                var clientUserProfiles2_ClientUser_LoadResult = clientUserProfiles2.LoadNavigation(x => x.ClientUser, _dbContext);

                var realAllClientUser = clientUserProfiles2.Select(x => x.ClientUser).DistinctBy(x => x.ClientUserId);

                TwoCollectionCompare(realAllClientUser, allClientUser, x => x.ClientUserId);
                TwoCollectionCompare(realAllClientUser, clientUserProfiles2_ClientUser_LoadResult, x => x.ClientUserId);

                var realAllClientUser_Orders_LoadResult = realAllClientUser.ToList().LoadNavigationCollection(x => x.Orders, _dbContext);

                var realAllOrders = realAllClientUser.SelectMany(x => x.Orders);

                TwoCollectionCompare(realAllOrders, allOrders, x => x.OrderId);
                TwoCollectionCompare(realAllOrders, realAllClientUser_Orders_LoadResult, x => x.OrderId);
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var clientUserProfilesWithOrderQuery = _dbContext.GetAllQueryableClientUserProfiles(isTracking: true)
                  .Where(x => x.ClientUser.Orders.Any())
                  .OrderBy(x => x.ClientUserProfileId)
                  .Take(1000);

                var clientUserProfiles = clientUserProfilesWithOrderQuery
                    .IncludeManually(x => x.ClientUser, _dbContext)
                    .ThenIncludeManually(x => x.Orders)
                    .ToList();

                var trackedEntitiesActual = _dbContext.ChangeTracker.Entries().Count();

                var trackedEntitiesExpected = clientUserProfiles.Count
                    + clientUserProfiles.Select(x => x.ClientUser).DistinctBy(x => x.ClientUserId).Count()
                    + clientUserProfiles.Select(x => x.ClientUser).DistinctBy(x => x.ClientUserId).SelectMany(x => x.Orders).Count();

                if (trackedEntitiesActual != trackedEntitiesExpected)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        [TestMethod]
        public void Test034ThenIncludeOnceManyToOneManyToOne()
        {
            try
            {
                var ordersQuery = _dbContext.GetAllQueryableOrders()
                    .Where(x => x.ClientUser.ClientBusinessId != null)
                    .OrderBy(x => x.OrderId)
                    .Take(1000);

                var orders = ordersQuery
                    .IncludeManually(x => x.ClientUser, _dbContext)
                    .ThenIncludeManually(x => x.ClientBusiness)
                    .ToList();

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                if (orders.Any(u => u.ClientUser == null))
                {
                    Assert.Fail("Error");
                }

                var allClientUsers = orders.Select(x => x.ClientUser).DistinctBy(x => x.ClientUserId);

                if (allClientUsers.Any(b => b.ClientBusiness == null))
                {
                    Assert.Fail("Error");
                }

                var allClientBusinesses = allClientUsers.Select(x => x.ClientBusiness).DistinctBy(x => x.ClientBusinessId);

                try
                {
                    _dbContext.AttachRange(orders);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                var changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }

                _dbContext.DetachAllEntities();

                var orders2 = ordersQuery.ToList();

                TwoCollectionCompare(orders, orders2, x => x.OrderId);

                var orders2_ClientUser_LoadResult = orders2.LoadNavigation(x => x.ClientUser, _dbContext);

                var realAllClientUser = orders2.Select(x => x.ClientUser).DistinctBy(x => x.ClientUserId);

                TwoCollectionCompare(realAllClientUser, allClientUsers, x => x.ClientUserId);
                TwoCollectionCompare(realAllClientUser, orders2_ClientUser_LoadResult, x => x.ClientUserId);

                var realAllClientUser_ClientBusiness_LoadResult = realAllClientUser.ToList().LoadNavigation(x => x.ClientBusiness, _dbContext);

                var realAllClientBusiness = realAllClientUser.Select(x => x.ClientBusiness).DistinctBy(x => x.ClientBusinessId);

                TwoCollectionCompare(realAllClientBusiness, allClientBusinesses, x => x.ClientBusinessId);
                TwoCollectionCompare(realAllClientBusiness, realAllClientUser_ClientBusiness_LoadResult, x => x.ClientBusinessId);
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var orders = _dbContext.GetAllQueryableOrders(isTracking: true)
                    .Where(x => x.ClientUser.ClientBusinessId != null)
                    .OrderBy(x => x.OrderId)
                    .Take(1000)
                    .IncludeManually(x => x.ClientUser, _dbContext)
                    .ThenIncludeManually(x => x.ClientBusiness)
                    .ToList();

                var trackedEntitiesActual = _dbContext.ChangeTracker.Entries().Count();

                var trackedEntitiesExpected = orders.Count
                    + orders.Select(x => x.ClientUser).DistinctBy(x => x.ClientUserId).Count()
                    + orders.Select(x => x.ClientUser.ClientBusiness).DistinctBy(x => x.ClientBusinessId).Count();

                if (trackedEntitiesActual != trackedEntitiesExpected)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        [TestMethod]
        public void Test035ThenIncludeOnceManyToOneOneToManyUnique()
        {
            var clientUserIdsNoProfile = _dbContext.GetAllQueryableClientUsers()
                .Where(x => x.ClientUserProfile == null)
                .Select(x => x.ClientUserId)
                .OrderBy(x => x)
                .Take(500)
                .ToList();

            var clientUserIdsWithProfile = _dbContext.GetAllQueryableClientUsers()
               .Where(x => x.ClientUserProfile != null)
               .Select(x => x.ClientUserId)
               .OrderBy(x => x)
               .Take(500)
               .ToList();

            var allClientUserIds = clientUserIdsNoProfile.Concat(clientUserIdsWithProfile).ToList();

            try
            {
                var ordersQuery = _dbContext.GetAllQueryableOrders()
                    .Where(x => allClientUserIds.Contains(x.ClientUserId))
                    .OrderBy(x => x.OrderId)
                    .Take(1000);

                var orders = ordersQuery
                    .IncludeManually(x => x.ClientUser, _dbContext)
                    .ThenIncludeManually(x => x.ClientUserProfile)
                    .ToList();

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                if (orders.Any(u => u.ClientUser == null))
                {
                    Assert.Fail("Error");
                }

                var allClientUsers = orders.Select(x => x.ClientUser).DistinctBy(x => x.ClientUserId);

                var allClientUserProfiles = allClientUsers
                    .Where(x => x.ClientUserProfile != null)
                    .Select(x => x.ClientUserProfile);

                if (!allClientUserProfiles.Any())
                {
                    Assert.Fail("No Many entities load");
                }

                if (allClientUserProfiles.Any(x => x.ClientUser == null))
                {
                    Assert.Fail("Error");
                }

                try
                {
                    _dbContext.AttachRange(orders);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                var changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }

                _dbContext.DetachAllEntities();

                var orders2 = ordersQuery.ToList();

                TwoCollectionCompare(orders, orders2, x => x.OrderId);

                var orders2_ClientUser_LoadResult = orders2.LoadNavigation(x => x.ClientUser, _dbContext);

                var realAllClientUsers = orders2.Select(x => x.ClientUser).DistinctBy(x => x.ClientUserId);

                TwoCollectionCompare(realAllClientUsers, allClientUsers, x => x.ClientUserId);
                TwoCollectionCompare(realAllClientUsers, orders2_ClientUser_LoadResult, x => x.ClientUserId);

                var realAllClientUsers_ClientUserProfile_LoadResult = realAllClientUsers.ToList().LoadNavigation(x => x.ClientUserProfile, _dbContext);

                var clientUserProfiles2 = realAllClientUsers
                    .Where(x => x.ClientUserProfile != null)
                    .Select(x => x.ClientUserProfile);

                TwoCollectionCompare(allClientUserProfiles, clientUserProfiles2, x => x.ClientUserProfileId);
                TwoCollectionCompare(clientUserProfiles2, realAllClientUsers_ClientUserProfile_LoadResult, x => x.ClientUserProfileId);
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var ordersQuery = _dbContext.GetAllQueryableOrders(isTracking: true)
                   .Where(x => allClientUserIds.Contains(x.ClientUserId))
                   .OrderBy(x => x.OrderId)
                   .Take(1000);

                var orders = ordersQuery
                    .IncludeManually(x => x.ClientUser, _dbContext)
                    .ThenIncludeManually(x => x.ClientUserProfile)
                    .ToList();

                var trackedEntitiesActual = _dbContext.ChangeTracker.Entries().Count();

                var trackedEntitiesExpected = orders.Count
                    + orders.Select(x => x.ClientUser).DistinctBy(x => x.ClientUserId).Count()
                    + orders.Select(x => x.ClientUser).DistinctBy(x => x.ClientUserId).Where(x => x.ClientUserProfile != null).Select(x => x.ClientUserProfile).Count();

                if (trackedEntitiesActual != trackedEntitiesExpected)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        [TestMethod]
        public void Test036ThenIncludeTwiceOneToManyOneToManyOneToMany()
        {
            var clientBusinessIdsWithOrders = _dbContext.GetAllQueryableClientBusinesses()
                .Where(x => x.ClientUsers.Any(c => c.Orders.Any()))
                .Select(x => x.ClientBusinessId)
                .OrderBy(x => x)
                .Take(50)
                .ToList();

            var clientBusinessIdsNoOrders = _dbContext.GetAllQueryableClientBusinesses()
                .Where(x => x.ClientUsers.Any(c => !c.Orders.Any()))
                .Select(x => x.ClientBusinessId)
                .OrderBy(x => x)
                .Take(50)
                .ToList();

            var allClientBusinessIds = clientBusinessIdsWithOrders.Concat(clientBusinessIdsNoOrders).ToList();

            try
            {
                var clientBusinessQuery = _dbContext.GetAllQueryableClientBusinesses()
                    .Where(x => allClientBusinessIds.Contains(x.ClientBusinessId))
                    .OrderBy(x => x.ClientBusinessId)
                    .Take(100);

                var clientBusinesses = clientBusinessQuery
                    .IncludeManually(x => x.ClientUsers, _dbContext)
                    .ThenIncludeManually(x => x.Orders)
                    .ThenIncludeManually(x => x.OrderProducts)
                    .ToList();

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                var allClientUsers = clientBusinesses.SelectMany(x => x.ClientUsers);

                if (!allClientUsers.Any())
                {
                    Assert.Fail("No Many entities load");
                }

                if (allClientUsers.Any(x => x.ClientBusiness == null))
                {
                    Assert.Fail("Error");
                }

                var allOrders = allClientUsers.SelectMany(x => x.Orders);

                if (!allOrders.Any())
                {
                    Assert.Fail("No Many entities load");
                }

                if (allOrders.Any(x => x.ClientUser == null))
                {
                    Assert.Fail("Error");
                }

                var allOrderProducts = allOrders.SelectMany(x => x.OrderProducts);

                if (!allOrderProducts.Any())
                {
                    Assert.Fail("No Many entities then include load");
                }

                if (allOrderProducts.Any(x => x.Order == null))
                {
                    Assert.Fail("Error");
                }

                try
                {
                    _dbContext.AttachRange(clientBusinesses);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                var changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }

                _dbContext.DetachAllEntities();

                var clientBusinesses2 = clientBusinessQuery.ToList();

                TwoCollectionCompare(clientBusinesses2, clientBusinesses, x => x.ClientBusinessId);

                var clientBusinesses2_ClientUsers_LoadResult = clientBusinesses2.LoadNavigationCollection(x => x.ClientUsers, _dbContext);

                var realAllClientUsers = clientBusinesses2.SelectMany(x => x.ClientUsers);

                TwoCollectionCompare(allClientUsers, realAllClientUsers, x => x.ClientUserId);
                TwoCollectionCompare(realAllClientUsers, clientBusinesses2_ClientUsers_LoadResult, x => x.ClientUserId);

                var realAllClientUsers_Orders_LoadResult = realAllClientUsers.LoadNavigationCollection(x => x.Orders, _dbContext);

                var realAllOrders = allClientUsers.SelectMany(x => x.Orders);

                TwoCollectionCompare(realAllOrders, allOrders, x => x.OrderId);
                TwoCollectionCompare(realAllOrders, realAllClientUsers_Orders_LoadResult, x => x.OrderId);

                var realAllOrders_OrderProducts_LoadResult = realAllOrders.LoadNavigationCollection(x => x.OrderProducts, _dbContext);

                var realAllOrderProducts = realAllOrders.SelectMany(x => x.OrderProducts);

                TwoCollectionCompare(realAllOrderProducts, allOrderProducts, x => x.OrderId, x => x.ProductId);
                TwoCollectionCompare(realAllOrderProducts, realAllOrders_OrderProducts_LoadResult, x => x.OrderId, x => x.ProductId);
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var clientBusinessQuery = _dbContext.GetAllQueryableClientBusinesses(isTracking: true)
                    .Where(x => allClientBusinessIds.Contains(x.ClientBusinessId))
                    .OrderBy(x => x.ClientBusinessId)
                    .Take(100);

                var clientBusinesses = clientBusinessQuery
                    .IncludeManually(x => x.ClientUsers, _dbContext)
                    .ThenIncludeManually(x => x.Orders)
                    .ThenIncludeManually(x => x.OrderProducts)
                    .ToList();

                var trackedEntitiesActual = _dbContext.ChangeTracker.Entries().Count();

                var trackedEntitiesExpected = clientBusinesses.Count
                    + clientBusinesses.SelectMany(x => x.ClientUsers).Count()
                    + clientBusinesses.SelectMany(x => x.ClientUsers).SelectMany(x => x.Orders).Count()
                    + clientBusinesses.SelectMany(x => x.ClientUsers).SelectMany(x => x.Orders).SelectMany(x => x.OrderProducts).Count();

                if (trackedEntitiesActual != trackedEntitiesExpected)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        [TestMethod]
        public void Test037ThenIncludeTwiceOneToManyOneToManyManyToOne()
        {
            var clientBusinessIdsWithPaymentMethods = _dbContext.GetAllQueryableClientBusinesses()
                .Where(x => x.ClientUsers.Any(c => c.ClientUserPaymentMethods.Any()))
                .Select(x => x.ClientBusinessId)
                .OrderBy(x => x)
                .Take(50)
                .ToList();

            var clientBusinessIdsNoPaymentMethods = _dbContext.GetAllQueryableClientBusinesses()
                .Where(x => x.ClientUsers.Any(c => !c.ClientUserPaymentMethods.Any()))
                .Select(x => x.ClientBusinessId)
                .OrderBy(x => x)
                .Take(50)
                .ToList();

            var allClientBusinessIds = clientBusinessIdsWithPaymentMethods.Concat(clientBusinessIdsNoPaymentMethods).ToList();

            try
            {
                var clientBusinessQuery = _dbContext.GetAllQueryableClientBusinesses()
                    .Where(x => allClientBusinessIds.Contains(x.ClientBusinessId))
                    .OrderBy(x => x.ClientBusinessId)
                    .Take(100);

                var clientBusinesses = clientBusinessQuery
                    .IncludeManually(x => x.ClientUsers, _dbContext)
                    .ThenIncludeManually(x => x.ClientUserPaymentMethods)
                    .ThenIncludeManually(x => x.PaymentMethod, isOneToOne: true)
                    .ToList();

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                var allClientUsers = clientBusinesses.SelectMany(x => x.ClientUsers);

                if (!allClientUsers.Any())
                {
                    Assert.Fail("No Many entities load");
                }

                if (allClientUsers.Any(x => x.ClientBusiness == null))
                {
                    Assert.Fail("Error");
                }

                var allClientUserPaymentMethods = allClientUsers.SelectMany(x => x.ClientUserPaymentMethods);

                if (!allClientUserPaymentMethods.Any())
                {
                    Assert.Fail("No Many entities then include load");
                }

                if (allClientUserPaymentMethods.Any(x => x.ClientUser == null))
                {
                    Assert.Fail("Error");
                }

                if (allClientUserPaymentMethods.Any(x => x.PaymentMethod == null))
                {
                    Assert.Fail("Error");
                }

                var allPaymentMethods = allClientUserPaymentMethods.Select(x => x.PaymentMethod);

                try
                {
                    _dbContext.AttachRange(clientBusinesses);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                var changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }

                _dbContext.DetachAllEntities();

                var clientBusinesses2 = clientBusinessQuery.ToList();

                TwoCollectionCompare(clientBusinesses2, clientBusinesses, x => x.ClientBusinessId);

                var clientBusinesses2_ClientUsers_LoadResult = clientBusinesses2.LoadNavigationCollection(x => x.ClientUsers, _dbContext);

                var realAllClientUsers = clientBusinesses2.SelectMany(x => x.ClientUsers);

                TwoCollectionCompare(realAllClientUsers, allClientUsers, x => x.ClientUserId);
                TwoCollectionCompare(realAllClientUsers, clientBusinesses2_ClientUsers_LoadResult, x => x.ClientUserId);

                var realAllClientUsers_ClientUserPaymentMethods_LoadResult = realAllClientUsers.LoadNavigationCollection(x => x.ClientUserPaymentMethods, _dbContext);

                var realAllClientUserPaymentMethods = realAllClientUsers.SelectMany(x => x.ClientUserPaymentMethods);

                TwoCollectionCompare(realAllClientUserPaymentMethods, allClientUserPaymentMethods, x => x.ClientUserPaymentMethodId);
                TwoCollectionCompare(realAllClientUserPaymentMethods, realAllClientUsers_ClientUserPaymentMethods_LoadResult, x => x.ClientUserPaymentMethodId);

                var realAllPaymentMethods_PaymentMethod_LoadResult = realAllClientUserPaymentMethods.LoadNavigation(x => x.PaymentMethod, _dbContext);

                var realAllPaymentMethods = realAllClientUserPaymentMethods.Select(x => x.PaymentMethod);

                TwoCollectionCompare(realAllPaymentMethods, allPaymentMethods, x => x.PaymentMethodId);
                TwoCollectionCompare(realAllPaymentMethods, realAllPaymentMethods_PaymentMethod_LoadResult, x => x.PaymentMethodId);
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var clientBusinessQuery = _dbContext.GetAllQueryableClientBusinesses(isTracking: true)
                    .Where(x => allClientBusinessIds.Contains(x.ClientBusinessId))
                    .OrderBy(x => x.ClientBusinessId)
                    .Take(100);

                var clientBusinesses = clientBusinessQuery
                    .IncludeManually(x => x.ClientUsers, _dbContext)
                    .ThenIncludeManually(x => x.ClientUserPaymentMethods)
                    .ThenIncludeManually(x => x.PaymentMethod)
                    .ToList();

                var trackedEntitiesActual = _dbContext.ChangeTracker.Entries().Count();

                var trackedEntitiesExpected = clientBusinesses.Count
                    + clientBusinesses.SelectMany(x => x.ClientUsers).Count()
                    + clientBusinesses.SelectMany(x => x.ClientUsers).SelectMany(x => x.ClientUserPaymentMethods).Count()
                    + clientBusinesses.SelectMany(x => x.ClientUsers).SelectMany(x => x.ClientUserPaymentMethods).Select(x => x.PaymentMethod).Count();

                if (trackedEntitiesActual != trackedEntitiesExpected)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        [TestMethod]
        public void Test038ThenIncludeTwiceOneToManyOneToManyManyToOne2()
        {
            try
            {
                var clientUserQuery = _dbContext.GetAllQueryableClientUsers()
                    .OrderBy(x => x.ClientUserId)
                    .Take(100);

                var clientUsers = clientUserQuery
                    .IncludeManually(x => x.Orders, _dbContext)
                    .ThenIncludeManually(x => x.OrderProducts)
                    .ThenIncludeManually(x => x.Product)
                    .ToList();

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                var allOrders = clientUsers.SelectMany(x => x.Orders);

                if (!allOrders.Any())
                {
                    Assert.Fail("No Many entities load");
                }

                if (allOrders.Any(x => x.ClientUser == null))
                {
                    Assert.Fail("Error");
                }

                var allOrderProducts = allOrders.SelectMany(x => x.OrderProducts);

                if (!allOrderProducts.Any())
                {
                    Assert.Fail("No Many entities then include load");
                }

                if (allOrderProducts.Any(x => x.Order == null))
                {
                    Assert.Fail("Error");
                }

                if (allOrderProducts.Any(x => x.Product == null))
                {
                    Assert.Fail("Error");
                }

                var allProducts = allOrderProducts.Select(x => x.Product).DistinctBy(x => x.ProductId);

                try
                {
                    _dbContext.AttachRange(clientUsers);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                var changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }

                _dbContext.DetachAllEntities();

                var clientUsers2 = clientUserQuery.ToList();

                TwoCollectionCompare(clientUsers2, clientUsers, x => x.ClientUserId);

                var clientUsers2_Orders_LoadResult = clientUsers2.LoadNavigationCollection(x => x.Orders, _dbContext);

                var realAllOrders = clientUsers2.SelectMany(x => x.Orders);

                TwoCollectionCompare(realAllOrders, allOrders, x => x.OrderId);
                TwoCollectionCompare(realAllOrders, clientUsers2_Orders_LoadResult, x => x.OrderId);

                var realAllOrders_OrderProducts_LoadResult = realAllOrders.LoadNavigationCollection(x => x.OrderProducts, _dbContext);

                var realAllOrderProducts = realAllOrders.SelectMany(x => x.OrderProducts);

                TwoCollectionCompare(realAllOrderProducts, allOrderProducts, x => x.OrderId, x => x.ProductId);
                TwoCollectionCompare(realAllOrderProducts, realAllOrders_OrderProducts_LoadResult, x => x.OrderId, x => x.ProductId);

                var realAllOrderProducts_Product_LoadResult = realAllOrderProducts.LoadNavigation(x => x.Product, _dbContext);

                var realAllProducts = realAllOrderProducts.Select(x => x.Product).DistinctBy(x => x.ProductId);

                TwoCollectionCompare(realAllProducts, allProducts, x => x.ProductId);
                TwoCollectionCompare(realAllProducts, realAllOrderProducts_Product_LoadResult, x => x.ProductId);
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var clientUserQuery = _dbContext.GetAllQueryableClientUsers(isTracking: true)
                    .OrderBy(x => x.ClientUserId)
                    .Take(100);

                var clientUsers = clientUserQuery
                    .IncludeManually(x => x.Orders, _dbContext)
                    .ThenIncludeManually(x => x.OrderProducts)
                    .ThenIncludeManually(x => x.Product)
                    .ToList();

                var trackedEntitiesActual = _dbContext.ChangeTracker.Entries().Count();

                var trackedEntitiesExpected = clientUsers.Count
                    + clientUsers.SelectMany(x => x.Orders).Count()
                    + clientUsers.SelectMany(x => x.Orders).SelectMany(x => x.OrderProducts).Count()
                    + clientUsers.SelectMany(x => x.Orders).SelectMany(x => x.OrderProducts).Select(x => x.Product).DistinctBy(x => x.ProductId).Count();

                if (trackedEntitiesActual != trackedEntitiesExpected)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        [TestMethod]
        public void Test039ThenIncludeTwiceOneToManyManyToOneOneToMany()
        {
            try
            {
                var orderQuery = _dbContext.GetAllQueryableOrders()
                    .OrderBy(x => x.OrderId)
                    .Take(100);

                var orders = orderQuery
                    .IncludeManually(x => x.OrderProducts, _dbContext)
                    .ThenIncludeManually(x => x.Product)
                    .ThenIncludeManually(x => x.ProductPrices)
                    .ToList();

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                var allOrderProducts = orders.SelectMany(x => x.OrderProducts);

                if (!allOrderProducts.Any())
                {
                    Assert.Fail("No Many entities load");
                }

                if (allOrderProducts.Any(x => x.Order == null))
                {
                    Assert.Fail("Error");
                }

                if (allOrderProducts.Any(u => u.Product == null))
                {
                    Assert.Fail("Error");
                }

                var allProducts = allOrderProducts.Select(x => x.Product).DistinctBy(x => x.ProductId);

                var allProductPrices = allProducts.SelectMany(x => x.ProductPrices);

                if (!allProductPrices.Any())
                {
                    Assert.Fail("No Many entities load");
                }

                if (allProductPrices.Any(x => x.Product == null))
                {
                    Assert.Fail("Error");
                }

                try
                {
                    _dbContext.AttachRange(orders);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                var changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }

                _dbContext.DetachAllEntities();

                var orders2 = orderQuery.ToList();

                TwoCollectionCompare(orders2, orders, x => x.OrderId);

                var orders2_OrderProducts_LoadResult = orders2.LoadNavigationCollection(x => x.OrderProducts, _dbContext);

                var realAllOrderProducts = orders2.SelectMany(x => x.OrderProducts);

                TwoCollectionCompare(realAllOrderProducts, allOrderProducts, x => x.OrderId, x => x.ProductId);
                TwoCollectionCompare(realAllOrderProducts, orders2_OrderProducts_LoadResult, x => x.OrderId, x => x.ProductId);

                var realAllOrderProducts_Product_LoadResult = realAllOrderProducts.LoadNavigation(x => x.Product, _dbContext);

                var realAllProducts = realAllOrderProducts.Select(x => x.Product).DistinctBy(x => x.ProductId);

                TwoCollectionCompare(realAllProducts, allProducts, x => x.ProductId);
                TwoCollectionCompare(realAllProducts, realAllOrderProducts_Product_LoadResult, x => x.ProductId);

                var realAllProducts_ProductPrices_LoadResult = realAllProducts.LoadNavigationCollection(x => x.ProductPrices, _dbContext);

                var realAllProductPrices = realAllProducts.SelectMany(x => x.ProductPrices);

                TwoCollectionCompare(realAllProductPrices, allProductPrices, x => x.ProductPriceId);
                TwoCollectionCompare(realAllProductPrices, realAllProducts_ProductPrices_LoadResult, x => x.ProductPriceId);
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var orderQuery = _dbContext.GetAllQueryableOrders(isTracking: true)
                    .OrderBy(x => x.OrderId)
                    .Take(100);

                var orders = orderQuery
                    .IncludeManually(x => x.OrderProducts, _dbContext)
                    .ThenIncludeManually(x => x.Product)
                    .ThenIncludeManually(x => x.ProductPrices)
                    .ToList();

                var trackedEntitiesActual = _dbContext.ChangeTracker.Entries().Count();

                var trackedEntitiesExpected = orders.Count
                    + orders.SelectMany(x => x.OrderProducts).Count()
                    + orders.SelectMany(x => x.OrderProducts).Select(x => x.Product).DistinctBy(x => x.ProductId).Count()
                    + orders.SelectMany(x => x.OrderProducts).Select(x => x.Product).DistinctBy(x => x.ProductId).SelectMany(x => x.ProductPrices).Count();

                if (trackedEntitiesActual != trackedEntitiesExpected)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        [TestMethod]
        public void Test040ThenIncludeTwiceOneToManyManyToOneManyToOne()
        {
            var paymentMethodWithClientBusinessId = _dbContext.GetAllQueryablePaymentMethod()
                .Where(x => x.ClientUserPaymentMethods.Any(cp => cp.ClientUser.ClientBusinessId != null))
                .Select(x => x.PaymentMethodId)
                .OrderBy(x => x)
                .Take(500)
                .ToList();

            var paymentMethodNoClientBusinessId = _dbContext.GetAllQueryablePaymentMethod()
                .Where(x => x.ClientUserPaymentMethods.Any(cp => cp.ClientUser.ClientBusinessId == null))
                .Select(x => x.PaymentMethodId)
                .OrderBy(x => x)
                .Take(500)
                .ToList();

            var allPaymentMethodIds = paymentMethodWithClientBusinessId.Concat(paymentMethodNoClientBusinessId).Distinct().ToList();

            try
            {
                var paymentMethodQuery = _dbContext.GetAllQueryablePaymentMethod()
                    .Where(x => allPaymentMethodIds.Contains(x.PaymentMethodId))
                    .OrderBy(x => x.PaymentMethodId)
                    .Take(1000);

                var paymentMethods = paymentMethodQuery
                    .IncludeManually(x => x.ClientUserPaymentMethods, _dbContext)
                    .ThenIncludeManually(x => x.ClientUser)
                    .ThenIncludeManually(x => x.ClientBusiness)
                    .ToList();

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                var allClientUserPaymentMethods = paymentMethods.SelectMany(x => x.ClientUserPaymentMethods);

                if (!allClientUserPaymentMethods.Any())
                {
                    Assert.Fail("No Many entities load");
                }

                if (allClientUserPaymentMethods.Any(u => u.PaymentMethod == null))
                {
                    Assert.Fail("Error");
                }

                if (allClientUserPaymentMethods.Any(x => x.ClientUser == null))
                {
                    Assert.Fail("Error");
                }

                var allClientUsers = allClientUserPaymentMethods.Select(x => x.ClientUser).DistinctBy(x => x.ClientUserId);

                if (allClientUsers.Any(x => x.ClientBusinessId != null && x.ClientBusiness == null))
                {
                    Assert.Fail("Error");
                }

                if (allClientUsers.Any(x => x.ClientBusinessId == null && x.ClientBusiness != null))
                {
                    Assert.Fail("Error");
                }

                var allClientBusiness = allClientUsers
                    .Where(x => x.ClientBusiness != null)
                    .Select(x => x.ClientBusiness)
                    .DistinctBy(x => x.ClientBusinessId);

                try
                {
                    _dbContext.AttachRange(paymentMethods);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                var changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }

                _dbContext.DetachAllEntities();

                var paymentMethods2 = paymentMethodQuery.ToList();

                TwoCollectionCompare(paymentMethods2, paymentMethods, x => x.PaymentMethodId);

                var paymentMethods2_ClientUserPaymentMethods_LoadResult = paymentMethods2.LoadNavigationCollection(x => x.ClientUserPaymentMethods, _dbContext);

                var realAllClientUserPaymentMethods = paymentMethods2.SelectMany(x => x.ClientUserPaymentMethods);

                TwoCollectionCompare(realAllClientUserPaymentMethods, allClientUserPaymentMethods, x => x.ClientUserPaymentMethodId);
                TwoCollectionCompare(realAllClientUserPaymentMethods, paymentMethods2_ClientUserPaymentMethods_LoadResult, x => x.ClientUserPaymentMethodId);

                var realAllClientUserPaymentMethods_ClientUser_LoadResult = realAllClientUserPaymentMethods.LoadNavigation(x => x.ClientUser, _dbContext);

                var realAllClientUser = realAllClientUserPaymentMethods.Select(x => x.ClientUser).DistinctBy(x => x.ClientUserId);

                TwoCollectionCompare(realAllClientUser, allClientUsers, x => x.ClientUserId);
                TwoCollectionCompare(realAllClientUser, realAllClientUserPaymentMethods_ClientUser_LoadResult, x => x.ClientUserId);

                var realAllClientUser_ClientBusiness_LoadResult = realAllClientUser.LoadNavigation(x => x.ClientBusiness, _dbContext);

                var realAllClientBusinesses = realAllClientUser.Where(x => x.ClientBusiness != null).Select(x => x.ClientBusiness).DistinctBy(x => x.ClientBusinessId);

                TwoCollectionCompare(realAllClientBusinesses, allClientBusiness, x => x.ClientBusinessId);
                TwoCollectionCompare(realAllClientBusinesses, realAllClientUser_ClientBusiness_LoadResult, x => x.ClientBusinessId);
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var paymentMethodQuery = _dbContext.GetAllQueryablePaymentMethod(isTracking: true)
                    .Where(x => allPaymentMethodIds.Contains(x.PaymentMethodId))
                    .OrderBy(x => x.PaymentMethodId)
                    .Take(1000);

                var paymentMethods = paymentMethodQuery
                    .IncludeManually(x => x.ClientUserPaymentMethods, _dbContext)
                    .ThenIncludeManually(x => x.ClientUser)
                    .ThenIncludeManually(x => x.ClientBusiness)
                    .ToList();

                var trackedEntitiesActual = _dbContext.ChangeTracker.Entries().Count();

                var trackedEntitiesExpected = paymentMethods.Count
                    + paymentMethods.SelectMany(x => x.ClientUserPaymentMethods).Count()
                    + paymentMethods.SelectMany(x => x.ClientUserPaymentMethods).Select(x => x.ClientUser).DistinctBy(x => x.ClientUserId).Count()
                    + paymentMethods.SelectMany(x => x.ClientUserPaymentMethods).Where(x => x.ClientUser.ClientBusinessId != null).Select(x => x.ClientUser.ClientBusiness).DistinctBy(x => x.ClientBusinessId).Count();

                if (trackedEntitiesActual != trackedEntitiesExpected)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        [TestMethod]
        public void Test041ThenIncludeTwiceManyToOneOneToManyOneToMany()
        {
            try
            {
                var contactEmailQuery = _dbContext.GetAllQueryableContactEmails()
                    .Where(x => x.ClientUserId != null)
                    .OrderBy(x => x.ContactEmailId)
                    .Take(1000);

                var contactEmails = contactEmailQuery
                    .IncludeManually(x => x.ClientUser, _dbContext)
                    .ThenIncludeManually(x => x.Orders)
                    .ThenIncludeManually(x => x.OrderProducts)
                    .ToList();

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                if (contactEmails.Any(u => u.ClientUser == null))
                {
                    Assert.Fail("Error");
                }

                var allClientUsers = contactEmails.Select(x => x.ClientUser).DistinctBy(x => x.ClientUserId);

                var allOrders = allClientUsers.SelectMany(x => x.Orders);

                if (!allOrders.Any())
                {
                    Assert.Fail("No Many entities load");
                }

                if (allOrders.Any(x => x.ClientUser == null))
                {
                    Assert.Fail("Error");
                }

                var allOrderProducts = allOrders.SelectMany(x => x.OrderProducts);

                if (!allOrderProducts.Any())
                {
                    Assert.Fail("No Many entities load");
                }

                if (allOrderProducts.Any(x => x.Order == null))
                {
                    Assert.Fail("Error");
                }

                try
                {
                    _dbContext.AttachRange(contactEmails);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                var changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }

                _dbContext.DetachAllEntities();

                var contactEmails2 = contactEmailQuery.ToList();

                TwoCollectionCompare(contactEmails2, contactEmails, x => x.ContactEmailId);

                var contactEmails2_ClientUser_LoadResult = contactEmails2.LoadNavigation(x => x.ClientUser, _dbContext);

                var realAllClientUsers = contactEmails2.Select(x => x.ClientUser).DistinctBy(x => x.ClientUserId);

                TwoCollectionCompare(realAllClientUsers, allClientUsers, x => x.ClientUserId);
                TwoCollectionCompare(realAllClientUsers, contactEmails2_ClientUser_LoadResult, x => x.ClientUserId);

                var realAllClientUsers_Orders_LoadResult = realAllClientUsers.LoadNavigationCollection(x => x.Orders, _dbContext);

                var realAllOrders = realAllClientUsers.SelectMany(x => x.Orders);

                TwoCollectionCompare(realAllOrders, allOrders, x => x.OrderId);
                TwoCollectionCompare(realAllOrders, realAllClientUsers_Orders_LoadResult, x => x.OrderId);

                var realAllOrders_OrderProducts_LoadResult = realAllOrders.LoadNavigationCollection(x => x.OrderProducts, _dbContext);

                var realAllOrderProducts = realAllOrders.SelectMany(x => x.OrderProducts);

                TwoCollectionCompare(realAllOrderProducts, allOrderProducts, x => x.OrderId, x => x.ProductId);
                TwoCollectionCompare(realAllOrderProducts, realAllOrders_OrderProducts_LoadResult, x => x.OrderId, x => x.ProductId);
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var contactEmailQuery = _dbContext.GetAllQueryableContactEmails(isTracking: true)
                    .Where(x => x.ClientUserId != null)
                    .OrderBy(x => x.ContactEmailId)
                    .Take(1000);

                var contactEmails = contactEmailQuery
                    .IncludeManually(x => x.ClientUser, _dbContext)
                    .ThenIncludeManually(x => x.Orders)
                    .ThenIncludeManually(x => x.OrderProducts)
                    .ToList();

                var trackedEntitiesActual = _dbContext.ChangeTracker.Entries().Count();

                var trackedEntitiesExpected = contactEmails.Count
                    + contactEmails.Select(x => x.ClientUser).DistinctBy(x => x.ClientUserId).Count()
                    + contactEmails.Select(x => x.ClientUser).DistinctBy(x => x.ClientUserId).SelectMany(x => x.Orders).Count()
                    + contactEmails.Select(x => x.ClientUser).DistinctBy(x => x.ClientUserId).SelectMany(x => x.Orders).SelectMany(x => x.OrderProducts).Count();

                if (trackedEntitiesActual != trackedEntitiesExpected)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        [TestMethod]
        public void Test042ThenIncludeTwiceManyToOneOneToManyManyToOne()
        {
            try
            {
                var contactEmailQuery = _dbContext.GetAllQueryableContactEmails()
                    .Where(x => x.ClientUserId != null)
                    .OrderBy(x => x.ContactEmailId)
                    .Take(1000);

                var contactEmails = contactEmailQuery
                    .IncludeManually(x => x.ClientUser, _dbContext)
                    .ThenIncludeManually(x => x.Orders)
                    .ThenIncludeManually(x => x.ClientUserPaymentMethod)
                    .ToList();

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                if (contactEmails.Any(u => u.ClientUser == null))
                {
                    Assert.Fail("Error");
                }

                var allClientUsers = contactEmails.Select(x => x.ClientUser).DistinctBy(x => x.ClientUserId);

                var allOrders = allClientUsers.SelectMany(x => x.Orders);

                if (!allOrders.Any())
                {
                    Assert.Fail("No Many entities load");
                }

                if (allOrders.Any(x => x.ClientUser == null))
                {
                    Assert.Fail("Error");
                }

                if (allOrders.Any(x => x.ClientUserPaymentMethod == null))
                {
                    Assert.Fail("Error");
                }

                var allClientUserPaymentMethods = allOrders.Select(x => x.ClientUserPaymentMethod).DistinctBy(x => x.ClientUserPaymentMethodId);

                try
                {
                    _dbContext.AttachRange(contactEmails);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                var changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }

                _dbContext.DetachAllEntities();

                var contactEmails2 = contactEmailQuery.ToList();

                TwoCollectionCompare(contactEmails2, contactEmails, x => x.ContactEmailId);

                var contactEmails2_ClientUser_LoadResult = contactEmails2.LoadNavigation(x => x.ClientUser, _dbContext);

                var realAllClientUsers = contactEmails2.Select(x => x.ClientUser).DistinctBy(x => x.ClientUserId);

                TwoCollectionCompare(realAllClientUsers, allClientUsers, x => x.ClientUserId);
                TwoCollectionCompare(realAllClientUsers, contactEmails2_ClientUser_LoadResult, x => x.ClientUserId);

                var realAllClientUsers_Orders_LoadResult = realAllClientUsers.LoadNavigationCollection(x => x.Orders, _dbContext);

                var realAllOrders = realAllClientUsers.SelectMany(x => x.Orders);

                TwoCollectionCompare(realAllOrders, allOrders, x => x.OrderId);
                TwoCollectionCompare(realAllOrders, realAllClientUsers_Orders_LoadResult, x => x.OrderId);

                var realAllOrders_ClientUserPaymentMethod_LoadResult = realAllOrders.LoadNavigation(x => x.ClientUserPaymentMethod, _dbContext);

                var realAllClientUserPaymentMethods = realAllOrders.Select(x => x.ClientUserPaymentMethod).DistinctBy(x => x.ClientUserPaymentMethodId);

                TwoCollectionCompare(realAllClientUserPaymentMethods, allClientUserPaymentMethods, x => x.ClientUserPaymentMethodId);
                TwoCollectionCompare(realAllClientUserPaymentMethods, realAllOrders_ClientUserPaymentMethod_LoadResult, x => x.ClientUserPaymentMethodId);
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var contactEmailQuery = _dbContext.GetAllQueryableContactEmails(isTracking: true)
                    .Where(x => x.ClientUserId != null)
                    .OrderBy(x => x.ContactEmailId)
                    .Take(1000);

                var contactEmails = contactEmailQuery
                    .IncludeManually(x => x.ClientUser, _dbContext)
                    .ThenIncludeManually(x => x.Orders)
                    .ThenIncludeManually(x => x.ClientUserPaymentMethod)
                    .ToList();

                var trackedEntitiesActual = _dbContext.ChangeTracker.Entries().Count();

                var trackedEntitiesExpected = contactEmails.Count
                    + contactEmails.Select(x => x.ClientUser).DistinctBy(x => x.ClientUserId).Count()
                    + contactEmails.Select(x => x.ClientUser).DistinctBy(x => x.ClientUserId).SelectMany(x => x.Orders).Count()
                    + contactEmails.Select(x => x.ClientUser).DistinctBy(x => x.ClientUserId).SelectMany(x => x.Orders).Select(x => x.ClientUserPaymentMethod).DistinctBy(x => x.ClientUserPaymentMethodId).Count();

                if (trackedEntitiesActual != trackedEntitiesExpected)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        [TestMethod]
        public void Test043ThenIncludeTwiceManyToOneManyToOneOneToMany()
        {
            var orderIdsWithContactEmail = _dbContext.GetAllQueryableOrders()
                .Where(x => x.ClientUser.ContactEmails.Any())
                .Select(x => x.OrderId)
                .OrderBy(x => x)
                .Take(500)
                .ToList();

            var orderIdsNoContactEmail = _dbContext.GetAllQueryableOrders()
                .Where(x => !x.ClientUser.ContactEmails.Any())
                .Select(x => x.OrderId)
                .OrderBy(x => x)
                .Take(500)
                .ToList();

            var allOrderIds = orderIdsWithContactEmail.Concat(orderIdsNoContactEmail).ToList();

            try
            {
                var orderProductQuery = _dbContext.GetAllQueryableOrderProducts()
                    .Where(x => allOrderIds.Contains(x.OrderId))
                    .OrderBy(x => x.OrderId).ThenBy(x => x.ProductId)
                    .Take(1000);

                var orderProducts = orderProductQuery
                    .IncludeManually(x => x.Order, _dbContext)
                    .ThenIncludeManually(x => x.ClientUser)
                    .ThenIncludeManually(x => x.ContactEmails)
                    .ToList();

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                if (orderProducts.Any(u => u.Order == null))
                {
                    Assert.Fail("Error");
                }

                var allOrders = orderProducts.Select(x => x.Order).DistinctBy(x => x.OrderId);

                if (allOrders.Any(b => b.ClientUser == null))
                {
                    Assert.Fail("Error");
                }

                var allClientUsers = allOrders.Select(x => x.ClientUser).DistinctBy(x => x.ClientUserId);

                var allContactEmails = allClientUsers.SelectMany(x => x.ContactEmails);

                if (!allContactEmails.Any())
                {
                    Assert.Fail("Error");
                }

                if (allContactEmails.Any(x => x.ClientUser == null))
                {
                    Assert.Fail("Error");
                }

                try
                {
                    _dbContext.AttachRange(orderProducts);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                var changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }

                _dbContext.DetachAllEntities();

                var orderProducts2 = orderProductQuery.ToList();

                TwoCollectionCompare(orderProducts2, orderProducts, x => x.OrderId, x => x.ProductId);

                var orderProducts2_Order_LoadResult = orderProducts2.LoadNavigation(x => x.Order, _dbContext);

                var realAllOrders = orderProducts2.Select(x => x.Order).DistinctBy(x => x.OrderId);

                TwoCollectionCompare(realAllOrders, allOrders, x => x.OrderId);
                TwoCollectionCompare(realAllOrders, orderProducts2_Order_LoadResult, x => x.OrderId);

                var realAllOrders_ClientUser_LoadResult = realAllOrders.LoadNavigation(x => x.ClientUser, _dbContext);

                var realAllClientUsers = realAllOrders.Select(x => x.ClientUser).DistinctBy(x => x.ClientUserId);

                TwoCollectionCompare(realAllClientUsers, allClientUsers, x => x.ClientUserId);
                TwoCollectionCompare(realAllClientUsers, realAllOrders_ClientUser_LoadResult, x => x.ClientUserId);

                var realAllClientUsers_ContactEmails_LoadResult = realAllClientUsers.LoadNavigationCollection(x => x.ContactEmails, _dbContext);

                var realAllContactEmails = realAllClientUsers.SelectMany(x => x.ContactEmails);

                TwoCollectionCompare(realAllContactEmails, allContactEmails, x => x.ContactEmailId);
                TwoCollectionCompare(realAllContactEmails, realAllClientUsers_ContactEmails_LoadResult, x => x.ContactEmailId);
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var orderProductQuery = _dbContext.GetAllQueryableOrderProducts(isTracking: true)
                    .Where(x => allOrderIds.Contains(x.OrderId))
                    .OrderBy(x => x.OrderId).ThenBy(x => x.ProductId)
                    .Take(1000);

                var orderProducts = orderProductQuery
                    .IncludeManually(x => x.Order, _dbContext)
                    .ThenIncludeManually(x => x.ClientUser)
                    .ThenIncludeManually(x => x.ContactEmails)
                    .ToList();

                var trackedEntitiesActual = _dbContext.ChangeTracker.Entries().Count();

                var trackedEntitiesExpected = orderProducts.Count
                    + orderProducts.Select(x => x.Order).DistinctBy(x => x.OrderId).Count()
                    + orderProducts.Select(x => x.Order.ClientUser).DistinctBy(x => x.ClientUserId).Count()
                    + orderProducts.Select(x => x.Order.ClientUser).DistinctBy(x => x.ClientUserId).SelectMany(x => x.ContactEmails).Count();

                if (trackedEntitiesActual != trackedEntitiesExpected)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        [TestMethod]
        public void Test044ThenIncludeTwiceManyToOneManyToOneManyToOne()
        {
            var orderIdsWithBusiness = _dbContext.GetAllQueryableOrders()
               .Where(x => x.ClientUser.ClientBusinessId != null)
               .Select(x => x.OrderId)
               .OrderBy(x => x)
               .Take(1000)
               .ToList();

            try
            {
                var orderProductQuery = _dbContext.GetAllQueryableOrderProducts()
                    .Where(x => orderIdsWithBusiness.Contains(x.OrderId))
                    .OrderBy(x => x.OrderId).ThenBy(x => x.ProductId)
                    .Take(1000);

                var orderProducts = orderProductQuery
                    .IncludeManually(x => x.Order, _dbContext)
                    .ThenIncludeManually(x => x.ClientUser)
                    .ThenIncludeManually(x => x.ClientBusiness)
                    .ToList();

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                if (orderProducts.Any(u => u.Order == null))
                {
                    Assert.Fail("Error");
                }

                var allOrder = orderProducts.Select(x => x.Order).DistinctBy(x => x.OrderId);

                if (allOrder.Any(f => f.ClientUser == null))
                {
                    Assert.Fail("Error");
                }

                var allClientUsers = allOrder.Select(x => x.ClientUser).DistinctBy(x => x.ClientUserId);

                if (allClientUsers.Any(b => b.ClientBusiness == null))
                {
                    Assert.Fail("Error");
                }

                var allClientBusinesses = allClientUsers.Select(x => x.ClientBusiness).DistinctBy(x => x.ClientBusinessId);

                try
                {
                    _dbContext.AttachRange(orderProducts);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                var changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }

                _dbContext.DetachAllEntities();

                var orderProduct2 = orderProductQuery.ToList();

                TwoCollectionCompare(orderProduct2, orderProducts, x => x.OrderId, x => x.ProductId);

                orderProduct2.LoadNavigation(x => x.Order, _dbContext);

                var realAllOrders = orderProduct2.Select(x => x.Order).DistinctBy(x => x.OrderId);

                TwoCollectionCompare(realAllOrders, allOrder, x => x.OrderId);

                realAllOrders.LoadNavigation(x => x.ClientUser, _dbContext);

                var realAllClientUsers = realAllOrders.Select(x => x.ClientUser).DistinctBy(x => x.ClientUserId);

                TwoCollectionCompare(realAllClientUsers, allClientUsers, x => x.ClientUserId);

                realAllClientUsers.LoadNavigation(x => x.ClientBusiness, _dbContext);

                var realAllClientBusinesses = realAllClientUsers.Select(x => x.ClientBusiness).DistinctBy(x => x.ClientBusinessId);

                TwoCollectionCompare(realAllClientBusinesses, allClientBusinesses, x => x.ClientBusinessId);
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var orderProductQuery = _dbContext.GetAllQueryableOrderProducts(isTracking: true)
                    .Where(x => orderIdsWithBusiness.Contains(x.OrderId))
                    .OrderBy(x => x.OrderId).ThenBy(x => x.ProductId)
                    .Take(1000);

                var orderProducts = orderProductQuery
                    .IncludeManually(x => x.Order, _dbContext)
                    .ThenIncludeManually(x => x.ClientUser)
                    .ThenIncludeManually(x => x.ClientBusiness)
                    .ToList();

                var trackedEntitiesActual = _dbContext.ChangeTracker.Entries().Count();

                var trackedEntitiesExpected = orderProducts.Count
                    + orderProducts.Select(x => x.Order).DistinctBy(x => x.OrderId).Count()
                    + orderProducts.Select(x => x.Order.ClientUser).DistinctBy(x => x.ClientUserId).Count()
                    + orderProducts.Select(x => x.Order.ClientUser.ClientBusiness).DistinctBy(x => x.ClientBusinessId).Count();

                if (trackedEntitiesActual != trackedEntitiesExpected)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        [TestMethod]
        public void Test045ThenIncludeComplexity1()
        {
            try
            {
                var orderQuery = _dbContext.GetAllQueryableOrders()
                    .OrderBy(x => x.OrderId)
                    .Take(1000);

                var q1 = orderQuery
                    .IncludeManually(x => x.OrderProducts, _dbContext)
                    .ThenIncludeManually(x => x.Product)
                    .ThenIncludeManually(x => x.ProductPrices)
                    .IncludeManually(x => x.OrderProducts)
                    .ThenIncludeManually(x => x.Product)
                    .ThenIncludeManually(x => x.OrderProducts)
                    .IncludeManually(x => x.OrderProducts)
                    .ThenIncludeManually(x => x.Product)
                    .ThenIncludeManually(x => x.ProductLogo, isOneToOne: true)
                    .IncludeManually(x => x.OrderProducts)
                    .ThenIncludeManually(x => x.Product)
                    .ThenIncludeManually(x => x.Image, isOneToOne: true)
                    .IncludeManually(x => x.OrderProducts)
                    .ThenIncludeManually(x => x.Product)
                    .ThenIncludeManually(x => x.License)
                    .ThenIncludeManually(x => x.Products)
                    .IncludeManually(x => x.OrderProducts)
                    .ThenIncludeManually(x => x.Product)
                    .ThenIncludeManually(x => x.ProductMerchantProvider)
                    .ThenIncludeManually(x => x.MerchantProvider)
                    .ThenIncludeManually(x => x.ProductMerchantProviders)
                    .IncludeManually(x => x.OrderProducts)
                    .ThenIncludeManually(x => x.Product)
                    .ThenIncludeManually(x => x.ProductPrices)
                    .IncludeManually(x => x.ClientUser)
                    .ThenIncludeManually(x => x.ClientUserProfile)
                    .IncludeManually(x => x.ClientUser)
                    .ThenIncludeManually(x => x.ClientBusiness)
                    .IncludeManually(x => x.ClientUser)
                    .ThenIncludeManually(x => x.IdentityCardBlobStorageItem)
                    .IncludeManually(x => x.ClientUser)
                    .ThenIncludeManually(x => x.Orders)
                    .IncludeManually(x => x.ClientUser)
                    .ThenIncludeManually(x => x.ContactEmails)
                    .IncludeManually(x => x.ClientUser)
                    .ThenIncludeManually(x => x.ContactPhoneNumbers)
                    .IncludeManually(x => x.ClientUser)
                    .ThenIncludeManually(x => x.ClientUserPaymentMethods)
                    .ThenIncludeManually(x => x.PaymentMethod)
                    .IncludeManually(x => x.BillingAddress, isOneToOne: true)
                    .ThenIncludeManually(x => x.CountryOrRegion)
                    .IncludeManually(x => x.DeliveryAddress, isOneToOne: true)
                    .ThenIncludeManually(x => x.CountryOrRegion)
                    .IncludeManually(x => x.ClientUserPaymentMethod)
                    .ThenIncludeManually(x => x.PaymentMethod);

                var orders1 = q1.ToList();

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                try
                {
                    _dbContext.AttachRange(orders1);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Attach test failed: [{ex.Message}] [{ex.ToString()}]");
                }

                var changedEntities = _dbContext.ChangeTracker.Entries().Where(x => x.State != EntityState.Unchanged).ToList();

                if (changedEntities.Any())
                {
                    Console.WriteLine("Changed entities found");

                    foreach (var e1 in changedEntities)
                    {
                        Console.WriteLine(e1);
                    }

                    Assert.Fail("Changed entities found");
                }

                _dbContext.DetachAllEntities();

                var orders2 = orderQuery.ToList();

                TwoCollectionCompare(orders1, orders2, x => x.OrderId);

                orders2.LoadNavigationCollection(x => x.OrderProducts, _dbContext);
                var allOrderProducts1 = orders1.SelectMany(x => x.OrderProducts);
                var allOrderProducts2 = orders2.SelectMany(x => x.OrderProducts);
                TwoCollectionCompare(allOrderProducts1, allOrderProducts2, x => x.OrderId, x => x.ProductId);

                allOrderProducts2.LoadNavigation(x => x.Product, _dbContext);
                var allProducts1 = allOrderProducts1.Select(x => x.Product).DistinctBy(x => x.ProductId);
                var allProducts2 = allOrderProducts2.Select(x => x.Product).DistinctBy(x => x.ProductId);
                TwoCollectionCompare(allProducts1, allProducts2, x => x.ProductId);

                allProducts2.LoadNavigationCollection(x => x.OrderProducts, _dbContext);
                var allProductOrderProducts1 = allProducts1.SelectMany(x => x.OrderProducts).DistinctBy(x => new { x.OrderId, x.ProductId });
                var allProductOrderProducts2 = allProducts2.SelectMany(x => x.OrderProducts).DistinctBy(x => new { x.OrderId, x.ProductId });
                TwoCollectionCompare(allProductOrderProducts1, allProductOrderProducts2, x => x.OrderId, x => x.ProductId);

                allProducts2.LoadNavigation(x => x.ProductLogo, _dbContext);
                var allProductLogos1 = allProducts1.Where(x => x.ProductLogo != null).Select(x => x.ProductLogo).DistinctBy(x => x.BlobStorageItemId);
                var allProductLogos2 = allProducts2.Where(x => x.ProductLogo != null).Select(x => x.ProductLogo).DistinctBy(x => x.BlobStorageItemId);
                TwoCollectionCompare(allProductLogos1, allProductLogos2, x => x.BlobStorageItemId);

                allProducts2.LoadNavigation(x => x.Image, _dbContext);
                var allProductImages1 = allProducts1.Where(x => x.Image != null).Select(x => x.Image).DistinctBy(x => x.BlobStorageItemId);
                var allProductImages2 = allProducts2.Where(x => x.Image != null).Select(x => x.Image).DistinctBy(x => x.BlobStorageItemId);
                TwoCollectionCompare(allProductImages1, allProductImages2, x => x.BlobStorageItemId);

                allProducts2.LoadNavigation(x => x.License, _dbContext);
                var allProductLicenses1 = allProducts1.Select(x => x.License).DistinctBy(x => x.LicenseId);
                var allProductLicenses2 = allProducts2.Select(x => x.License).DistinctBy(x => x.LicenseId);
                TwoCollectionCompare(allProductLicenses1, allProductLicenses2, x => x.LicenseId);

                allProductLicenses2.LoadNavigationCollection(x => x.Products, _dbContext);
                var allProductLicenseProducts1 = allProductLicenses1.SelectMany(x => x.Products).DistinctBy(x => x.ProductId);
                var allProductLicenseProducts2 = allProductLicenses2.SelectMany(x => x.Products).DistinctBy(x => x.ProductId);
                TwoCollectionCompare(allProductLicenseProducts1, allProductLicenseProducts2, x => x.ProductId);

                allProducts2.LoadNavigation(x => x.ProductMerchantProvider, _dbContext);
                var allProductMerchantProviders1 = allProducts1.Where(x => x.ProductMerchantProvider != null).Select(x => x.ProductMerchantProvider).DistinctBy(x => x.ProductMerchantProviderId);
                var allProductMerchantProviders2 = allProducts2.Where(x => x.ProductMerchantProvider != null).Select(x => x.ProductMerchantProvider).DistinctBy(x => x.ProductMerchantProviderId);
                TwoCollectionCompare(allProductMerchantProviders1, allProductMerchantProviders2, x => x.ProductMerchantProviderId);

                allProductMerchantProviders2.LoadNavigation(x => x.MerchantProvider, _dbContext);
                var allMerchantProviders1 = allProductMerchantProviders1.Select(x => x.MerchantProvider).DistinctBy(x => x.MerchantProviderId);
                var allMerchantProviders2 = allProductMerchantProviders2.Select(x => x.MerchantProvider).DistinctBy(x => x.MerchantProviderId);
                TwoCollectionCompare(allMerchantProviders1, allMerchantProviders2, x => x.MerchantProviderId);

                allMerchantProviders2.LoadNavigationCollection(x => x.ProductMerchantProviders, _dbContext);
                var allMerchantProviderProductMerchantProviders1 = allMerchantProviders1.SelectMany(x => x.ProductMerchantProviders).DistinctBy(x => x.ProductMerchantProviderId);
                var allMerchantProviderProductMerchantProviders2 = allMerchantProviders2.SelectMany(x => x.ProductMerchantProviders).DistinctBy(x => x.ProductMerchantProviderId);
                TwoCollectionCompare(allMerchantProviderProductMerchantProviders1, allMerchantProviderProductMerchantProviders2, x => x.ProductMerchantProviderId);

                allProducts2.LoadNavigationCollection(x => x.ProductPrices, _dbContext);
                var allProductPrices1 = allProducts1.SelectMany(x => x.ProductPrices).DistinctBy(x => x.ProductPriceId);
                var allProductPrices2 = allProducts2.SelectMany(x => x.ProductPrices).DistinctBy(x => x.ProductPriceId);
                TwoCollectionCompare(allProductPrices1, allProductPrices2, x => x.ProductPriceId);

                orders2.LoadNavigation(x => x.ClientUser, _dbContext);
                var allClientUsers1 = orders1.Select(x => x.ClientUser).DistinctBy(x => x.ClientUserId);
                var allClientUsers2 = orders2.Select(x => x.ClientUser).DistinctBy(x => x.ClientUserId);
                TwoCollectionCompare(allClientUsers1, allClientUsers2, x => x.ClientUserId);

                allClientUsers2.LoadNavigation(x => x.ClientUserProfile, _dbContext);
                var allClientUserProfiles1 = allClientUsers1.Where(x => x.ClientUserProfile != null).Select(x => x.ClientUserProfile).DistinctBy(x => x.ClientUserProfileId);
                var allClientUserProfiles2 = allClientUsers2.Where(x => x.ClientUserProfile != null).Select(x => x.ClientUserProfile).DistinctBy(x => x.ClientUserProfileId);
                TwoCollectionCompare(allClientUserProfiles1, allClientUserProfiles2, x => x.ClientUserProfileId);

                allClientUsers2.LoadNavigation(x => x.ClientBusiness, _dbContext);
                var allClientBusinesses1 = allClientUsers1.Where(x => x.ClientBusiness != null).Select(x => x.ClientBusiness).DistinctBy(x => x.ClientBusinessId);
                var allClientBusinesses2 = allClientUsers2.Where(x => x.ClientBusiness != null).Select(x => x.ClientBusiness).DistinctBy(x => x.ClientBusinessId);
                TwoCollectionCompare(allClientBusinesses1, allClientBusinesses2, x => x.ClientBusinessId);

                allClientUsers2.LoadNavigationCollection(x => x.ContactEmails, _dbContext);
                var allClientUserContactEmails1 = allClientUsers1.SelectMany(x => x.ContactEmails);
                var allClientUserContactEmails2 = allClientUsers2.SelectMany(x => x.ContactEmails);
                TwoCollectionCompare(allClientUserContactEmails1, allClientUserContactEmails2, x => x.ContactEmailId);

                allClientUsers2.LoadNavigationCollection(x => x.ContactPhoneNumbers, _dbContext);
                var allClientUserContactPhoneNumbers1 = allClientUsers1.SelectMany(x => x.ContactPhoneNumbers);
                var allClientUserContactPhoneNumbers2 = allClientUsers2.SelectMany(x => x.ContactPhoneNumbers);
                TwoCollectionCompare(allClientUserContactPhoneNumbers1, allClientUserContactPhoneNumbers2, x => x.ContactPhoneNumberId);

                allClientUsers2.LoadNavigationCollection(x => x.ClientUserPaymentMethods, _dbContext);
                var allClientUserPaymentMethods1 = allClientUsers1.SelectMany(x => x.ClientUserPaymentMethods);
                var allClientUserPaymentMethods2 = allClientUsers2.SelectMany(x => x.ClientUserPaymentMethods);
                TwoCollectionCompare(allClientUserPaymentMethods1, allClientUserPaymentMethods2, x => x.ClientUserPaymentMethodId);

                allClientUserPaymentMethods2.LoadNavigation(x => x.PaymentMethod, _dbContext);
                var allPaymentMethods1 = allClientUserPaymentMethods1.Select(x => x.PaymentMethod).DistinctBy(x => x.PaymentMethodId);
                var allPaymentMethods2 = allClientUserPaymentMethods2.Select(x => x.PaymentMethod).DistinctBy(x => x.PaymentMethodId);
                TwoCollectionCompare(allPaymentMethods1, allPaymentMethods2, x => x.PaymentMethodId);

                orders2.LoadNavigation(x => x.BillingAddress, _dbContext);
                var allBillingAddresses1 = orders1.Select(x => x.BillingAddress);
                var allBillingAddresses2 = orders2.Select(x => x.BillingAddress);
                TwoCollectionCompare(allBillingAddresses1, allBillingAddresses2, x => x.AddressId);

                allBillingAddresses2.LoadNavigation(x => x.CountryOrRegion, _dbContext);
                var allBillingAddressCountries1 = allBillingAddresses1.Select(x => x.CountryOrRegion).DistinctBy(x => x.CountryOrRegionId);
                var allBillingAddressCountries2 = allBillingAddresses2.Select(x => x.CountryOrRegion).DistinctBy(x => x.CountryOrRegionId);
                TwoCollectionCompare(allBillingAddressCountries1, allBillingAddressCountries2, x => x.CountryOrRegionId);

                orders2.LoadNavigation(x => x.DeliveryAddress, _dbContext);
                var allDeliveryAddresses1 = orders1.Select(x => x.DeliveryAddress);
                var allDeliveryAddresses2 = orders2.Select(x => x.DeliveryAddress);
                TwoCollectionCompare(allDeliveryAddresses1, allDeliveryAddresses2, x => x.AddressId);

                allDeliveryAddresses2.LoadNavigation(x => x.CountryOrRegion, _dbContext);
                var allDeliveryAddressCountries1 = allDeliveryAddresses1.Select(x => x.CountryOrRegion).DistinctBy(x => x.CountryOrRegionId);
                var allDeliveryAddressCountries2 = allDeliveryAddresses2.Select(x => x.CountryOrRegion).DistinctBy(x => x.CountryOrRegionId);
                TwoCollectionCompare(allDeliveryAddressCountries1, allDeliveryAddressCountries1, x => x.CountryOrRegionId);

                orders2.LoadNavigation(x => x.ClientUserPaymentMethod, _dbContext);
                var allOrderClientUserPaymentMethods1 = orders1.Select(x => x.ClientUserPaymentMethod).DistinctBy(x => x.ClientUserPaymentMethodId);
                var allOrderClientUserPaymentMethods2 = orders2.Select(x => x.ClientUserPaymentMethod).DistinctBy(x => x.ClientUserPaymentMethodId);
                TwoCollectionCompare(allOrderClientUserPaymentMethods1, allOrderClientUserPaymentMethods2, x => x.ClientUserPaymentMethodId);

                allOrderClientUserPaymentMethods2.LoadNavigation(x => x.PaymentMethod, _dbContext);
                var allOrderPaymentMethods1 = allOrderClientUserPaymentMethods1.Select(x => x.PaymentMethod).DistinctBy(x => x.PaymentMethodId);
                var allOrderPaymentMethods2 = allOrderClientUserPaymentMethods2.Select(x => x.PaymentMethod).DistinctBy(x => x.PaymentMethodId);
                TwoCollectionCompare(allOrderPaymentMethods1, allOrderPaymentMethods2, x => x.PaymentMethodId);
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var orderQuery = _dbContext.GetAllQueryableOrders(isTracking: true)
                    .OrderBy(x => x.OrderId)
                    .Take(1000);

                var q1 = orderQuery
                    .IncludeManually(x => x.OrderProducts, _dbContext)
                    .ThenIncludeManually(x => x.Product)
                    .ThenIncludeManually(x => x.ProductPrices)
                    .IncludeManually(x => x.OrderProducts)
                    .ThenIncludeManually(x => x.Product)
                    .ThenIncludeManually(x => x.OrderProducts)
                    .IncludeManually(x => x.OrderProducts)
                    .ThenIncludeManually(x => x.Product)
                    .ThenIncludeManually(x => x.ProductLogo, isOneToOne: true)
                    .IncludeManually(x => x.OrderProducts)
                    .ThenIncludeManually(x => x.Product)
                    .ThenIncludeManually(x => x.Image, isOneToOne: true)
                    .IncludeManually(x => x.OrderProducts)
                    .ThenIncludeManually(x => x.Product)
                    .ThenIncludeManually(x => x.License)
                    .ThenIncludeManually(x => x.Products)
                    .IncludeManually(x => x.OrderProducts)
                    .ThenIncludeManually(x => x.Product)
                    .ThenIncludeManually(x => x.ProductMerchantProvider)
                    .ThenIncludeManually(x => x.MerchantProvider)
                    .ThenIncludeManually(x => x.ProductMerchantProviders)
                    .IncludeManually(x => x.OrderProducts)
                    .ThenIncludeManually(x => x.Product)
                    .ThenIncludeManually(x => x.ProductPrices)
                    .IncludeManually(x => x.ClientUser)
                    .ThenIncludeManually(x => x.ClientUserProfile)
                    .IncludeManually(x => x.ClientUser)
                    .ThenIncludeManually(x => x.ClientBusiness)
                    .IncludeManually(x => x.ClientUser)
                    .ThenIncludeManually(x => x.IdentityCardBlobStorageItem)
                    .IncludeManually(x => x.ClientUser)
                    .ThenIncludeManually(x => x.Orders)
                    .IncludeManually(x => x.ClientUser)
                    .ThenIncludeManually(x => x.ContactEmails)
                    .IncludeManually(x => x.ClientUser)
                    .ThenIncludeManually(x => x.ContactPhoneNumbers)
                    .IncludeManually(x => x.ClientUser)
                    .ThenIncludeManually(x => x.ClientUserPaymentMethods)
                    .ThenIncludeManually(x => x.PaymentMethod)
                    .IncludeManually(x => x.BillingAddress, isOneToOne: true)
                    .ThenIncludeManually(x => x.CountryOrRegion)
                    .IncludeManually(x => x.DeliveryAddress, isOneToOne: true)
                    .ThenIncludeManually(x => x.CountryOrRegion)
                    .IncludeManually(x => x.ClientUserPaymentMethod)
                    .ThenIncludeManually(x => x.PaymentMethod);

                var orders1 = q1.ToList();

                var trackedEntitiesActual = _dbContext.ChangeTracker.Entries().Count();

                var trackedEntitiesExpected = orders1.Count
                    + orders1.SelectMany(x => x.OrderProducts).Count()
                    + orders1.SelectMany(x => x.OrderProducts).Select(x => x.Product).DistinctBy(x => x.ProductId).Count()
                    + orders1.SelectMany(x => x.OrderProducts).Where(x => x.Product.ProductLogoId != null).Select(x => x.Product.ProductLogo).DistinctBy(x => x.BlobStorageItemId).Count()
                    + orders1.SelectMany(x => x.OrderProducts).Where(x => x.Product.ImageId != null).Select(x => x.Product.Image).DistinctBy(x => x.BlobStorageItemId).Count()
                    + orders1.SelectMany(x => x.OrderProducts).Select(x => x.Product.License).DistinctBy(x => x.LicenseId).Count()
                    + orders1.SelectMany(x => x.OrderProducts).SelectMany(x => x.Product.OrderProducts).Except(orders1.SelectMany(x => x.OrderProducts)).DistinctBy(x => new { x.OrderId, x.ProductId }).Count()
                    + orders1.SelectMany(x => x.OrderProducts).Where(x => x.Product.ProductMerchantProvider != null).Select(x => x.Product.ProductMerchantProvider).DistinctBy(x => x.ProductMerchantProviderId).Count()
                    + orders1.SelectMany(x => x.OrderProducts).Where(x => x.Product.ProductMerchantProvider != null).Select(x => x.Product.ProductMerchantProvider.MerchantProvider).DistinctBy(x => x.MerchantProviderId).Count()
                    + orders1.SelectMany(x => x.OrderProducts).Where(x => x.Product.ProductMerchantProvider != null).SelectMany(x => x.Product.ProductMerchantProvider.MerchantProvider.ProductMerchantProviders).Except(orders1.SelectMany(x => x.OrderProducts).Where(x => x.Product.ProductMerchantProvider != null).Select(x => x.Product.ProductMerchantProvider)).DistinctBy(x => x.ProductMerchantProviderId).Count()
                    + orders1.SelectMany(x => x.OrderProducts).SelectMany(x => x.Product.ProductPrices).DistinctBy(x => x.ProductPriceId).Count()
                    + orders1.Select(x => x.ClientUser).DistinctBy(x => x.ClientUserId).Count()
                    + orders1.Select(x => x.ClientUser).Where(x => x.ClientUserProfile != null).Select(x => x.ClientUserProfile).DistinctBy(x => x.ClientUserProfileId).Count()
                    + orders1.Select(x => x.ClientUser).Where(x => x.ClientBusiness != null).Select(x => x.ClientBusiness).DistinctBy(x => x.ClientBusinessId).Count()
                    + orders1.Select(x => x.ClientUser).Where(x => x.IdentityCardBlobStorageItem != null).Select(x => x.IdentityCardBlobStorageItem).DistinctBy(x => x.BlobStorageItemId).Count()
                    + orders1.Select(x => x.ClientUser).SelectMany(x => x.Orders).Except(orders1).DistinctBy(x => x.OrderId).Count()
                    + orders1.Select(x => x.ClientUser).SelectMany(x => x.ContactEmails).DistinctBy(x => x.ContactEmailId).Count()
                    + orders1.Select(x => x.ClientUser).SelectMany(x => x.ContactPhoneNumbers).DistinctBy(x => x.ContactPhoneNumberId).Count()
                    + orders1.Select(x => x.ClientUser).SelectMany(x => x.ClientUserPaymentMethods).DistinctBy(x => x.ClientUserPaymentMethodId).Count()
                    + orders1.Select(x => x.ClientUser).SelectMany(x => x.ClientUserPaymentMethods).Select(x => x.PaymentMethod).DistinctBy(x => x.PaymentMethodId).Count()
                    + orders1.Select(x => x.ClientUserPaymentMethod).Except(orders1.SelectMany(x => x.ClientUser.ClientUserPaymentMethods)).DistinctBy(x => x.ClientUserPaymentMethodId).Count()
                    + orders1.Select(x => x.ClientUserPaymentMethod).Select(x => x.PaymentMethod).Except(orders1.SelectMany(x => x.ClientUser.ClientUserPaymentMethods).Select(x => x.PaymentMethod)).DistinctBy(x => x.PaymentMethodId).Count()
                    + orders1.Select(x => x.BillingAddress).DistinctBy(x => x.AddressId).Count()
                    + orders1.Select(x => x.BillingAddress).Select(x => x.CountryOrRegion).DistinctBy(x => x.CountryOrRegionId).Count()
                    + orders1.Select(x => x.DeliveryAddress).DistinctBy(x => x.AddressId).Count()
                    + orders1.Select(x => x.DeliveryAddress).Select(x => x.CountryOrRegion).Except(orders1.Select(x => x.BillingAddress.CountryOrRegion)).DistinctBy(x => x.CountryOrRegionId).Count();

                if (trackedEntitiesActual != trackedEntitiesExpected)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        [TestMethod]
        public void Test046IncludeMix1()
        {
            try
            {
                var orderQuery = _dbContext.GetAllQueryableOrders()
                    .OrderBy(x => x.OrderId)
                    .Take(1000);

                var q1 = orderQuery
                    .Include(x => x.ClientUser)
                    .ThenInclude(x => x.ClientUserPaymentMethods)
                    .ThenInclude(x => x.PaymentMethod)
                    .Include(x => x.ClientUser)
                    .ThenInclude(x => x.ContactEmails)
                    .IncludeManually(x => x.OrderProducts, _dbContext)
                    .ThenIncludeManually(x => x.Product)
                    .ThenIncludeManually(x => x.ProductPrices)
                    .IncludeManually(x => x.OrderProducts)
                    .ThenIncludeManually(x => x.Product)
                    .ThenIncludeManually(x => x.License);

                var test1 = q1.ToList();

                if (_dbContext.ChangeTracker.Entries().Any())
                {
                    Assert.Fail("should not tracking");
                }

                //No attach test since auto include has duplicated items


                _dbContext.DetachAllEntities();

                var orders2 = orderQuery.ToList();

                TwoCollectionCompare(test1, orders2, x => x.OrderId);

                orders2.LoadNavigation(x => x.ClientUser, _dbContext);
                var allClientUsers1 = test1.Select(x => x.ClientUser).DistinctBy(x => x.ClientUserId);
                var allClientUsers2 = orders2.Select(x => x.ClientUser).DistinctBy(x => x.ClientUserId);
                TwoCollectionCompare(allClientUsers1, allClientUsers2, x => x.ClientUserId);

                allClientUsers2.LoadNavigationCollection(x => x.ClientUserPaymentMethods, _dbContext);
                var allClientUserPaymentMethods1 = allClientUsers1.SelectMany(x => x.ClientUserPaymentMethods).DistinctBy(x => x.ClientUserPaymentMethodId);
                var allClientUserPaymentMethods2 = allClientUsers2.SelectMany(x => x.ClientUserPaymentMethods).DistinctBy(x => x.ClientUserPaymentMethodId);
                TwoCollectionCompare(allClientUserPaymentMethods1, allClientUserPaymentMethods2, x => x.ClientUserPaymentMethodId);

                allClientUserPaymentMethods2.LoadNavigation(x => x.PaymentMethod, _dbContext);
                var allPaymentMethods1 = allClientUserPaymentMethods1.Select(x => x.PaymentMethod).DistinctBy(x => x.PaymentMethodId);
                var allPaymentMethods2 = allClientUserPaymentMethods2.Select(x => x.PaymentMethod).DistinctBy(x => x.PaymentMethodId);
                TwoCollectionCompare(allPaymentMethods1, allPaymentMethods2, x => x.PaymentMethodId);

                allClientUsers2.LoadNavigationCollection(x => x.ContactEmails, _dbContext);
                var allClientUserContactEmails1 = allClientUsers1.SelectMany(x => x.ContactEmails).DistinctBy(x => x.ContactEmailId);
                var allClientUserContactEmails2 = allClientUsers2.SelectMany(x => x.ContactEmails).DistinctBy(x => x.ContactEmailId);
                TwoCollectionCompare(allClientUserContactEmails1, allClientUserContactEmails2, x => x.ContactEmailId);

                orders2.LoadNavigationCollection(x => x.OrderProducts, _dbContext);
                var allOrderProducts1 = test1.SelectMany(x => x.OrderProducts);
                var allOrderProducts2 = orders2.SelectMany(x => x.OrderProducts);
                TwoCollectionCompare(allOrderProducts1, allOrderProducts2, x => x.OrderId, x => x.ProductId);

                allOrderProducts2.LoadNavigation(x => x.Product, _dbContext);
                var allProducts1 = allOrderProducts1.Select(x => x.Product).DistinctBy(x => x.ProductId);
                var allProducts2 = allOrderProducts2.Select(x => x.Product).DistinctBy(x => x.ProductId);
                TwoCollectionCompare(allProducts1, allProducts2, x => x.ProductId);

                allProducts2.LoadNavigationCollection(x => x.ProductPrices, _dbContext);
                var allProductPrices1 = allProducts1.SelectMany(x => x.ProductPrices).DistinctBy(x => x.ProductPriceId);
                var allProductPrices2 = allProducts2.SelectMany(x => x.ProductPrices).DistinctBy(x => x.ProductPriceId);
                TwoCollectionCompare(allProductPrices1, allProductPrices2, x => x.ProductPriceId);

                allProducts2.LoadNavigation(x => x.License, _dbContext);
                var allProductLicenses1 = allProducts1.Select(x => x.License).DistinctBy(x => x.LicenseId);
                var allProductLicenses2 = allProducts2.Select(x => x.License).DistinctBy(x => x.LicenseId);
                TwoCollectionCompare(allProductLicenses1, allProductLicenses2, x => x.LicenseId);
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }

            try
            {
                var orderQuery = _dbContext.GetAllQueryableOrders(isTracking: true)
                   .OrderBy(x => x.OrderId)
                   .Take(1000);

                var q1 = orderQuery
                    .Include(x => x.ClientUser)
                    .ThenInclude(x => x.ClientUserPaymentMethods)
                    .ThenInclude(x => x.PaymentMethod)
                    .Include(x => x.ClientUser)
                    .ThenInclude(x => x.ContactEmails)
                    .IncludeManually(x => x.OrderProducts, _dbContext)
                    .ThenIncludeManually(x => x.Product)
                    .ThenIncludeManually(x => x.ProductPrices)
                    .IncludeManually(x => x.OrderProducts)
                    .ThenIncludeManually(x => x.Product)
                    .ThenIncludeManually(x => x.License);

                var orders1 = q1.ToList();

                var trackedEntitiesActual = _dbContext.ChangeTracker.Entries().Count();

                var trackedEntitiesExpected = orders1.Count
                    + orders1.Select(x => x.ClientUser).DistinctBy(x => x.ClientUserId).Count()
                    + orders1.SelectMany(x => x.ClientUser.ClientUserPaymentMethods).DistinctBy(x => x.ClientUserPaymentMethodId).Count()
                    + orders1.SelectMany(x => x.ClientUser.ClientUserPaymentMethods).Select(x => x.PaymentMethod).DistinctBy(x => x.PaymentMethodId).Count()
                    + orders1.SelectMany(x => x.ClientUser.ContactEmails).DistinctBy(x => x.ContactEmailId).Count()
                    + orders1.SelectMany(x => x.OrderProducts).DistinctBy(x => new { x.OrderId, x.ProductId }).Count()
                    + orders1.SelectMany(x => x.OrderProducts).Select(x => x.Product).DistinctBy(x => x.ProductId).Count()
                    + orders1.SelectMany(x => x.OrderProducts).Select(x => x.Product).SelectMany(x => x.ProductPrices).DistinctBy(x => x.ProductPriceId).Count()
                    + orders1.SelectMany(x => x.OrderProducts).Select(x => x.Product.License).DistinctBy(x => x.LicenseId).Count();

                if (trackedEntitiesActual != trackedEntitiesExpected)
                {
                    Assert.Fail("Error");
                }
            }
            finally
            {
                _dbContext.DetachAllEntities();
            }
        }

        #endregion

        private void TwoElementCompare<T, TProperty>(T t1, T t2, Func<T, TProperty> keySelector)
        {
            Assert.IsNotNull(t1);
            Assert.IsNotNull(t2);

            var key1 = keySelector(t1);
            var key2 = keySelector(t2);

            Assert.AreEqual(key1, key2);
        }

        private void TwoCollectionCompare<T, TProperty>(IEnumerable<T> l1, IEnumerable<T> l2, Func<T, TProperty> keySelector)
        {
            Assert.AreEqual(l1.Count(), l2.Count());

            Assert.IsFalse(l1.Any(x => x == null));
            Assert.IsFalse(l2.Any(x => x == null));

            var joinQuery = l1.Join(l2, x => keySelector(x), y => keySelector(y), (x, y) => new { l1 = x, l2 = y });

            Assert.AreEqual(l1.Count(), joinQuery.Count());
        }

        private void TwoCollectionCompare<T, TProperty>(IEnumerable<T> l1, IEnumerable<T> l2, Func<T, TProperty> keySelector1, Func<T, TProperty> keySelector2)
        {
            Assert.AreEqual(l1.Count(), l2.Count());

            Assert.IsFalse(l1.Any(x => x == null));
            Assert.IsFalse(l2.Any(x => x == null));

            var joinQuery = l1.Join(l2,
                x => new { Key1 = keySelector1(x), Key2 = keySelector2(x) },
                y => new { Key1 = keySelector1(y), Key2 = keySelector2(y) },
                (x, y) => new { l1 = x, l2 = y });

            Assert.AreEqual(l1.Count(), joinQuery.Count());
        }

        private void ManuallyLoadProductProductPrices(IEnumerable<Product> products,
            bool isTracking = false)
        {
            if (products == null || !products.Any())
            {
                return;
            }

            var productsIds = products.Select(x => x.ProductId).ToList();

            var query = _dbContext.ProductPrices
                .Where(x => productsIds.Contains(x.ProductId));

            if (!isTracking)
            {
                query = query.AsNoTracking();
            }

            var allProductPrices = _dbContext.GetAllQueryableProductPrices()
                .Where(x => productsIds.Contains(x.ProductId))
                .ToArray();

            var productPricesLookUp = allProductPrices.ToLookup(x => x.ProductId);

            foreach (var product in products)
            {
                var productPrices = productPricesLookUp.FirstOrDefault(x => x.Key == product.ProductId);

                if (productPrices != null)
                {
                    var productPricesList = productPrices.ToList();
                    product.ProductPrices = productPricesList;
                    productPricesList.ForEach(x => x.Product = product);
                }
            }
        }

        private void ManuallyLoadProductPricesProduct(IEnumerable<ProductPrice> productPrices,
            bool isIncludeProductLicense,
            bool isTracking = false)
        {
            if (productPrices == null || !productPrices.Any())
            {
                return;
            }

            var productsIds = productPrices.Select(x => x.ProductId).Distinct().ToList();

            var productsQuery = _dbContext.Products.AsQueryable().Where(x => productsIds.Contains(x.ProductId));

            if (!isTracking)
            {
                productsQuery = productsQuery.AsNoTracking();
            }

            if (isIncludeProductLicense)
            {
                productsQuery = productsQuery.Include(x => x.License);
            }

            var products = productsQuery.ToList();

            foreach (var productPrice in productPrices)
            {
                productPrice.Product = products.FirstOrDefault(x => x.ProductId == productPrice.ProductId);
            }
        }
    }
}
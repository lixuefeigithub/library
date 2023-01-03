using ManualIncludableQueryable.UnitTest.TestDatabase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManualIncludableQueryable.UnitTest
{
    public static class DatabaseSeeder
    {
        public static void SeedDateBaseForManualIncludeUnitTest(MyDbContext dbContext)
        {
            var radmon = new Random();

            #region seed country region
            var canada = new CountryOrRegion
            {
                Code = "CA",
                Name = "Canada",
            };
            dbContext.CountryOrRegions.Add(canada);
            dbContext.SaveChanges();
            #endregion

            #region seed merchant provider

            var merchantProviders = Enumerable.Range(1, 5)
                .Select(x => new MerchantProvider
                {
                    Name = $"test merchant {x}"
                })
                .ToList();

            dbContext.MerchantProviders.AddRange(merchantProviders);

            dbContext.SaveChanges();

            var merchantProviderIds = merchantProviders.Select(x => x.MerchantProviderId).ToArray();

            #endregion

            #region seed products

            var products = new List<Product>();
            var productsBasic = new List<Product>();
            var productsWithLogo = new List<Product>();
            var productsWithImage = new List<Product>();
            var productsWithMerchantProvider = new List<Product>();

            int productIndex = 1;

            #region basic products

            for (int i = productIndex; i <= productIndex + 20; i++)
            {
                var product = new Product
                {
                    ProductName = $"test product {i}",
                    ProductCategory = $"test category {i}",
                    ProductDescription = $"test description {i}",
                    PriceRevision = 1,
                    License = new License
                    {
                        IssuedDate = DateTime.Now.AddDays(-3),
                        ExpiryDate = DateTime.Now.AddYears(1),
                    },
                    ProductPrices = new List<ProductPrice>()
                    {
                        new ProductPrice
                        {
                            Revsision = 1,
                            Price = 10m,
                            RevisionTimestamp = DateTimeOffset.UtcNow,
                        }
                    },
                };

                products.Add(product);
                productsBasic.Add(product);
            }

            productIndex = products.Count + 1;

            #endregion

            #region products with multiple price

            for (int i = productIndex; i <= productIndex + 20; i++)
            {
                var product = new Product
                {
                    ProductName = $"test product {i}",
                    ProductCategory = $"test category {i}",
                    ProductDescription = $"test description {i}",
                    PriceRevision = 1,
                    License = new License
                    {
                        IssuedDate = DateTime.Now.AddDays(-3),
                        ExpiryDate = DateTime.Now.AddYears(1),
                    },
                    ProductPrices = Enumerable.Range(1, 10).Select(i => new ProductPrice
                    {
                        Price = 10m + i,
                        Revsision = i,
                        RevisionTimestamp = DateTimeOffset.UtcNow,
                    })
                    .ToList(),
                };

                products.Add(product);
            }

            productIndex = products.Count + 1;

            #endregion

            #region products with Image

            for (int i = productIndex; i <= productIndex + 20; i++)
            {
                var product = new Product
                {
                    ProductName = $"test product {i}",
                    ProductCategory = $"test category {i}",
                    ProductDescription = $"test description {i}",
                    PriceRevision = 1,
                    License = new License
                    {
                        IssuedDate = DateTime.Now.AddDays(-3),
                        ExpiryDate = DateTime.Now.AddYears(1),
                    },
                    ProductPrices = Enumerable.Range(1, 10).Select(i => new ProductPrice
                    {
                        Price = 10m + i,
                        Revsision = i,
                        RevisionTimestamp = DateTimeOffset.UtcNow,
                    })
                    .ToList(),
                    Image = new BlobStorageItem
                    {
                        BlobName = $"testblob/{Guid.NewGuid()}",
                    },
                };

                products.Add(product);
                productsWithImage.Add(product);
            }

            productIndex = products.Count + 1;

            #endregion

            #region products with Logo

            for (int i = productIndex; i <= productIndex + 20; i++)
            {
                var product = new Product
                {
                    ProductName = $"test product {i}",
                    ProductCategory = $"test category {i}",
                    ProductDescription = $"test description {i}",
                    PriceRevision = 1,
                    License = new License
                    {
                        IssuedDate = DateTime.Now.AddDays(-3),
                        ExpiryDate = DateTime.Now.AddYears(1),
                    },
                    ProductPrices = new List<ProductPrice>()
                    {
                        new ProductPrice
                        {
                            Revsision = 1,
                            Price = 10m,
                            RevisionTimestamp = DateTimeOffset.UtcNow,
                        }
                    },
                    Image = new BlobStorageItem
                    {
                        BlobName = $"testblob/{Guid.NewGuid()}",
                    },
                    ProductLogo = new BlobStorageItem
                    {
                        BlobName = $"testblob/{Guid.NewGuid()}",
                    }
                };

                products.Add(product);
                productsWithImage.Add(product);
                productsWithLogo.Add(product);
            }

            productIndex = products.Count + 1;

            #endregion

            #region products with merchant provider

            for (int i = productIndex; i <= productIndex + 20; i++)
            {
                var product = new Product
                {
                    ProductName = $"test product {i}",
                    ProductCategory = $"test category {i}",
                    ProductDescription = $"test description {i}",
                    PriceRevision = 1,
                    License = new License
                    {
                        IssuedDate = DateTime.Now.AddDays(-3),
                        ExpiryDate = DateTime.Now.AddYears(1),
                    },
                    ProductPrices = new List<ProductPrice>()
                    {
                        new ProductPrice
                        {
                            Revsision = 1,
                            Price = 10m,
                            RevisionTimestamp = DateTimeOffset.UtcNow,
                        }
                    },
                    ProductLogo = new BlobStorageItem
                    {
                        BlobName = $"testblob/{Guid.NewGuid()}",
                    },
                    ProductMerchantProvider = new ProductMerchantProvider
                    {
                        MerchantProviderId = merchantProviderIds[radmon.Next(0, merchantProviderIds.Length - 1)],
                    }
                };

                products.Add(product);
                productsWithLogo.Add(product);
                productsWithMerchantProvider.Add(product);
            }

            productIndex = products.Count + 1;

            #endregion

            dbContext.AttachRange(products);

            dbContext.SaveChanges();

            #endregion

            #region seed client business
            int businessIndex = 1;
            var clientBusinesses = new List<ClientBusiness>();

            for (int i = businessIndex; i <= businessIndex + 10; i++)
            {
                var clientBusiness = new ClientBusiness
                {
                    ClientBusinessName = $"Test Business {businessIndex}",
                };

                clientBusinesses.Add(clientBusiness);
            }

            businessIndex = clientBusinesses.Count + 1;

            dbContext.ClientBusinesses.AddRange(clientBusinesses);
            dbContext.SaveChanges();
            #endregion

            #region seed client user

            var clientUserIndex = 1;
            var clientUsers = new List<ClientUser>();
            var clientUsersWithPaymentMethods = new List<ClientUser>();

            #region no profile no business no order

            for (int i = clientUserIndex; i <= clientUserIndex + 5; i++)
            {
                var clientUser = new ClientUser
                {
                    FirstName = $"test first name {i}",
                    LastName = $"test last name {i}",
                    Email = "test@aaa.com",
                    IdentityCardBlobStorageItem = new BlobStorageItem
                    {
                        BlobName = $"testblob/{Guid.NewGuid()}"
                    },
                };

                clientUsers.Add(clientUser);
            }

            clientUserIndex = clientUsers.Count + 1;

            #endregion

            #region no profile no email no phone

            for (int i = clientUserIndex; i <= clientUserIndex + 25; i++)
            {
                var business = clientBusinesses[radmon.Next(0, clientBusinesses.Count - 1)];

                var clientUser = new ClientUser
                {
                    ClientBusinessId = business.ClientBusinessId,
                    FirstName = $"test first name {i}",
                    LastName = $"test last name {i}",
                    Email = "test@aaa.com",
                    IdentityCardBlobStorageItem = new BlobStorageItem
                    {
                        BlobName = $"testblob/{Guid.NewGuid()}"
                    },
                    ClientUserPaymentMethods = new List<ClientUserPaymentMethod>()
                    {
                        new ClientUserPaymentMethod
                        {
                            PaymentMethod = new PaymentMethod
                            {
                                PaymentMethodPicture = new BlobStorageItem
                                {
                                    BlobName = $"testblob/{Guid.NewGuid()}",
                                }
                            }
                        },
                        new ClientUserPaymentMethod
                        {
                            PaymentMethod = new PaymentMethod
                            {
                                PaymentMethodPicture = new BlobStorageItem
                                {
                                    BlobName = $"testblob/{Guid.NewGuid()}",
                                }
                            }
                        },
                    }
                };

                clientUsers.Add(clientUser);
                clientUsersWithPaymentMethods.Add(clientUser);
            }

            clientUserIndex = clientUsers.Count + 1;

            #endregion

            #region has profile no email no phone

            for (int i = clientUserIndex; i <= clientUserIndex + 30; i++)
            {
                var business = clientBusinesses[radmon.Next(0, clientBusinesses.Count - 1)];

                var clientUser = new ClientUser
                {
                    ClientBusinessId = business.ClientBusinessId,
                    FirstName = $"test first name {i}",
                    LastName = $"test last name {i}",
                    Email = "test@aaa.com",
                    IdentityCardBlobStorageItem = new BlobStorageItem
                    {
                        BlobName = $"testblob/{Guid.NewGuid()}"
                    },
                    ClientUserProfile = new ClientUserProfile
                    {
                        Age = "18",
                        Title = "CEO",
                    },
                    ClientUserPaymentMethods = new List<ClientUserPaymentMethod>()
                    {
                        new ClientUserPaymentMethod
                        {
                            PaymentMethod = new PaymentMethod
                            {
                                PaymentMethodPicture = new BlobStorageItem
                                {
                                    BlobName = $"testblob/{Guid.NewGuid()}",
                                }
                            }
                        },
                        new ClientUserPaymentMethod
                        {
                            PaymentMethod = new PaymentMethod
                            {
                                PaymentMethodPicture = new BlobStorageItem
                                {
                                    BlobName = $"testblob/{Guid.NewGuid()}",
                                }
                            }
                        },
                    }
                };

                clientUsers.Add(clientUser);
                clientUsersWithPaymentMethods.Add(clientUser);
            }

            clientUserIndex = clientUsers.Count + 1;

            #endregion

            #region has profile has email no phone

            for (int i = clientUserIndex; i <= clientUserIndex + 20; i++)
            {
                var business = clientBusinesses[radmon.Next(0, clientBusinesses.Count - 1)];

                var clientUser = new ClientUser
                {
                    ClientBusinessId = business.ClientBusinessId,
                    FirstName = $"test first name {i}",
                    LastName = $"test last name {i}",
                    Email = "test@aaa.com",
                    IdentityCardBlobStorageItem = new BlobStorageItem
                    {
                        BlobName = $"testblob/{Guid.NewGuid()}"
                    },
                    ClientUserProfile = new ClientUserProfile
                    {
                        Age = "18",
                        Title = "CEO",
                    },
                    ContactEmails = Enumerable
                    .Range(1, 5).Select(x => new ContactEmail
                    {
                        ContactEmailAddress = "test@test.com",
                        NickName = x.ToString(),
                    })
                    .ToList(),
                    ClientUserPaymentMethods = new List<ClientUserPaymentMethod>()
                    {
                        new ClientUserPaymentMethod
                        {
                            PaymentMethod = new PaymentMethod
                            {
                                PaymentMethodPicture = new BlobStorageItem
                                {
                                    BlobName = $"testblob/{Guid.NewGuid()}",
                                }
                            }
                        },
                        new ClientUserPaymentMethod
                        {
                            PaymentMethod = new PaymentMethod
                            {
                                PaymentMethodPicture = new BlobStorageItem
                                {
                                    BlobName = $"testblob/{Guid.NewGuid()}",
                                }
                            }
                        },
                    }
                };

                clientUsers.Add(clientUser);
                clientUsersWithPaymentMethods.Add(clientUser);
            }

            clientUserIndex = clientUsers.Count + 1;

            #endregion

            #region has profile has email has phone

            for (int i = clientUserIndex; i <= clientUserIndex + 20; i++)
            {
                var business = clientBusinesses[radmon.Next(0, clientBusinesses.Count - 1)];

                var clientUser = new ClientUser
                {
                    ClientBusinessId = business.ClientBusinessId,
                    FirstName = $"test first name {i}",
                    LastName = $"test last name {i}",
                    Email = "test@aaa.com",
                    IdentityCardBlobStorageItem = new BlobStorageItem
                    {
                        BlobName = $"testblob/{Guid.NewGuid()}"
                    },
                    ClientUserProfile = new ClientUserProfile
                    {
                        Age = "18",
                        Title = "CEO",
                    },
                    ContactEmails = Enumerable
                    .Range(1, 5).Select(x => new ContactEmail
                    {
                        ContactEmailAddress = "test@test.com",
                        NickName = x.ToString(),
                    })
                    .ToList(),
                    ContactPhoneNumbers = Enumerable
                    .Range(1, 5).Select(x => new ContactPhoneNumber
                    {
                        PhoneNumber = "1111",
                        NickName = x.ToString(),
                    })
                    .ToList(),
                    ClientUserPaymentMethods = new List<ClientUserPaymentMethod>()
                    {
                        new ClientUserPaymentMethod
                        {
                            PaymentMethod = new PaymentMethod
                            {
                                PaymentMethodPicture = new BlobStorageItem
                                {
                                    BlobName = $"testblob/{Guid.NewGuid()}",
                                }
                            }
                        },
                        new ClientUserPaymentMethod
                        {
                            PaymentMethod = new PaymentMethod
                            {
                                PaymentMethodPicture = new BlobStorageItem
                                {
                                    BlobName = $"testblob/{Guid.NewGuid()}",
                                }
                            }
                        },
                    }
                };

                clientUsers.Add(clientUser);
                clientUsersWithPaymentMethods.Add(clientUser);
            }

            clientUserIndex = clientUsers.Count + 1;

            #endregion

            dbContext.ClientUsers.AddRange(clientUsers);

            dbContext.SaveChanges();

            #endregion

            #region seed orders

            var orders = new List<Order>();

            foreach (var clientUser in clientUsersWithPaymentMethods)
            {
                var paymentMethod = clientUser.ClientUserPaymentMethods.ToArray()[radmon.Next(0, clientUser.ClientUserPaymentMethods.Count - 1)];

                var product = productsBasic[radmon.Next(0, productsBasic.Count - 1)];
                var product1 = productsWithLogo
                    .Where(x => x.ProductId != product.ProductId)
                    .ToArray()[radmon.Next(0, productsWithLogo.Count - 1)];
                var product2 = productsWithImage.Where(x => x.ProductId != product.ProductId)
                    .Where(x => x.ProductId != product.ProductId)
                    .Where(x => x.ProductId != product1.ProductId)
                    .ToArray()[radmon.Next(0, productsWithImage.Count - 1)];
                var product3 = productsWithMerchantProvider
                    .Where(x => x.ProductId != product.ProductId)
                    .Where(x => x.ProductId != product1.ProductId)
                    .Where(x => x.ProductId != product2.ProductId)
                    .ToArray()[radmon.Next(0, productsWithMerchantProvider.Count - 1)];

                var orderProducts = new Product[] { product, product1, product2, product3 };

                var order = new Order
                {
                    ClientUserId = clientUser.ClientUserId,
                    ClientUserPaymentMethodId = paymentMethod.ClientUserPaymentMethodId,
                    BillingAddress = new Address
                    {
                        CountryOrRegionId = canada.CountryOrRegionId,
                        AddressLine1 = "123 Magic street",
                        AddressLine2 = "Apt 201",
                        City = "Dream",
                        ZipCode = "00001",
                    },
                    DeliveryAddress = new Address
                    {
                        CountryOrRegionId = canada.CountryOrRegionId,
                        AddressLine1 = "123 Magic street",
                        AddressLine2 = "Apt 201",
                        City = "Dream",
                        ZipCode = "00001",
                    },
                    Timestamp = DateTimeOffset.UtcNow,
                };

                order.OrderProducts = orderProducts
                    .Select(x => new OrderProduct
                    {
                        ProductId = x.ProductId,
                        Order = order,
                        Price = 10m,
                    })
                    .ToList();

                orders.Add(order);
                //dbContext.Attach(order);
            }

            dbContext.AttachRange(orders);

            dbContext.SaveChanges();

            #endregion

            dbContext.DetachAllEntities();
        }
    }
}

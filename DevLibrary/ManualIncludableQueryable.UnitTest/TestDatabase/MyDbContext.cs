using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManualIncludableQueryable.UnitTest.TestDatabase
{
    public partial class MyDbContext : DbContext
    {
        public MyDbContext()
        {

        }

        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
        {

        }

        public virtual DbSet<BlobStorageItem> BlobStorageItems { get; set; }
        public virtual DbSet<ClientUser> ClientUsers { get; set; }
        public virtual DbSet<ClientBusiness> ClientBusinesses { get; set; }
        public virtual DbSet<ClientUserProfile> ClientUserProfiles { get; set; }
        public virtual DbSet<ContactEmail> ContactEmails { get; set; }
        public virtual DbSet<ContactPhoneNumber> ContactPhoneNumbers { get; set; }
        public virtual DbSet<CountryOrRegion> CountryOrRegions { get; set; }
        public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<OrderProduct> OrderProducts { get; set; }
        public virtual DbSet<PaymentMethod> PaymentMethods { get; set; }
        public virtual DbSet<Product> Products { get; set; }
        public virtual DbSet<ProductPrice> ProductPrices { get; set; }
        public virtual DbSet<MerchantProvider> MerchantProviders { get; set; }
        public virtual DbSet<ProductMerchantProvider> ProductMerchantProviders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ClientUserPaymentMethod>()
                .HasOne(x => x.PaymentMethod)
                .WithMany(p => p.ClientUserPaymentMethods)
                .HasForeignKey(d => d.PaymentMethodId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            modelBuilder.Entity<Order>()
                .HasOne(x => x.DeliveryAddress)
                .WithMany(p => p.OrderDeliveryAddressInverseProperties)
                .HasForeignKey(d => d.DeliveryAddressId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            modelBuilder.Entity<Order>()
                .HasOne(x => x.BillingAddress)
                .WithMany(p => p.OrderBillingAddressInverseProperties)
                .HasForeignKey(d => d.BillingAddressId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            modelBuilder.Entity<Order>()
                .HasOne(x => x.ClientUser)
                .WithMany(p => p.Orders)
                .HasForeignKey(d => d.ClientUserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        }

        public IQueryable<ContactEmail> GetAllQueryableContactEmails(bool isTracking = false)
        {
            var query = this.ContactEmails.AsQueryable();

            if (!isTracking)
            {
                query = query.AsNoTracking();
            }

            return query;
        }

        public IQueryable<BlobStorageItem> GetAllQueryableBlobStorageItems(bool isTracking = false)
        {
            var query = this.BlobStorageItems.AsQueryable();

            if (!isTracking)
            {
                query = query.AsNoTracking();
            }

            return query;
        }

        public IQueryable<PaymentMethod> GetAllQueryablePaymentMethod(bool isTracking = false)
        {
            var query = this.PaymentMethods.AsQueryable();

            if (!isTracking)
            {
                query = query.AsNoTracking();
            }

            return query;
        }

        public IQueryable<Product> GetAllQueryableProducts(bool isTracking = false)
        {
            var query = this.Products.AsQueryable();

            if (!isTracking)
            {
                query = query.AsNoTracking();
            }

            return query;
        }

        public IQueryable<ProductPrice> GetAllQueryableProductPrices(bool isTracking = false)
        {
            var query = this.ProductPrices.AsQueryable();

            if (!isTracking)
            {
                query = query.AsNoTracking();
            }

            return query;
        }

        public IQueryable<OrderProduct> GetAllQueryableOrderProducts(bool isTracking = false)
        {
            var query = this.OrderProducts.AsQueryable();

            if (!isTracking)
            {
                query = query.AsNoTracking();
            }

            return query;
        }

        public IQueryable<Order> GetAllQueryableOrders(bool isTracking = false)
        {
            var query = this.Orders.AsQueryable();

            if (!isTracking)
            {
                query = query.AsNoTracking();
            }

            return query;
        }

        public IQueryable<ClientBusiness> GetAllQueryableClientBusinesses(bool isTracking = false)
        {
            var query = this.ClientBusinesses.AsQueryable();

            if (!isTracking)
            {
                query = query.AsNoTracking();
            }

            return query;
        }

        public IQueryable<ClientUser> GetAllQueryableClientUsers(bool isTracking = false)
        {
            var query = this.ClientUsers.AsQueryable();

            if (!isTracking)
            {
                query = query.AsNoTracking();
            }

            return query;
        }

        public IQueryable<ClientUserProfile> GetAllQueryableClientUserProfiles(bool isTracking = false)
        {
            var query = this.ClientUserProfiles.AsQueryable();

            if (!isTracking)
            {
                query = query.AsNoTracking();
            }

            return query;
        }

        public void DetachAllEntities()
        {
            this.ChangeTracker.Clear();
            //var changedEntries = this.ChangeTracker.Entries().ToList();

            //foreach (var entry in changedEntries)
            //{
            //    entry.State = EntityState.Detached;
            //}
        }
    }
}

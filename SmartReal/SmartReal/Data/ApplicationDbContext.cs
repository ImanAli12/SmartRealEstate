using Microsoft.EntityFrameworkCore;
using RealEstateWebApp.Models;

namespace SmartReal.Data
{

        public class ApplicationDbContext : DbContext
        {
            public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);
                
            }
            public DbSet<City> Cities => Set<City>();
            public DbSet<Neighborhood> Neighborhoods => Set<Neighborhood>();
            public DbSet<PropertyType> PropertyTypes => Set<PropertyType>();
            public DbSet<Property> Properties => Set<Property>();
            public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();
            public DbSet<Favorite> Favorites => Set<Favorite>();
            public DbSet<Cluster> Clusters => Set<Cluster>();

      

        }

    }



using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LOSTBOOKS.Models;


namespace LOSTBOOKS.Data
{
    public class LOSTBOOKSContext : DbContext
    {
        public LOSTBOOKSContext(DbContextOptions<LOSTBOOKSContext> options)
            : base(options)
        {
        }

        public DbSet<LOSTBOOKS.Models.Product> Products { get; set; } = default!;
        public DbSet<LOSTBOOKS.Models.Service> Services { get; set; } = default!;
        public DbSet<LOSTBOOKS.Models.Consignor> Consignors { get; set; } = default!;
        public DbSet<LOSTBOOKS.Models.Merchandise> Merchandises { get; set; } = default!;
        public DbSet<LOSTBOOKS.Models.Book> Books { get; set; } = default!;

        public DbSet<History> Histories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Book → Consignor (USING ID, NOT NAME)
            modelBuilder.Entity<Book>()
                .HasOne(b => b.Consignor)
                .WithMany(c => c.Books)
                .HasForeignKey(b => b.ConsignorID);

            // Merchandise → Consignor (USING ID)
            modelBuilder.Entity<Merchandise>()
                .HasOne(m => m.Consignor)
                .WithMany(c => c.Merchandises)
                .HasForeignKey(m => m.ConsignorID);
        }
    }
}
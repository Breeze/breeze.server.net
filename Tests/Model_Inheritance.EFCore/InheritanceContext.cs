using Microsoft.EntityFrameworkCore;

namespace Inheritance.Models
{
    public partial class InheritanceContext : DbContext
    {
        public InheritanceContext()
        {
        }

        public InheritanceContext(DbContextOptions<InheritanceContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                //optionsBuilder.UseSqlServer("Data Source=.;Initial Catalog=InheritanceContext;Integrated Security=True;MultipleActiveResultSets=True");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // TPHs
            modelBuilder.Entity<BillingDetailTPH>()
                .HasDiscriminator<string>("BillingDetailType")
                .HasValue<BankAccountTPH>("BA")
                .HasValue<CreditCardTPH>("CC");

            modelBuilder.Entity<BankAccountTPH>(x => x.HasBaseType<BillingDetailTPH>());
            modelBuilder.Entity<CreditCardTPH>(x => x.HasBaseType<BillingDetailTPH>());

            // TPTs - Not supported in EFCore 3
            modelBuilder.Entity<BankAccountTPT>().ToTable("BankAccountTPTs");
            modelBuilder.Entity<CreditCardTPT>().ToTable("CreditCardTPTs");

            // TPCs - Not supported in EFCore 3 or 5
            //modelBuilder.Entity<BillingDetailTPC>();

            modelBuilder.Entity<BankAccountTPC>(m =>
            {
                m.ToTable("BankAccountTPCs");
            });

            modelBuilder.Entity<CreditCardTPC>(m =>
            {
                m.ToTable("CreditCardsTPCs");
            });

            modelBuilder.Entity<AccountType>()
                        .Property(p => p.Id)
                        .ValueGeneratedNever();

        }

        public DbSet<AccountType> AccountTypes { get; set; }

        public DbSet<BillingDetailTPH> BillingDetailTPHs { get; set; }
#if NET5_0_OR_GREATER
        // TPT supported in EFCore 5 but not 3
        public DbSet<BillingDetailTPT> BillingDetailTPTs { get; set; }
#endif
        // TPC not supported in EFCore 3 or 5
        //public DbSet<BillingDetailTPC> BillingDetailTPCs { get; set; }

        // Public for initializer; should not expose to client in Web API controller
        public DbSet<DepositTPH> DepositTPHs { get; set; }
        public DbSet<DepositTPT> DepositTPTs { get; set; }
        public DbSet<DepositTPC> DepositTPCs { get; set; }

        // Added as a test to see if these would show up in EdmModel ( they don't !). 
        public DbSet<BankAccountTPH> BankAccountTPHs { get; set; }
        public DbSet<BankAccountTPT> BankAccountTPTs { get; set; }
        public DbSet<BankAccountTPC> BankAccountTPCs { get; set; }

        public DbSet<CreditCardTPH> CreditCardTPHs { get; set; }
        public DbSet<CreditCardTPT> CreditCardTPTs { get; set; }
        public DbSet<CreditCardTPC> CreditCardTPCs { get; set; }

    }
}

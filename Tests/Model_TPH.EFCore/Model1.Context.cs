
namespace ProduceTPH
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class ProduceTPHContext : DbContext
    {
        public ProduceTPHContext()
            : base("name=ProduceTPHContext")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public DbSet<ItemOfProduce> ItemsOfProduce { get; set; }
    }
}

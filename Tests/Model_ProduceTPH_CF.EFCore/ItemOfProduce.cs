using System;

namespace ProduceTPH
{
    public abstract partial class ItemOfProduce
    {
        public ItemOfProduce()
        {
            this.RowVersion = 0;
        }
    
        public Guid Id { get; set; }
        public string ItemNumber { get; set; }
        public decimal? UnitPrice { get; set; }
        public string QuantityPerUnit { get; set; }
        public short? UnitsInStock { get; set; }
        public short UnitsOnOrder { get; set; }
        public int? RowVersion { get; set; }
    }
}

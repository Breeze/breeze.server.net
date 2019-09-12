namespace ProduceTPH
{
    using System;
    using System.Collections.Generic;
    
    public abstract partial class ItemOfProduce
    {
        public ItemOfProduce()
        {
            this.RowVersion = 0;
        }
    
        public System.Guid Id { get; set; }
        public string ItemNumber { get; set; }
        public Nullable<decimal> UnitPrice { get; set; }
        public string QuantityPerUnit { get; set; }
        public Nullable<short> UnitsInStock { get; set; }
        public short UnitsOnOrder { get; set; }
        public Nullable<int> RowVersion { get; set; }
    }
}

namespace Models.NorthwindIB.NH
{
    using System;
    using System.Collections.Generic;

    public partial class ProductMulti
    {
        public virtual int ProductID { get; set; }
        public virtual string ProductName { get; set; }
        public virtual Nullable<int> SupplierID { get; set; }
        public virtual Nullable<int> CategoryID { get; set; }
        public virtual Nullable<int> CategoryID2 { get; set; }
        public virtual string QuantityPerUnit { get; set; }
        public virtual Nullable<decimal> UnitPrice { get; set; }
        public virtual Nullable<short> UnitsInStock { get; set; }
        public virtual Nullable<short> UnitsOnOrder { get; set; }
        public virtual Nullable<short> ReorderLevel { get; set; }
        //public virtual bool Discontinued { get; set; }
        public virtual int RowVersion { get; set; }

        public virtual CategoryMulti Category { get; set; }
        public virtual CategoryMulti Category2 { get; set; }
        public virtual Supplier Supplier { get; set; }
    }
}

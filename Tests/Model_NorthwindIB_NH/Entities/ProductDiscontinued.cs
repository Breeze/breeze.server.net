namespace Models.NorthwindIB.NH
{
    using System;
    using System.Collections.Generic;

    public class ProductDiscontinued : ProductMulti
    {
        public virtual Nullable<System.DateTime> DiscontinuedDate { get; set; }
        public virtual Nullable<int> AlternateSupplierID { get; set; }

        public virtual Supplier AlternateSupplier { get; set; }

    }
}

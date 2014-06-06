namespace Models.NorthwindIB.NH
{
    using System;
    using System.Collections.Generic;

    public partial class CategoryMulti
    {
        public CategoryMulti()
        {
            this.Products = new HashSet<ProductMulti>();
        }

        public virtual int CategoryID { get; set; }
        public virtual string CategoryName { get; set; }
        public virtual string Description { get; set; }
        public virtual int RowVersion { get; set; }

        public virtual ICollection<ProductMulti> Products { get; set; }
    }
}

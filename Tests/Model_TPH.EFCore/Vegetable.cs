
namespace ProduceTPH
{
    using System;
    using System.Collections.Generic;
    
    public partial class Vegetable : ItemOfProduce
    {
        public string Name { get; set; }
        public string USDACategory { get; set; }
        public Nullable<bool> AboveGround { get; set; }
    }
}

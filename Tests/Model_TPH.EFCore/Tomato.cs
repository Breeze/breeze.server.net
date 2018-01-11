
namespace ProduceTPH
{
    using System;
    using System.Collections.Generic;
    
    public partial class Tomato : Vegetable
    {
        public string Variety { get; set; }
        public string Description { get; set; }
        public byte[] Photo { get; set; }
        public Nullable<bool> Determinate { get; set; }
    }
}

namespace ProduceTPH
{
    using System;
    using System.Collections.Generic;
    
    public partial class WhitePotato : Vegetable
    {
        public string Variety { get; set; }
        public string Description { get; set; }
        public byte[] Photo { get; set; }
        public string Eyes { get; set; }
        public string SkinColor { get; set; }
        public string PrimaryUses { get; set; }
    }
}

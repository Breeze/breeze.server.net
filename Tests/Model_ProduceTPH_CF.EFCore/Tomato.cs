
namespace ProduceTPH
{
    public partial class Tomato : Vegetable
    {
        public string Variety { get; set; }
        public string Description { get; set; }
        public byte[] Photo { get; set; }
        public bool? Determinate { get; set; }
    }
}

namespace PopcornCoreExample.Models
{
    public class Car
    {
        public string Model { get; set; }
        public string Make { get; set; }
        public int Year { get; set; }
        public bool Insured { get; set; }

        public enum Colors
        {
            Black,
            Red,
            Blue,
            Gray,
            White,
            Yellow,
        }
        public Colors Color { get; set; }

        public string User { get; set; }
    }
}

namespace SchoolFeesSystem.Models
{
    public class Class
    {
        public int ClassId { get; set; }

        public string? ClassName { get; set; }

        public ICollection<Student>? Students { get; set; }

        public virtual ICollection<Fee>? Fees { get; set; } = new List<Fee>();
    }
}

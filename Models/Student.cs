using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolFeesSystem.Models
{
    public class Student
    {
        public int StudentId { get; set; }
        public string FullName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; }

        public int ClassId { get; set; }

        // Use '?' to tell the system this isn't required during form submission
        public virtual Class? Class { get; set; }

        public string? PhotoPath { get; set; }

        [NotMapped]
        public IFormFile? PhotoFile { get; set; }
        public bool IsActive { get; set; } = true;

        // Initialize as nullable lists
        public virtual List<Guardian>? Guardians { get; set; } = new List<Guardian>();
        public virtual ICollection<Payment>? Payments { get; set; } = new List<Payment>();
    }
}
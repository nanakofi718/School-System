namespace SchoolFeesSystem.Models
{
    public class Guardian
    {
        public int GuardianId { get; set; }

        public int StudentId { get; set; }
        // FIX: Add ? to avoid validation errors for the full Student object
        public virtual Student? Student { get; set; }

        public string FullName { get; set; }

        // FIX: Add ? to these strings so they are optional during registration
        public string? Phone { get; set; }
        public string? WhatsApp { get; set; }
        public string? Email { get; set; }

        public int? UserId { get; set; }
        // FIX: Add ? to avoid validation errors for the full User object
        public virtual User? User { get; set; }
    }
}
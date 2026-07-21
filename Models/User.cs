namespace SchoolFeesSystem.Models
{
    public class User
    {

        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }

        public int RoleId { get; set; }
        public Role Role { get; set; }

        public bool IsActive { get; set; } = true;
        //public string Password { get; internal set; }

        public virtual Guardian Guardian { get; set; }

        public int? ClassId { get; set; }
        public virtual Class Class { get; set; }
        public bool IsFirstLogin { get; set; } = true; // Default to true for new accounts
    }
}

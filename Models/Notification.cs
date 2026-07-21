namespace SchoolFeesSystem.Models
{
    public class Notification
    {
        public int NotificationId { get; set; }

        public int StudentId { get; set; }
        public Student Student { get; set; }

        public string MessageType { get; set; }
        public string MessageText { get; set; }
        public DateTime SentDate { get; set; } = DateTime.Now;
        public string Status { get; set; }
    }
}

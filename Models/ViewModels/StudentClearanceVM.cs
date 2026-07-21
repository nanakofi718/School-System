namespace SchoolFeesSystem.Models.ViewModels
{
    public class StudentClearanceVM
    {
        public int StudentId { get; set; }
        public string FullName { get; set; }
        public string ClassName { get; set; }
        public decimal TotalFee { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal Balance => TotalFee - TotalPaid;

        public string Status => Balance <= 0 ? "CLEARED" : "OWING";
    }

}

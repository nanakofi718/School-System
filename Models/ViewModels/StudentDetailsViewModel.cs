using SchoolFeesSystem.Models;
using System.Collections.Generic;

namespace SchoolFeesSystem.ViewModels
{
    public class StudentDetailsViewModel
    {
        public Student? Student { get; set; }
        public List<Fee>? Fees { get; set; }
    }
}

using System.Collections.Generic;

namespace ClinicDentServer.RequestCustomAnswers
{
    public class WeekMoneySummaryRequestAnswer
    {
        public List<int> StagesPaidSum { get; set; } = new List<int>();
        public List<int> StagesPriceSum { get; set; } = new List<int>();
        public List<int> StagesExpensesSum { get; set; } = new List<int>();
        public List<int> DoctorIds { get; set; } = new List<int>();

    }
}

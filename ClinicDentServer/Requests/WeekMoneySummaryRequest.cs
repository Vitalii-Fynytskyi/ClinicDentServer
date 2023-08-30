using System;

namespace ClinicDentServer.Requests
{
    public class WeekMoneySummaryRequest
    {
        public int CabinetId { get; set; }
        public DateTime AnySunday { get; set; }
    }
}

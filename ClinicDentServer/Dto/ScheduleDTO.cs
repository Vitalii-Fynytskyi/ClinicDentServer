using ClinicDentServer.Models;
using System;
using System.Collections.Generic;

namespace ClinicDentServer.Dto
{
    public class ScheduleDTO
    {
        public ScheduleDTO()
        {
            StagesPaidSum = new List<int>();
            StagesPriceSum = new List<int>();
            StagesExpensesSum = new List<int>();
            DoctorIds = new List<int>();
        }
        public ScheduleDTO(Schedule s) : this()
        {
            Id = s.Id;
            StartDatetime = s.StartDatetime.ToString("yyyy-MM-dd HH:mm");
            EndDatetime = s.EndDatetime.ToString("yyyy-MM-dd HH:mm");

            Comment = s.Comment;
            PatientId = s.PatientId;
            DoctorId = s.DoctorId;
            PatientName = s.Patient.Name;
            CabinetName = s.Cabinet.CabinetName;
            CabinetId = s.Cabinet.Id;

            State = s.State;

        }
        public ScheduleDTO(string id, string startDateTime, string endDateTime, string comment, string patientId, string doctorId, string patientName, string cabinetId, string cabinetName, string state):this()
        {
            Id = Int32.Parse(id);
            StartDatetime = startDateTime;
            EndDatetime = endDateTime;
            Comment = comment;
            if (patientId == "<null>")
            {
                PatientId = null;
            }
            else
            {
                PatientId = Int32.Parse(patientId);
            }
            DoctorId = Int32.Parse(doctorId);
            PatientName = patientName;
            CabinetId = Int32.Parse(cabinetId);
            CabinetName = cabinetName;
            State = (SchedulePatientState)Int32.Parse(state);
        }
        public int Id { get; set; }
        public string StartDatetime { get; set; }
        public string EndDatetime { get; set; }

        public string Comment { get; set; }

        public int? PatientId { get; set; }

        public int DoctorId { get; set; }
        public string PatientName { get; set; }
        public int CabinetId { get; set; }

        public string CabinetName { get; set; }
        public SchedulePatientState State { get; set; } //0 - unknown, 1 - will appear, 2 - refused

        public ScheduleIsSentViaMessagetState StagesSentViaMessagerState { get; set; }
        public List<int> StagesPaidSum { get; set; }
        public List<int> StagesPriceSum { get; set; }
        public List<int> StagesExpensesSum { get; set; }
        public List<int> DoctorIds { get; set; }
    }
}

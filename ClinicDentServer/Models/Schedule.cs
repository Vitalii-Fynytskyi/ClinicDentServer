using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicDentServer.Models
{
    public enum SchedulePatientState
    {
        Unknown = 0, WillAppear = 1, Refused = 2
    }
    public class Schedule
    {
        public Schedule()
        {

        }
        public Schedule(ScheduleDTO scheduleDTO)
        {
            Id = scheduleDTO.Id;
            DateTime dt;
            if (DateTime.TryParse(scheduleDTO.StartDatetime, out dt) == false)
            {
                dt = new DateTime(2500, 1, 1);
            }
            StartDatetime = dt;
            if (DateTime.TryParse(scheduleDTO.EndDatetime, out dt) == false)
            {
                dt = new DateTime(2500, 1, 1);
            }
            EndDatetime = dt;
            Comment = scheduleDTO.Comment;
            PatientId = scheduleDTO.PatientId;
            DoctorId = scheduleDTO.DoctorId;
            CabinetId = scheduleDTO.CabinetId;
            State = scheduleDTO.State;

        }
        public int Id { get; set; }
        public DateTime StartDatetime { get; set; }
        public DateTime EndDatetime { get; set; }

        public string Comment { get; set; }
        public virtual Patient Patient { get; set; }
        public virtual Doctor Doctor { get; set; }
        public virtual Cabinet Cabinet { get; set; }

        public int? PatientId { get; set; }
        public int DoctorId { get; set; }
        public int CabinetId { get; set; }
        public SchedulePatientState State { get; set; } //0 - unknown, 1 - will appear, 2 - refused
        public virtual ICollection<Stage> Stages { get; set; }

    }
    public class ScheduleDTO
    {
        public ScheduleDTO() { }
        public ScheduleDTO(Schedule s)
        {
            Id = s.Id;
            StartDatetime = s.StartDatetime.ToString("yyyy-MM-dd HH:mm");
            EndDatetime = s.EndDatetime.ToString("yyyy-MM-dd HH:mm");

            Comment = s.Comment;
            PatientId = s.PatientId;
            DoctorId= s.DoctorId;
            PatientName = s.Patient.Name;
            CabinetName = s.Cabinet.CabinetName;
            CabinetId = s.Cabinet.Id;

            State = s.State;
        }
        public ScheduleDTO(string id, string startDateTime, string endDateTime, string comment, string patientId, string doctorId, string patientName, string cabinetId, string cabinetName, string state)
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
    }
}

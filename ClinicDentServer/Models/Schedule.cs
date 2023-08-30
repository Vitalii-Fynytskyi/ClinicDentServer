using ClinicDentServer.Dto;
using System;
using System.Collections.Generic;

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

    }
    
    public enum ScheduleIsSentViaMessagetState
    {
        NoStages=0,
        CanSend=1,
        AllSent=2
    }
}

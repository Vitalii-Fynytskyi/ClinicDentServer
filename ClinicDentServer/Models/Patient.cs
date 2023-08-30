using System.Collections.Generic;

namespace ClinicDentServer.Models
{
    public class Patient
    {
        
        public int Id { get; set; }
        public string Name { get; set; }
        public string Gender { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string Birthdate { get; set; }
        public string Illness { get; set; }
        public string Notes { get; set; }
        public string RegisterDate { get; set; }
        public string Statuses { get; set; }
        public string CurePlan { get; set; }

        public byte[] ImageBytes { get; set; }
        public virtual ICollection<Stage> Stages { get; private set; }
        public virtual ICollection<Schedule> Schedules { get; private set; }

        public Patient()
        {
            Stages = new List<Stage>();
        }
        public Patient(PatientDTO p)
        {
            Id = p.Id;
            Name = p.Name;
            Gender = p.Gender;
            Phone = p.Phone;
            Address = p.Address;
            Birthdate = p.Birthdate;
            Illness = p.Illness;
            Notes = p.Notes;
            RegisterDate = p.RegisterDate;
            Statuses = p.Statuses;
            ImageBytes = p.ImageBytes;
            CurePlan= p.CurePlan;
            Stages = new List<Stage>();
            Schedules = new List<Schedule>();
        }
    }
    public class PatientDTO
    {
        public PatientDTO() { }
        public PatientDTO(Patient p)
        {
            Id = p.Id;
            Name = p.Name;
            Gender = p.Gender;
            Phone = p.Phone;
            Address = p.Address;
            Birthdate = p.Birthdate;
            Illness = p.Illness;
            Notes = p.Notes;
            CurePlan = p.CurePlan;

            RegisterDate = p.RegisterDate;
            Statuses = p.Statuses;
            ImageBytes = p.ImageBytes;
        }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Gender { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string Birthdate { get; set; }
        public string Illness { get; set; }
        public string Notes { get; set; }
        public string RegisterDate { get; set; }
        public string CurePlan { get; set; }

        public string Statuses { get; set; }
        public byte[] ImageBytes { get; set; }
    }
    public class PatientsToClient
    {
        public PatientDTO[] Patients { get; set; }
        public int CountPages { get; set; }
    }
}

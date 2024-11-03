using System.Collections.Generic;

namespace ClinicDentServer.Models
{
    public class Doctor :BaseModel
    {
        public string Email { get; set; }

        public string Password { get; set; }

        public string Name { get; set; }
        public virtual IEnumerable<Stage> Stages { get; set; }
        public virtual IEnumerable<Image> Images { get; set; }

        public virtual IEnumerable<Schedule> Schedules { get; set; }


    }
    public class DoctorDTO
    {
        public DoctorDTO() { }
        public DoctorDTO(Doctor d)
        {
            Id = d.Id;
            Name = d.Name;
        }
        public int Id { get; set; }

        public string Name { get; set; }

        public string EncodedJwt { get; set; }
    }
}

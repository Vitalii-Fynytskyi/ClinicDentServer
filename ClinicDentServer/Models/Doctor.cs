using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicDentServer.Models
{
    public class Doctor
    {
        public int Id { get; set; }
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

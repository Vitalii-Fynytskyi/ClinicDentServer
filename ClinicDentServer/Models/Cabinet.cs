using System.Collections.Generic;

namespace ClinicDentServer.Models
{
    public class Cabinet:BaseModel
    {
        public string CabinetName { get; set; }
        public virtual ICollection<Schedule> Schedules { get; set; }
        public virtual ICollection<CabinetComment> CabinetComments { get; set; }

    }
}

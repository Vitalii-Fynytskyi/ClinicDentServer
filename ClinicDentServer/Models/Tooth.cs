using System.Collections.Generic;

namespace ClinicDentServer.Models
{
    public class Tooth
    {
        public Tooth() { }
        public Tooth(byte toothNumber) { Id =  toothNumber; }
        public byte Id { get; set; }
        public virtual ICollection<Stage> Stages { get; set; }
    }
}

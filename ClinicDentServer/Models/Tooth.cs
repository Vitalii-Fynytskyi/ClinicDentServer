using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicDentServer.Models
{
    public class Tooth:BaseModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Column("Id")]
        public new byte Id
        {
            get { return (byte)base.Id; }
            set { base.Id = value; }
        }
        public Tooth() { }
        public Tooth(byte toothNumber) { Id =  toothNumber; }
        public virtual ICollection<Stage> Stages { get; set; }
    }
}

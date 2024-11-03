using System;

namespace ClinicDentServer.Models
{
    public class CabinetComment:BaseModel
    {
        public virtual Cabinet Cabinet { get; set; }
        public int CabinetId { get; set; }
        public DateTime Date { get; set; }
        public string CommentText { get; set; }

    }
}

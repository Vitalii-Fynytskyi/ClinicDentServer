using System.Collections.Generic;

namespace ClinicDentServer.RequestCustomAnswers
{
    public class PutStagesRequestAnswer
    {
        public List<int> ConflictedStagesIds { get; set; }
        public string NewLastModifiedDateTime { get; set; }
    }
}

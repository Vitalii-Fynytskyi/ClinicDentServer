namespace ClinicDentServer.Requests
{
    public class ChangeCurePlanRequest
    {
        public int PatientId { get; set; }
        public string CurePlan { get; set; }
    }
}
namespace ClinicDentServer.Models
{
    public class ToothUnderObservation
    {
        public int Id { get; set; }
        public int StageId { get; set; }
        public Stage Stage { get; set; } = null!;
        public string ToothName { get; set; }
        public string ObservationDescription { get; set;}
        public ToothUnderObservation() { }
        public ToothUnderObservation(ToothUnderObservationDTO d)
        {
            Id = d.Id;
            StageId = d.StageId;
            ToothName = d.ToothName;
            ObservationDescription = d.ObservationDescription;
        }
    }
    public class ToothUnderObservationDTO
    {
        public int Id { get; set; }
        public string PatientName { get; set; }
        public int StageId { get; set; }
        public string ToothName { get; set; }
        public string ObservationDescription { get; set; }
        public ToothUnderObservationDTO() { }
        public ToothUnderObservationDTO(ToothUnderObservation d)
        {
            Id = d.Id;
            StageId = d.StageId;
            ToothName = d.ToothName;
            ObservationDescription = d.ObservationDescription;
        }
        public ToothUnderObservationDTO(ToothUnderObservation d, string patientName):this(d)
        {
            PatientName = patientName;
        }
    }
}

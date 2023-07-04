namespace ClinicDentServer.Models
{
    public class DoctorUser
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }
        public string ConnectionString { get; set; }
        public int InternalId { get; set; }
    }
}

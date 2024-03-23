using System.ComponentModel.DataAnnotations;

namespace ClinicDentServer.ViewModels
{
    public class LoginModel
    {
        public string Email { get; set; }
        [DataType(DataType.Password)]
        public string Password { get; set; }
        public string Tenant { get; set; }

    }
}

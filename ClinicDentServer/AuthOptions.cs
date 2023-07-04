using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ClinicDentServer
{
    public class AuthOptions
    {
        public const string ISSUER = "ClinicDentServer";
        public const string AUDIENCE = "ClinicDentClient";
        public const string KEY = "mysupersecret_secretkey!123";
        public const int LIFETIME = 1440;
        public static SymmetricSecurityKey GetSymmetricSecurityKey()
        {
            return new SymmetricSecurityKey(Encoding.ASCII.GetBytes(KEY));
        }
    }
}

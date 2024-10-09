using ClinicDentServer.Exceptions;
using ClinicDentServer.Models;
using ClinicDentServer.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ClinicDentServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        [HttpPost("login")]
        public async Task<ActionResult<DoctorDTO>> Login(LoginModel model)
        {
            using(ClinicDentUsersContext usersDb = new ClinicDentUsersContext(Startup.Configuration["ConnectionStrings:AllUsers"]))
            {
                DoctorUser doctor = await usersDb.Doctors.AsNoTracking().FirstOrDefaultAsync(d => d.Email == model.Email && d.Password == model.Password && d.ConnectionString == model.Tenant);
                if (doctor != null)
                {
                    return GetDoctorDTO(doctor);
                }
                throw new NotFoundException("Invalid username or password.");
            }
        }
        [HttpGet("tenantNames")]
        public IEnumerable<string> GetTenantConnectionStrings()
        {
            var connectionStringsSection = Startup.Configuration.GetSection("ConnectionStrings");

            // Get all connection strings as a dictionary
            var connectionStrings = connectionStringsSection.Get<Dictionary<string, string>>();

            // Remove the "General" connection string and return the remaining
            connectionStrings.Remove("AllUsers");

            return connectionStrings.Keys;
        }
        [HttpGet("apiVersion")]
        [Produces("text/plain")]
        public ActionResult<string> GetApiVersion()
        {
            return Ok(Startup.Configuration.GetValue<string>("RequiredClientVersion"));
        }
        
        private ActionResult<DoctorDTO> GetDoctorDTO(DoctorUser doctorUser)
        {
            using(ClinicContext clinicContext = new ClinicContext(Startup.Configuration["ConnectionStrings:" + doctorUser.ConnectionString]))
            {
                Doctor doctor = clinicContext.Doctors.Find(doctorUser.InternalId);
                var identity = GetIdentity(doctorUser);
                var now = DateTime.UtcNow;
                var jwt = new JwtSecurityToken(
                        issuer: AuthOptions.ISSUER,
                        audience: AuthOptions.AUDIENCE,
                        notBefore: now,
                        claims: identity.Claims,
                        expires: now.Add(TimeSpan.FromMinutes(AuthOptions.LIFETIME)),
                        signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));
                var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);
                DoctorDTO doctorDTO = new DoctorDTO(doctor);
                doctorDTO.EncodedJwt = encodedJwt;
                return Ok(doctorDTO);
            }
            
        }
        private ClaimsIdentity GetIdentity(DoctorUser doctor)
        {
            var claims = new List<Claim>
                {
                    new Claim(ClaimsIdentity.DefaultNameClaimType, doctor.Email),
                    new Claim(ClaimsIdentity.DefaultRoleClaimType, "Doctor"),
                    new Claim("ConnectionString",Startup.Configuration["ConnectionStrings:" + doctor.ConnectionString])
                };
            ClaimsIdentity claimsIdentity =
            new ClaimsIdentity(claims, "Token", ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);
            return claimsIdentity;
        }
    }
}

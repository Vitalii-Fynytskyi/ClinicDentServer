using ClinicDentServer.Models;
using ClinicDentServer.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            ClinicDentUsersContext usersDb = new ClinicDentUsersContext(Startup.Configuration["ConnectionStrings:AllUsers"]);

            DoctorUser doctor = await usersDb.Doctors.FirstOrDefaultAsync(d => d.Email == model.Email && d.Password == model.Password);
            if (doctor != null)
            {
                usersDb.Dispose();
                return GetDoctorDTO(doctor);
            }
            usersDb.Dispose();
            return BadRequest(new { errorText = "Invalid username or password." });
        }
        //[HttpPost("register")]
        //public async Task<ActionResult<DoctorDTO>> Register(RegisterModel model)
        //{
        //    ClinicDentUsersContext usersDb = new ClinicDentUsersContext(Startup.Configuration["ConnectionStrings:AllUsers"]);

        //    DoctorUser doctor = await usersDb.Doctors.FirstOrDefaultAsync(u => u.Email == model.Email);
        //    if (doctor == null)
        //    {
        //        doctor = new Doctor
        //        {
        //            Email = model.Email,
        //            Password = model.Password,
        //            Name = model.Name
        //        };
        //        db.Doctors.Add(doctor);
        //        await db.SaveChangesAsync();
        //        return GetDoctorDTO(doctor);

        //    }
        //    return BadRequest(new { errorText = "User with such email already exists." });
        //}
        private ActionResult<DoctorDTO> GetDoctorDTO(DoctorUser doctorUser)
        {
            ClinicContext clinicContext = new ClinicContext(Startup.Configuration["ConnectionStrings:" + doctorUser.ConnectionString]);
            Doctor doctor = clinicContext.Doctors.Find(doctorUser.InternalId);
            var identity = GetIdentity(doctorUser);
            var now = DateTime.UtcNow;
            // створюєм JWT-токен
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
            clinicContext.Dispose();
            return Ok(doctorDTO);
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

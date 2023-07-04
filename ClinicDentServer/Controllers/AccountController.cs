using Microsoft.AspNetCore.Http;
using ClinicDentServer.ViewModels;
using ClinicDentServer.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;

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
        [HttpGet("test")]
        public async Task<ActionResult<string>> TestRequest()
        {
            Process process = new Process();
            // Configure the process using the StartInfo properties.
            process.StartInfo.FileName = "C:\\AcumaticaReportLauncher\\AcumaticaReportLauncher.exe";
            process.StartInfo.Arguments = $"ACTUAL 032018 032019";
            process.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
            bool isStarted = process.Start();

            process.WaitForExit();// Waits here for the process to exit.
            await Task.CompletedTask;
            return Ok("Lalala");
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

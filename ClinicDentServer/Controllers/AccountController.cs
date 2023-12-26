using ClinicDentServer.Exceptions;
using ClinicDentServer.Models;
using ClinicDentServer.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using XRayMLApp;

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
                DoctorUser doctor = await usersDb.Doctors.AsNoTracking().FirstOrDefaultAsync(d => d.Email == model.Email && d.Password == model.Password);
                if (doctor != null)
                {
                    return GetDoctorDTO(doctor);
                }
                throw new NotFoundException("Invalid username or password.");
            }
        }
        [HttpGet("createImages")]
        public async Task<ActionResult> CreateImages()
        {
            string outputDirectory = "C:\\Projects\\images\\";
            int batchSize = 100;
            int processed = 0;

            using (ClinicContext db = new ClinicContext(Startup.Configuration["ConnectionStrings:ClinicDent"]))
            {
                // Get the total number of images to process
                int totalImages = await db.Images.CountAsync();

                while (processed < totalImages)
                {
                    // Get the next batch of image bytes
                    List<byte[]> imagesBytes = await db.Images.OrderBy(i => i.Id)
                                                       .Select(i => i.OriginalBytes)
                                                       .Skip(processed)
                                                       .Take(batchSize)
                                                       .ToListAsync();

                    // Process each image in the current batch
                    for (int i = 0; i < imagesBytes.Count; i++)
                    {
                        string imageExtension = GetImageExtension(imagesBytes[i]);
                        if (imageExtension == null) { continue; } // Skip file if can't determine extension
                        string filePath = Path.Combine(outputDirectory, $"{processed + i}{imageExtension}");
                        System.IO.File.WriteAllBytes(filePath, imagesBytes[i]);
                    }

                    // Update the count of processed images
                    processed += imagesBytes.Count;
                }
            }
            return Ok();
        }
        [HttpGet("markXRay")]
        public async Task<ActionResult> MarkXRay()
        {
            int batchSize = 100;
            int imagesRead = 0;

            using (ClinicContext db = new ClinicContext(Startup.Configuration["ConnectionStrings:ClinicDent"]))
            {
                // Get the total number of images to process
                int totalImages = await db.Images.CountAsync();

                while (imagesRead < totalImages)
                {
                    // Get the next batch of image bytes
                    List<Models.Image> images = db.Images.OrderBy(i => i.Id)
                                                       .Skip(imagesRead)
                                                       .Take(batchSize)
                                                       .ToList();
                    imagesRead += images.Count;

                    // Process each image in the current batch
                    for (int i = 0; i < images.Count; i++)
                    {
                        XRayMLModel.ModelInput sampleData = new XRayMLModel.ModelInput()
                        {
                            ImageSource = images[i].OriginalBytes,
                        };

                        //Load model and predict output
                        XRayMLModel.ModelOutput result = XRayMLModel.Predict(sampleData);
                        if (result.Score[0] > result.Score[1])
                        {
                            images[i].IsXRay = false;
                        }
                        else if (result.Score[1] > result.Score[0])
                        {
                            images[i].IsXRay = true;
                        }
                    }
                    db.SaveChanges();
                }
            }
            return Ok();
        }
        [HttpGet("markXRay2/{startId}/{endId}")]
        public ActionResult MarkXRay2(int startId, int endId)
        {

            using (ClinicContext db = new ClinicContext(Startup.Configuration["ConnectionStrings:ClinicDent"]))
            {
                List<Models.Image> images = db.Images.Where(i => i.Id >= startId && i.Id<=endId).OrderBy(i => i.Id).ToList();

                // Process each image in the current batch
                for (int i = 0; i < images.Count; i++)
                {
                    XRayMLModel.ModelInput sampleData = new XRayMLModel.ModelInput()
                    {
                        ImageSource = images[i].OriginalBytes,
                    };

                    //Load model and predict output
                    XRayMLModel.ModelOutput result = XRayMLModel.Predict(sampleData);
                    if (result.Score[0] > result.Score[1])
                    {
                        images[i].IsXRay = false;
                    }
                    else if (result.Score[1] > result.Score[0])
                    {
                        images[i].IsXRay = true;
                    }
                }
                db.SaveChanges();
            }
            return Ok();
        }
        [HttpGet("markXRay3")]
        public ActionResult MarkXRay3()
        {

            using (ClinicContext db = new ClinicContext(Startup.Configuration["ConnectionStrings:ClinicDent"]))
            {
                int startId = 8441;
                int endId = 11800;
                while(startId <= endId)
                {
                    Models.Image image = db.Images.FirstOrDefault(i => i.Id == startId);
                    if (image==null )
                    {
                        startId++;
                        continue;
                    }
                    XRayMLModel.ModelInput sampleData = new XRayMLModel.ModelInput()
                    {
                        ImageSource = image.OriginalBytes,
                    };

                    //Load model and predict output
                    XRayMLModel.ModelOutput result = XRayMLModel.Predict(sampleData);
                    if (result.Score[0] > result.Score[1])
                    {
                        image.IsXRay = false;
                    }
                    else if (result.Score[1] > result.Score[0])
                    {
                        image.IsXRay = true;
                    }
                    db.SaveChanges();
                    startId++;
                }
            }
            return Ok();
        }
        public static string GetImageExtension(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length < 4) // We'll need at least 4 bytes to determine type
                return null;
            // JPEG
            if (imageBytes[0] == 0xFF && imageBytes[1] == 0xD8)
                return ".jpg";
            // PNG
            if (imageBytes[0] == 0x89 && imageBytes[1] == 0x50 && imageBytes[2] == 0x4E && imageBytes[3] == 0x47)
                return ".png";
            // BMP
            if (imageBytes[0] == 0x42 && imageBytes[1] == 0x4D)
                return ".bmp";
            // TIFF (little endian)
            if (imageBytes[0] == 0x49 && imageBytes[1] == 0x49 && imageBytes[2] == 0x2A && imageBytes[3] == 0x00)
                return ".tif";
            // TIFF (big endian)
            if (imageBytes[0] == 0x4D && imageBytes[1] == 0x4D && imageBytes[2] == 0x00 && imageBytes[3] == 0x2A)
                return ".tif";
            return null;
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

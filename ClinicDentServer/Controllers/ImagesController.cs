using ClinicDentServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClinicDentServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ImagesController : ControllerBase
    {
        //return single compressed image by id
        [HttpGet("{imageId}")]
        public async Task<ActionResult<ImageDTO>> Get(int imageId)
        {
            ClinicContext clinicContext = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value);
            ImageDTO image = await clinicContext.Images.Where(i => i.Id == imageId).Select(i => new ImageDTO()
            {
                Id=i.Id,
                FileName=i.FileName,
                CompressedBytes=i.CompressedBytes,
                OriginalBytes=null
            }).FirstOrDefaultAsync();
            clinicContext.Dispose();
            if (image == null)
            {
                return NotFound();
            }
            return Ok(image);
        }
        //return collection of compressed images
        [HttpGet("{selectedPage}/{photosPerPage}/{doctorId}")]
        public async Task<ActionResult<ImagesToClient>> Get(int selectedPage, int photosPerPage, int doctorId)
        {
            ClinicContext clinicContext = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value);
            double countPages = await clinicContext.Images.Where(i => i.DoctorId == doctorId).CountAsync() / Convert.ToDouble(photosPerPage);
            if (countPages - Math.Truncate(countPages) != 0)
            {
                countPages = Math.Ceiling(countPages);
            }
            ImagesToClient imagesToClient = new ImagesToClient();
            imagesToClient.CountPages = (int)countPages;

            ImageDTO[] imagesFromDb = clinicContext.Images.Where(i => i.DoctorId == doctorId).OrderByDescending(i => i.Id).Skip(photosPerPage * (selectedPage - 1)).Take(photosPerPage).Select(i => new ImageDTO()
            {
                Id = i.Id,
                FileName = i.FileName,
                CompressedBytes = i.CompressedBytes,
                OriginalBytes = null
            }).ToArray();
            imagesToClient.Images = imagesFromDb;
            clinicContext.Dispose();
            return Ok(imagesToClient);

        }
        //return original bytes of image by id
        [HttpGet("getOriginalBytes/{imageId}")]
        [Produces("application/octet-stream")]
        public async Task<ActionResult> GetOriginalBytes(int imageId)
        {
            ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value);
            byte[] originalBytes = await db.Images.Where(i => i.Id == imageId).Select(i=>i.OriginalBytes).FirstOrDefaultAsync();
            db.Dispose();
            if (originalBytes == null)
            {
                return NotFound();
            }
            return File(originalBytes, "application/octet-stream");
        }
        [HttpPost]
        public async Task<ActionResult<ImageDTO>> Post(ImageDTO image)
        {
            ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value);
            Image imageFromDTO = new Image(image);
            db.Images.Add(imageFromDTO);

            await db.SaveChangesAsync();
            db.Dispose();

            image.Id = imageFromDTO.Id;
            return Ok(image);
        }
        [HttpPost("changeImageName/{imageId}")]
        [Consumes("text/plain")]
        public async Task<ActionResult> ChangeImageName(int imageId, [FromBody]string newName)
        {
            ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value);

            db.Database.ExecuteSqlRaw($"UPDATE [Images] SET [FileName]='{newName}' WHERE [Id]={imageId}");
            await db.SaveChangesAsync();
            db.Dispose();
            return Ok();
        }
        [HttpDelete("{id}")]
        public async Task<ActionResult<ImageDTO>> Delete(int id)
        {
            ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value);

            Image image = await db.Images.FirstOrDefaultAsync(x => x.Id == id);
            if (image == null)
            {
                return NotFound();
            }
            db.Images.Remove(image);
            db.SaveChanges();
            db.Dispose();
            return Ok(new ImageDTO(image));
        }
    }
}

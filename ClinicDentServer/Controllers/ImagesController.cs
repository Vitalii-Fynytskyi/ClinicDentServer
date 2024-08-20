using ClinicDentServer.Exceptions;
using ClinicDentServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using XRayMLApp;

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
            using(ClinicContext clinicContext = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                ImageDTO image = await clinicContext.Images.AsNoTracking().Where(i => i.Id == imageId).Select(i => new ImageDTO()
                {
                    Id = i.Id,
                    FileName = i.FileName,
                    CompressedBytes = i.CompressedBytes,
                    OriginalBytes = null
                }).FirstOrDefaultAsync();
                if (image == null)
                {
                    throw new NotFoundException($"Image with id='{imageId}' cannot be found");
                }
                return Ok(image);
            }
            
        }

        //return collection of compressed images
        [HttpGet("{selectedPage}/{photosPerPage}/{doctorId}/{imageType}")]
        public async Task<ActionResult<ImagesToClient>> Get(int selectedPage, int photosPerPage, int doctorId, ImageType imageType)
        {
            using(ClinicContext clinicContext = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                double countPages = 0;
                if (imageType == ImageType.All)
                    countPages = await clinicContext.Images.AsNoTracking().Where(i => i.DoctorId == doctorId).CountAsync() / Convert.ToDouble(photosPerPage);
                else if (imageType == ImageType.XRay)
                    countPages = await clinicContext.Images.AsNoTracking().Where(i => i.DoctorId == doctorId && i.IsXRay == true).CountAsync() / Convert.ToDouble(photosPerPage);
                else if(imageType==ImageType.Regular)
                    countPages = await clinicContext.Images.AsNoTracking().Where(i => i.DoctorId == doctorId && i.IsXRay == false).CountAsync() / Convert.ToDouble(photosPerPage);
                else if (imageType == ImageType.Undefined)
                    countPages = await clinicContext.Images.AsNoTracking().Where(i => i.DoctorId == doctorId && i.IsXRay == null).CountAsync() / Convert.ToDouble(photosPerPage);


                if (countPages - Math.Truncate(countPages) != 0)
                {
                    countPages = Math.Ceiling(countPages);
                }
                ImagesToClient imagesToClient = new ImagesToClient();
                imagesToClient.CountPages = (int)countPages;

                ImageDTO[] imagesFromDb = null;

                if (imageType == ImageType.All)
                {
                    imagesFromDb = clinicContext.Images.AsNoTracking().Where(i => i.DoctorId == doctorId).OrderByDescending(i => i.Id).Skip(photosPerPage * (selectedPage - 1)).Take(photosPerPage).Select(i => new ImageDTO()
                    {
                        Id = i.Id,
                        FileName = i.FileName,
                        CompressedBytes = i.CompressedBytes,
                        OriginalBytes = null
                    }).ToArray();
                }
                else  if(imageType == ImageType.XRay)
                {
                    imagesFromDb = clinicContext.Images.AsNoTracking().Where(i => i.DoctorId == doctorId && i.IsXRay == true).OrderByDescending(i => i.Id).Skip(photosPerPage * (selectedPage - 1)).Take(photosPerPage).Select(i => new ImageDTO()
                    {
                        Id = i.Id,
                        FileName = i.FileName,
                        CompressedBytes = i.CompressedBytes,
                        OriginalBytes = null
                    }).ToArray();
                }
                else if(imageType == ImageType.Regular)
                {
                    imagesFromDb = clinicContext.Images.AsNoTracking().Where(i => i.DoctorId == doctorId && i.IsXRay == false).OrderByDescending(i => i.Id).Skip(photosPerPage * (selectedPage - 1)).Take(photosPerPage).Select(i => new ImageDTO()
                    {
                        Id = i.Id,
                        FileName = i.FileName,
                        CompressedBytes = i.CompressedBytes,
                        OriginalBytes = null
                    }).ToArray();
                }
                else if(imageType == ImageType.Undefined)
                {
                    imagesFromDb = clinicContext.Images.AsNoTracking().Where(i => i.DoctorId == doctorId && i.IsXRay == null).OrderByDescending(i => i.Id).Skip(photosPerPage * (selectedPage - 1)).Take(photosPerPage).Select(i => new ImageDTO()
                    {
                        Id = i.Id,
                        FileName = i.FileName,
                        CompressedBytes = i.CompressedBytes,
                        OriginalBytes = null
                    }).ToArray();
                }
                
                imagesToClient.Images = imagesFromDb;
                return Ok(imagesToClient);
            }
        }
        //return original bytes of image by id
        [HttpGet("getOriginalBytes/{imageId}")]
        [Produces("application/octet-stream")]
        public async Task<ActionResult> GetOriginalBytes(int imageId)
        {
            using(ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                byte[] originalBytes = await db.Images.AsNoTracking().Where(i => i.Id == imageId).Select(i => i.OriginalBytes).FirstOrDefaultAsync();
                if (originalBytes == null)
                {
                    throw new NotFoundException($"Image with id='{imageId}' cannot be found");
                }
                return File(originalBytes, "application/octet-stream");
            }
            
        }
        [HttpPost]
        public async Task<ActionResult<ImageDTO>> Post(ImageDTO image)
        {
            using(ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                Image imageFromDTO = new Image(image);
                XRayMLModel.ModelInput sampleData = new XRayMLModel.ModelInput()
                {
                    ImageSource = imageFromDTO.OriginalBytes,
                };
                try
                {
                    //Load model and predict output
                    XRayMLModel.ModelOutput result = XRayMLModel.Predict(sampleData);
                    if (result.Score[0] > result.Score[1])
                    {
                        imageFromDTO.IsXRay = false;
                    }
                    else if (result.Score[1] > result.Score[0])
                    {
                        imageFromDTO.IsXRay = true;
                    }
                }
                catch { }
                db.Images.Add(imageFromDTO);
                await db.SaveChangesAsync();
                image.Id = imageFromDTO.Id;
                image.IsXRay = imageFromDTO.IsXRay;
                return Ok(image);
            }
        }
        [HttpPost("changeImageName/{imageId}")]
        [Consumes("text/plain")]
        public async Task<ActionResult> ChangeImageName(int imageId, [FromBody]string newName)
        {
            using(ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                db.Database.ExecuteSqlRaw($"UPDATE [Images] SET [FileName]='{newName}' WHERE [Id]={imageId}");
                return Ok();
            }
        }
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            using(ClinicContext db = new ClinicContext(HttpContext.User.Claims.FirstOrDefault(c => c.Type == "ConnectionString").Value))
            {
                Image image = await db.Images.FirstOrDefaultAsync(x => x.Id == id);
                if (image == null)
                {
                    throw new NotFoundException($"Image with id='{id}' cannot be found");
                }
                db.Images.Remove(image);
                db.SaveChanges();
                return NoContent();
            }
        }
    }
}

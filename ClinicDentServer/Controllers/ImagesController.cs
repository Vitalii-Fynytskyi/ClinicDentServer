using ClinicDentServer.Exceptions;
using ClinicDentServer.Interfaces.Repositories;
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
        public IImageRepository<Image> imageRepository;
        public ImagesController(IImageRepository<Image> clinicResositoryToSet)
        {
            imageRepository = clinicResositoryToSet;
        }
        //return single compressed image by id
        [HttpGet("{imageId}")]
        public async Task<ActionResult<ImageDTO>> Get(int imageId)
        {
            ImageDTO image = await imageRepository.dbSet.AsNoTracking().Where(i => i.Id == imageId).Select(i => new ImageDTO()
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

        //return collection of compressed images
        [HttpGet("{selectedPage}/{photosPerPage}/{doctorId}/{imageType}")]
        public async Task<ActionResult<ImagesToClient>> Get(int selectedPage, int photosPerPage, int doctorId, ImageType imageType)
        {
            double countPages = 0;
            if (imageType == ImageType.All)
                countPages = await imageRepository.dbSet.AsNoTracking().Where(i => i.DoctorId == doctorId).CountAsync() / Convert.ToDouble(photosPerPage);
            else if (imageType == ImageType.XRay)
                countPages = await imageRepository.dbSet.AsNoTracking().Where(i => i.DoctorId == doctorId && i.IsXRay == true).CountAsync() / Convert.ToDouble(photosPerPage);
            else if (imageType == ImageType.Regular)
                countPages = await imageRepository.dbSet.AsNoTracking().Where(i => i.DoctorId == doctorId && i.IsXRay == false).CountAsync() / Convert.ToDouble(photosPerPage);
            else if (imageType == ImageType.Undefined)
                countPages = await imageRepository.dbSet.AsNoTracking().Where(i => i.DoctorId == doctorId && i.IsXRay == null).CountAsync() / Convert.ToDouble(photosPerPage);


            if (countPages - Math.Truncate(countPages) != 0)
            {
                countPages = Math.Ceiling(countPages);
            }
            ImagesToClient imagesToClient = new ImagesToClient();
            imagesToClient.CountPages = (int)countPages;

            ImageDTO[] imagesFromDb = null;

            if (imageType == ImageType.All)
            {
                imagesFromDb = imageRepository.dbSet.AsNoTracking().Where(i => i.DoctorId == doctorId).OrderByDescending(i => i.Id).Skip(photosPerPage * (selectedPage - 1)).Take(photosPerPage).Select(i => new ImageDTO()
                {
                    Id = i.Id,
                    FileName = i.FileName,
                    CompressedBytes = i.CompressedBytes,
                    OriginalBytes = null
                }).ToArray();
            }
            else if (imageType == ImageType.XRay)
            {
                imagesFromDb = imageRepository.dbSet.AsNoTracking().Where(i => i.DoctorId == doctorId && i.IsXRay == true).OrderByDescending(i => i.Id).Skip(photosPerPage * (selectedPage - 1)).Take(photosPerPage).Select(i => new ImageDTO()
                {
                    Id = i.Id,
                    FileName = i.FileName,
                    CompressedBytes = i.CompressedBytes,
                    OriginalBytes = null
                }).ToArray();
            }
            else if (imageType == ImageType.Regular)
            {
                imagesFromDb = imageRepository.dbSet.AsNoTracking().Where(i => i.DoctorId == doctorId && i.IsXRay == false).OrderByDescending(i => i.Id).Skip(photosPerPage * (selectedPage - 1)).Take(photosPerPage).Select(i => new ImageDTO()
                {
                    Id = i.Id,
                    FileName = i.FileName,
                    CompressedBytes = i.CompressedBytes,
                    OriginalBytes = null
                }).ToArray();
            }
            else if (imageType == ImageType.Undefined)
            {
                imagesFromDb = imageRepository.dbSet.AsNoTracking().Where(i => i.DoctorId == doctorId && i.IsXRay == null).OrderByDescending(i => i.Id).Skip(photosPerPage * (selectedPage - 1)).Take(photosPerPage).Select(i => new ImageDTO()
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
        //return original bytes of image by id
        [HttpGet("getOriginalBytes/{imageId}")]
        [Produces("application/octet-stream")]
        public async Task<ActionResult> GetOriginalBytes(int imageId)
        {
            byte[] originalBytes = await imageRepository.dbSet.AsNoTracking().Where(i => i.Id == imageId).Select(i => i.OriginalBytes).FirstOrDefaultAsync();
            if (originalBytes == null)
            {
                throw new NotFoundException($"Image with id='{imageId}' cannot be found");
            }
            return File(originalBytes, "application/octet-stream");

        }
        [HttpPost]
        public async Task<ActionResult<ImageDTO>> Post(ImageDTO image)
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
            await imageRepository.Add(imageFromDTO);
            image.Id = imageFromDTO.Id;
            image.IsXRay = imageFromDTO.IsXRay;
            return Ok(image);
        }
        [HttpPost("changeImageName/{imageId}")]
        [Consumes("text/plain")]
        public async Task<ActionResult> ChangeImageName(int imageId, [FromBody]string newName)
        {
            await imageRepository.ChangeImageName(imageId, newName);
            return Ok();
        }
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            Image image = await imageRepository.dbSet.FirstOrDefaultAsync(x => x.Id == id);
            if (image == null)
            {
                throw new NotFoundException($"Image with id='{id}' cannot be found");
            }
            await imageRepository.Remove(image);
            return NoContent();
        }
    }
}

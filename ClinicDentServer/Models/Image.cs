using System.Collections.Generic;

namespace ClinicDentServer.Models
{
    public enum ImageType
    {
        Undefined = 0,
        Regular = 1,
        XRay = 2,
        All = 3,

    }
    public class Image
    {
        public Image()
        {
            Stages = new List<Stage>();
        }
        public Image(ImageDTO i)
        {
            Id = i.Id;
            OriginalBytes = i.OriginalBytes;
            CompressedBytes = i.CompressedBytes;
            FileName = i.FileName;
            DoctorId = i.DoctorId;
            IsXRay = i.IsXRay;
            Stages = new List<Stage>();

        }
        public int Id { get; set; }
        public byte[] OriginalBytes { get; set; }
        public byte[] CompressedBytes { get; set; }
        public string FileName { get; set; }
        public virtual Doctor Doctor { get; set; }
        public int? DoctorId { get; set; }
        public bool? IsXRay { get; set; }
        public virtual ICollection<Stage> Stages { get; set; }

    }
    public class ImageDTO
    {
        public ImageDTO() { }
        public ImageDTO(Image i)
        {
            Id = i.Id;
            OriginalBytes = i.OriginalBytes;
            CompressedBytes = i.CompressedBytes;
            FileName = i.FileName;
            IsXRay = i.IsXRay;
            DoctorId = i.DoctorId.Value;
        }
        public int Id { get; set; }
        public byte[] OriginalBytes { get; set; }
        public byte[] CompressedBytes { get; set; }
        public string FileName { get; set; }
        public bool? IsXRay { get; set; }

        public int DoctorId { get; set; }

    }
    public class ImagesToClient
    {
        public ImageDTO[] Images;
        public int CountPages { get; set; }
    }
}

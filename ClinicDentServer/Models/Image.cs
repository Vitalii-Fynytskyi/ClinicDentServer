using System.Collections.Generic;

namespace ClinicDentServer.Models
{
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
            Stages = new List<Stage>();

        }
        public int Id { get; set; }
        public byte[] OriginalBytes { get; set; }
        public byte[] CompressedBytes { get; set; }
        public string FileName { get; set; }
        public virtual Doctor Doctor { get; set; }
        public int? DoctorId { get; set; }
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
            DoctorId = i.DoctorId.Value;
        }
        public int Id { get; set; }
        public byte[] OriginalBytes { get; set; }
        public byte[] CompressedBytes { get; set; }
        public string FileName { get; set; }
        public int DoctorId { get; set; }

    }
    public class ImagesToClient
    {
        public ImageDTO[] Images;
        public int CountPages { get; set; }
    }
}

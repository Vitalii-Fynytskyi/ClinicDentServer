﻿namespace ClinicDentServer.Models
{
    public enum AssetType :byte
    {
        Bond = 0,
        Dentin = 1,
        Enamel = 2,
        CanalMethod = 3,
        Sealer = 4,
        Cement = 5,
        Technician = 6,
        Pin = 7,
        Operation = 8,
        Calcium = 9
    }
    /// <summary>
    /// Assets represents additional params of stages such as used materials in work
    /// </summary>
    public class StageAsset :BaseModel
    {
        public AssetType Type { get; set; }
        public string Value { get; set; }
    }
}

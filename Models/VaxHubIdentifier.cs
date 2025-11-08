using Newtonsoft.Json;

namespace VaxCareApiTests.Models;

public class VaxHubIdentifier
{
    [JsonProperty("androidSdk")]
    public int AndroidSdk { get; set; }

    [JsonProperty("androidVersion")]
    public string AndroidVersion { get; set; } = string.Empty;

    [JsonProperty("assetTag")]
    public int AssetTag { get; set; }

    [JsonProperty("clinicId")]
    public int ClinicId { get; set; }

    [JsonProperty("deviceSerialNumber")]
    public string DeviceSerialNumber { get; set; } = string.Empty;

    [JsonProperty("partnerId")]
    public int PartnerId { get; set; }

    [JsonProperty("userId")]
    public int UserId { get; set; }

    [JsonProperty("userName")]
    public string UserName { get; set; } = string.Empty;

    [JsonProperty("version")]
    public int Version { get; set; }

    [JsonProperty("versionName")]
    public string VersionName { get; set; } = string.Empty;

    [JsonProperty("modelType")]
    public string ModelType { get; set; } = string.Empty;
}


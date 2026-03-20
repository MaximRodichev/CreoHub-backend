using System.Text.Json.Serialization;
using CreoHub.Domain.Types;

namespace CreoHub.Application.DTO.StorageDTOs;

public class StorageObjectResponseDTO
{
    public Guid Id { get; set; }
    public string Key { get; set; }
    public string MimeType { get; set; }
    public long FileSize { get; set; }
    public string FileName { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public FileType FileType { get; set; }
}
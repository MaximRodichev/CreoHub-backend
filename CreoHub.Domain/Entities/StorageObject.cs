using System.ComponentModel.DataAnnotations;
using CreoHub.Domain.Types;

namespace CreoHub.Domain.Entities;

public class StorageObject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Key { get; set; }
    public string FileName { get; set; }
    public long FileSize { get; set; }
    public string MimeType { get; set; }
    public DateTime UploadedAt  { get; set; } = DateTime.Now;
    public FileType FileType { get; private set; } = FileType.Unregistred;
    public Shop Owner { get; set; }
    public Guid OwnerId { get; set; }

    public StorageObject ChangeFileType(FileType fileType)
    {
        if (this.FileType != FileType.Content)
        {
            this.FileType = fileType;
            return this;
        }
        throw new InvalidOperationException("Cannot change file type");
    }
}

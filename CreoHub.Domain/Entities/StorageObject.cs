using CreoHub.Domain.Types;

namespace CreoHub.Domain.Entities;

public class StorageObject
{
    public Guid Id { get; private init; } = Guid.NewGuid();
    public string Key { get; private set; }
    public string FileName { get; private set; }
    public long FileSize { get; private set; }
    public string MimeType { get; private set; }
    public DateTime UploadedAt { get; private init; } = DateTime.UtcNow;
    public FileType FileType { get; private set; } = FileType.Unregistred;

    /// <summary>
    /// True — файл содержит данные покупателей. Удаление и оптимизация запрещены.
    /// Привязка к другим товарам разрешена.
    /// </summary>
    public bool IsSystemLocked { get; private set; }

    public Shop Owner { get; private init; }
    public Guid OwnerId { get; private init; }
    
    public MediaProduct? MediaProduct { get; private set; }
    public ICollection<ContentFile>  ContentFiles { get; private set; } = new List<ContentFile>();

    private StorageObject() {}

    public StorageObject(string key, string fileName, long fileSize, 
        string mimeType, Guid ownerId)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        FileSize = fileSize > 0 ? fileSize 
            : throw new ArgumentException("File size must be greater than zero.", nameof(fileSize));
        MimeType = mimeType ?? throw new ArgumentNullException(nameof(mimeType));
        OwnerId = ownerId;
    }

    public void ReplaceFile(string newKey, string newFileName, 
        long newFileSize, string newMimeType)
    {
        Key = newKey ?? throw new ArgumentNullException(nameof(newKey));
        FileName = newFileName ?? throw new ArgumentNullException(nameof(newFileName));
        FileSize = newFileSize > 0 ? newFileSize 
            : throw new ArgumentException("File size must be greater than zero.", nameof(newFileSize));
        MimeType = newMimeType ?? throw new ArgumentNullException(nameof(newMimeType));
    }

    public void ChangeFileType(FileType fileType)
    {
        if (FileType == FileType.Content && fileType == FileType.Media)
            throw new InvalidOperationException("Cannot change file type content to media.");
        if (FileType == FileType.Media && fileType == FileType.Content)
            throw new InvalidOperationException("Cannot change file type media to content.");
        if (IsSystemLocked && fileType == FileType.Unregistred)
            throw new InvalidOperationException("Cannot unregister a system-locked storage object.");
        FileType = fileType;
    }

    /// <summary>
    /// Блокирует файл от удаления и оптимизации.
    /// Вызывается при архивировании контент файла с существующими покупателями.
    /// </summary>
    public void Lock()
    {
        IsSystemLocked = true;
    }
}


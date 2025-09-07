namespace FileShare.Models
{
    public class DownloadModel
    {
        public string FileKey { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string FileExtension { get; set; }
    }
}

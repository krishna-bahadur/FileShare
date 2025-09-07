
using Microsoft.Extensions.Caching.Memory;

namespace FileShare.Services
{
    public class FileCleanupService : BackgroundService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<FileCleanupService> _logger;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(12); // Check every 12 hours

        public FileCleanupService(IWebHostEnvironment env, ILogger<FileCleanupService> logger, IMemoryCache cache)
        {
            _env = env;
            _logger = logger;
            _cache = cache;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var uploads = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploads))
            {
                Directory.CreateDirectory(uploads);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var files = Directory.GetFiles(uploads);
                    foreach (var file in files)
                    {
                        var key = Path.GetFileNameWithoutExtension(file);

                        // If cache does not contain this key, delete file
                        if (!_cache.TryGetValue(key, out _))
                        {
                            System.IO.File.Delete(file);
                            _logger.LogInformation("Deleted Expired file: {file}", file);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during file cleanup.");
                }

                await Task.Delay(_cleanupInterval, stoppingToken);
            }
        }
    }
}

using System.Diagnostics;
using FileShare.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace FileShare.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IMemoryCache _cache;

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment env, IMemoryCache cache)
        {
            _logger = logger;
            _env = env;
            _cache = cache;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Upload(IFormFile file)
        {
            if(file != null && file.Length > 0)
            {
                var uploads = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploads))
                {
                    Directory.CreateDirectory(uploads); 
                }

                var uniqueFile = GenerateShortId();
                var uniqueFileName = uniqueFile + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(uploads, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                _cache.Set(uniqueFile, file.FileName, TimeSpan.FromHours(12));

                var downloadLink = Url.RouteUrl("share", new { fileName = uniqueFile }, Request.Scheme);

                return Json(new { success = true, link = downloadLink });
            }

            return Json(new {success = false, message = "No file uploaded"});
        }

        public IActionResult Share(string fileName)
        {
            var uploads = Path.Combine(_env.WebRootPath, "uploads");
            var filePath = Directory.GetFiles(uploads, fileName + "*").FirstOrDefault();

            if (filePath != null && System.IO.File.Exists(filePath))
            {
                _cache.TryGetValue(fileName, out string originalName);
                var fileInfo = new FileInfo(filePath);

                var model = new DownloadModel
                {
                    FileKey = fileName,
                    FileName = originalName ?? fileInfo.Name,
                    FileSize = fileInfo.Length,
                    FileExtension = fileInfo.Extension.ToLower()
                };

                return View(model);
            }

            return NotFound();
        }

        [HttpGet]
        public IActionResult Download(string fileName)
        {
            var uploads = Path.Combine(_env.WebRootPath, "uploads");
            var filePath = Directory.GetFiles(uploads, fileName + "*").FirstOrDefault();

            if (filePath == null || !System.IO.File.Exists(filePath))
            {
                return NotFound("File not found");
            }

            _cache.TryGetValue(fileName, out string originalName);
            var fileInfo = new FileInfo(filePath);
            var downloadName = originalName ?? fileInfo.Name;

            // Get content type
            var contentType = GetContentType(fileInfo.Extension);

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, contentType, downloadName);
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private static string GenerateShortId(int length = 8)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private string GetContentType(string extension)
        {
            return extension.ToLower() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".pdf" => "application/pdf",
                ".txt" => "text/plain",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".zip" => "application/zip",
                ".rar" => "application/x-rar-compressed",
                _ => "application/octet-stream"
            };
        }
    }
}

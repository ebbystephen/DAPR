using AzureBlob.Sdk.Web.Services;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;

namespace AzureBlob.Sdk.Web.Controllers
{
    public class BlobController : Controller
    {
        private readonly AzureBlobService _blobService;

        public BlobController(AzureBlobService blobService)
        {
            _blobService = blobService;
        }

        public async Task<IActionResult> Index()
        {
            var blobs = await _blobService.ListAsync();
            ViewBag.AuthMode = _blobService.AuthMode;
            return View(blobs);
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return RedirectToAction("Index", new { message = "No file selected." });

            using var stream = file.OpenReadStream();
            await _blobService.UploadAsync(stream, file.FileName);

            return RedirectToAction("Index", new { message = "File uploaded successfully!" });
        }

        [HttpGet]
        public async Task<IActionResult> Download(string fileName)
        {
            var stream = await _blobService.DownloadAsync(fileName);
            return File(stream, "application/octet-stream", fileName);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string fileName)
        {
            await _blobService.DeleteAsync(fileName);
            return RedirectToAction("Index", new { message = "File deleted successfully!" });
        }
    }
}

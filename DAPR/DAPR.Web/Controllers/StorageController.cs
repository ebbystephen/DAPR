using Dapr.Client;
using DAPR.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace DAPR.Web.Controllers
{
    //[Route("[controller]")] // Base route is /Storage
    //[ApiController]
    public class StorageController : Controller
    {
        private readonly DaprClient _daprClient;
        // Reusing the same component name
        private const string BindingName = "azblob-storage";

        public StorageController(DaprClient daprClient)
        {
            _daprClient = daprClient;
        }

        // --- Controller Entry Point (Lists the blobs) ---
        public async Task<IActionResult> Index()
        {
            // Call the ListBlobs method to populate the view model
            var listResult = await ListBlobsInternal();

            // Check if the result is an OkObjectResult containing the list data
            if (listResult is OkObjectResult okResult)
            {
                dynamic resultObject = okResult.Value;
                // We must convert the dynamic/anonymous object back into a list of strings
                // for the view model (List<string>) which we will modify in the CSHTML.
                List<string> fileNames = new List<string>();
                if (resultObject.Blobs is IEnumerable<dynamic> blobs)
                {
                    foreach (var blob in blobs)
                    {
                        // Assuming the 'Name' property exists on the anonymous object
                        fileNames.Add(blob.Name);
                    }
                }

                ViewBag.BindingName = BindingName;
                // If you had a mechanism to determine AuthMode, you'd set it here
                // ViewBag.AuthMode = "SAS"; 

                return View(fileNames);
            }

            // On failure, return an empty list but still show the view
            ViewBag.BindingName = BindingName;
            return View(new List<string>());
        }

        // --- UPLOAD (Using Dapr Output Binding) ---
        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var fileBytes = ms.ToArray();

            var operation = "create";

            var metadata = new Dictionary<string, string>
            {
                { "blobName", file.FileName }
            };

            await _daprClient.InvokeBindingAsync(
                BindingName,
                operation,
                fileBytes,
                metadata);

            return RedirectToAction(nameof(Index)); // Redirect to refresh the list
        }

        // --- DOWNLOAD (Using Dapr HTTP GET Request) ---
        [HttpGet]
        public async Task<IActionResult> Download(string fileName)
        {
            var request = new BindingRequest(BindingName, "get");
            // The metadata for this overload is set on the request object, 
            // matching the successful implementation pattern from your FileController.
            request.Metadata.Add("blobName", fileName);

            var response = await _daprClient.InvokeBindingAsync(request);

            if (response.Data.IsEmpty)
                return NotFound("File not found.");

            // 1. Get the raw file data as a byte array
            byte[] fileBytes = response.Data.ToArray();

            // 2. Determine the content type (MIME type). 
            // "application/octet-stream" is the safe default for any binary file.
            // If you need a specific MIME type (e.g., "image/jpeg"), you could try to 
            // get it from Dapr metadata or infer it from the file name.
            string contentType = "application/octet-stream";

            // 3. 🛑 CORRECTED: Return the FileContentResult using the byte array.
            return File(
                fileBytes,          // The byte array of the file content
                contentType,        // The MIME type
                fileName            // The name the browser should use for the downloaded file
            );
        }

        // --- DELETE (Using Dapr Output Binding) ---
        // Changed to [HttpPost] to match form submission logic in the view
        [HttpPost]
        public async Task<IActionResult> Delete([FromForm] string fileName)
        {
            var operation = "delete";

            var metadata = new Dictionary<string, string>
            {
                { "blobName", fileName }
            };

            // Invoke binding with empty data payload
            await _daprClient.InvokeBindingAsync(
                BindingName,
                operation,
                data: default(object),
                metadata);

            return RedirectToAction(nameof(Index)); // Redirect to refresh the list
        }

        /// <summary>
        /// Internal method to fetch the blob list, based on the working ListBlobs logic.
        /// </summary>
        private async Task<IActionResult> ListBlobsInternal(string prefix = null, int maxResults = 50, string marker = null)
        {
            const string Operation = "list";

            var listPayload = new ListBlobsRequest
            {
                MaxResults = maxResults,
                Prefix = prefix,
                Marker = marker
            };

            var bindingRequestData = new BindingInvocationRequest<ListBlobsRequest>
            {
                Operation = Operation,
                Data = listPayload
            };

            try
            {
                var requestBytes = JsonSerializer.SerializeToUtf8Bytes(bindingRequestData);

                var bindingPayload = new Dapr.Client.BindingRequest(
                    BindingName,
                    Operation
                )
                {
                    Data = requestBytes,
                };

                // Using the specific working overload that returns BindingResponse
                var bindingResponse = await _daprClient.InvokeBindingAsync(
                    bindingPayload,
                    CancellationToken.None
                );

                // Accessing the Data property of BindingResponse
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(bindingResponse.Data.ToArray());

                if (jsonResponse.ValueKind == JsonValueKind.Array)
                {
                    var foundBlobs = new List<object>();

                    foreach (var element in jsonResponse.EnumerateArray())
                    {
                        element.TryGetProperty("Name", out var nameElement);
                        element.TryGetProperty("Properties", out var propertiesElement);
                        propertiesElement.TryGetProperty("LastModified", out var lastModifiedElement);

                        foundBlobs.Add(new
                        {
                            Name = nameElement.GetString(),
                            LastModified = lastModifiedElement.GetString()
                        });
                    }

                    // Return an OkObjectResult containing the list structure
                    return Ok(new
                    {
                        Count = foundBlobs.Count,
                        Blobs = foundBlobs,
                    });
                }

                return BadRequest("Dapr response was not a JSON array of blobs.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Dapr Binding Invocation Failed: {ex.Message}");
            }
        }
    }
}

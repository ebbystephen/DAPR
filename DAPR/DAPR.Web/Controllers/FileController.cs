using Dapr.Client;
using DAPR.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace DAPR.Web.Controllers
{
    [Route("[controller]")] // This tells ASP.NET Core that the base route is /File
    [ApiController]
    public class FileController : Controller
    {
        private readonly DaprClient _daprClient;
        private const string BindingName = "azblob-storage"; // Must match the component name

        public FileController(DaprClient daprClient)
        {
            _daprClient = daprClient;
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }

        // --- UPLOAD (Using Dapr Output Binding) ---
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var fileBytes = ms.ToArray();

            var operation = "create"; // Action for the output binding

            // Dapr Bindings Metadata (Blob name is crucial)
            var metadata = new Dictionary<string, string>
        {
            { "blobName", file.FileName } // Specifies the file name in the container
        };

            // Invoke the Dapr Output Binding
            await _daprClient.InvokeBindingAsync(
                BindingName,
                operation,
                fileBytes,
                metadata);

            return Ok($"File '{file.FileName}' uploaded successfully via Dapr Binding.");
        }

        // --- DOWNLOAD (Using Dapr HTTP GET Request) ---
        [HttpGet("download/{fileName}")]
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            var request = new BindingRequest(BindingName, "get");
            request.Metadata.Add("blobName", fileName);

            var response = await _daprClient.InvokeBindingAsync(request);

            if (response.Data.IsEmpty)
                return NotFound("File not found.");

            var content = Encoding.UTF8.GetString(response.Data.ToArray());
            return Ok(content);
        }

        // --- DELETE (Using Dapr Output Binding) ---
        [HttpDelete("delete/{fileName}")]
        public async Task<IActionResult> DeleteFile(string fileName)
        {
            var operation = "delete"; // Action for the output binding

            // Dapr Bindings Metadata (Blob name is crucial)
            var metadata = new Dictionary<string, string>
            {
                { "blobName", fileName } // Specifies the file name in the container
            };

            // Invoke the Dapr Output Binding
            // The data payload is empty for a delete operation
            await _daprClient.InvokeBindingAsync(
                BindingName,
                operation,
                data: default(object), // No data payload needed
                metadata);

            return Ok($"File '{fileName}' deleted successfully via Dapr Binding.");
        }

        /// <summary>
        /// Invokes the Dapr Azure Blob Storage binding to list blobs.
        /// </summary>
        [HttpGet("list")]
        public async Task<IActionResult> ListBlobs(
            [FromQuery] string prefix = null,
            [FromQuery] int maxResults = 50,
            [FromQuery] string marker = null)
        {
            const string Operation = "list";

            // 1. Construct the data payload (maps to the "data" field in the JSON)
            var listPayload = new ListBlobsRequest
            {
                MaxResults = maxResults,
                Prefix = prefix,
                Marker = marker
            };

            // 2. Construct the complete request object (maps to the full JSON POST body)
            var bindingRequestData = new BindingInvocationRequest<ListBlobsRequest>
            {
                Operation = "list",
                Data = listPayload
            };

            try
            {
                // 3. Manually serialize the ENTIRE request body to bytes.
                var requestBytes = JsonSerializer.SerializeToUtf8Bytes(bindingRequestData);

                // 4. Wrap the serialized bytes into a Dapr.Client.BindingRequest object.
                // Error: 'BindingRequest' does not contain a constructor that takes 4 arguments
                var bindingPayload = new Dapr.Client.BindingRequest(
             BindingName,
             Operation // <-- Argument 2 is the operation string
         )
                {
                    // 🛑 CRITICAL FIX 2: Set the Data property. This one MUST be writable.
                    Data = requestBytes,
                };

                // 5. Invoke the non-generic binding method to get raw response bytes.
                var responseBytes = await _daprClient.InvokeBindingAsync(
    bindingPayload, // Pass the entire configured object
    CancellationToken.None // Use CancellationToken.None if you don't need cancellation
);

                // 6. Manually deserialize the raw response bytes into a JsonElement.
                // This is necessary because the response format is complex (an array of blobs 
                // plus headers/metadata like marker and number).// New (Correct) Line:
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseBytes.Data.ToArray());

                // 7. Parse Response: Dapr returns the blob list as a JSON array.
                if (jsonResponse.ValueKind == JsonValueKind.Array)
                {
                    var foundBlobs = new List<object>();

                    foreach (var element in jsonResponse.EnumerateArray())
                    {
                        // Accessing Name and LastModified from the complex object structure
                        element.TryGetProperty("Name", out var nameElement);
                        element.TryGetProperty("Properties", out var propertiesElement);
                        propertiesElement.TryGetProperty("LastModified", out var lastModifiedElement);

                        foundBlobs.Add(new
                        {
                            Name = nameElement.GetString(),
                            LastModified = lastModifiedElement.GetString()
                        });
                    }

                    // Response metadata (marker and number) is often returned as HTTP headers, 
                    // but sometimes Dapr includes it in the body based on the version/component.
                    // We assume the successful blob list array is the primary result.
                    return Ok(new
                    {
                        Count = foundBlobs.Count,
                        Blobs = foundBlobs,
                        // If you need the 'marker' or 'number', they would typically be extracted 
                        // from the raw response headers or a wrapper object, not the array itself.
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
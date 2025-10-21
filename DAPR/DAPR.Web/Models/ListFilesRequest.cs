using System.Text.Json.Serialization;

namespace DAPR.Web.Models
{
    using System.Text.Json.Serialization;
    using System.Text.Json;

    // Maps to the entire POST body for the binding invocation.
    public class BindingInvocationRequest<T>
    {
        [JsonPropertyName("operation")]
        public string Operation { get; set; } = "list"; // Hardcode for this operation

        [JsonPropertyName("data")]
        public T Data { get; set; } // The ListBlobsRequest

        [JsonPropertyName("metadata")]
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }

    // Maps to the "data" field in the request body.
    public class ListBlobsRequest
    {
        [JsonPropertyName("maxResults")]
        public int MaxResults { get; set; } = 50;

        [JsonPropertyName("prefix")]
        public string? Prefix { get; set; }

        [JsonPropertyName("marker")]
        public string? Marker { get; set; }

        [JsonPropertyName("include")]
        public BlobIncludeOptions Include { get; set; } = new BlobIncludeOptions();
    }

    // Maps to the "include" sub-field.
    public class BlobIncludeOptions
    {
        [JsonPropertyName("snapshots")]
        public bool Snapshots { get; set; } = false;

        [JsonPropertyName("metadata")]
        public bool Metadata { get; set; } = true;

        [JsonPropertyName("uncommittedBlobs")]
        public bool UncommittedBlobs { get; set; } = false;

        [JsonPropertyName("copy")]
        public bool Copy { get; set; } = false;

        [JsonPropertyName("deleted")]
        public bool Deleted { get; set; } = false;
    }
}

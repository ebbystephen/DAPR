## 🚀 Azure Blob Storage Integration Patterns: SDK vs. Dapr

This repository demonstrates two distinct approaches to performing CRUD (Create, Read, Update, Delete) operations on **Azure Blob Storage** using ASP.NET Core MVC applications.

The goal is to showcase the difference between **direct SDK integration** and using **Dapr's Azure Blob Storage Output Binding**.

-----

## ✨ Features

Both projects provide the following functionalities for the configured Azure Blob container:

  * **File Upload:** Create new blobs.
  * **File List:** Retrieve a list of all blobs.
  * **File Download:** Read the content of a specific blob.
  * **File Delete:** Remove a specific blob.

-----

## 📁 Project Structure

The repository is organized into two main folders, one for each integration method:

| Folder | Description | Key Technology |
| :--- | :--- | :--- |
| **`AzureBlob`** | ASP.NET Core MVC application using the official **Azure SDK (`Azure.Storage.Blobs`)** for all operations. | Azure SDK |
| **`DAPR`** | ASP.NET Core MVC application using the **Dapr Client (`Dapr.Client`)** to interact with the Dapr Sidecar via the Blob Storage binding. | Dapr Bindings |

-----

## ⚙️ Prerequisites

  * [.NET 8 SDK (or later)](https://dotnet.microsoft.com/download)
  * **Azure Storage Account:** A configured Azure Storage Account and an active container.
  * **For DAPR project:** [Dapr CLI](https://docs.dapr.io/getting-started/install-dapr-cli/) and a local Dapr installation (`dapr init`).

-----

## 🔧 Configuration

### 1\. Azure SDK Project (`AzureBlob`)

Configuration details are managed via `appsettings.json`. Update the placeholders with your actual Azure credentials.

**File: `AzureBlob/appsettings.json`**

```json
{
  "AzureBlob": {
    "AccountName": "<your-account-name>",
    "ContainerName": "<your-container-name>",
    "SasUrl": "https://<accountname>.blob.core.windows.net/<containername>/?<sastoken>", // Optional
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=<accountname>;AccountKey=<accountkey>;EndpointSuffix=core.windows.net", // Primary configuration
    "BlobUrl": "https://<accountname>.blob.core.windows.net",
    "UseAuthMode": "KEY" // Current implementation relies on ConnectionString (KEY or SAS)
  }
}
```

### 2\. Dapr Project (`DAPR`)

Configuration for the Dapr component is defined in a YAML file, located in the `components` folder relative to the Dapr application.

**File: `DAPR/components/azblob-storage.yaml`**

Ensure this file is correctly populated with your Azure Storage details.

```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: azblob-storage # This name MUST match the binding name used in the C# code (e.g., BindingName const).
  namespace: default
spec:
  type: bindings.azure.blobstorage
  version: v1
  metadata:
    - name: accountName
      value: "<your-account-name>" 
    - name: accountKey # Dapr uses the accountKey for authentication.
      value : "<your-account-key>"
    - name: container
      value: "<your-container-name>"
```

-----

## ▶️ How to Run

### Project A: Azure SDK (`AzureBlob` Folder)

This is a standard .NET Core application launch.

```bash
cd ./AzureBlob
dotnet run
```

Access the application in your browser, typically at `http://localhost:5213` (or the port specified in the console output).

### Project B: Dapr (`DAPR` Folder)

This requires launching the Dapr sidecar alongside the application using the Dapr CLI.

**Run the following command inside the `DAPR.Web` project folder:**

```bash
# Ensure you are in the DAPR.Web directory (assuming DAPR is the solution folder)
cd ./DAPR/DAPR.Web 

# Launch Dapr sidecar and then the application
dapr run --app-id poc-app \
         --dapr-http-port 3500 \
         --resources-path ./components \
         dotnet run
```

  * **`--app-id poc-app`**: The application ID used for Dapr service discovery.
  * **`--dapr-http-port 3500`**: The port for Dapr's API.
  * **`--resources-path ./components`**: Tells Dapr to load component YAML files from the local `components` directory.

Access the application in your browser, typically at `http://localhost:<application-port>`. The application port is dynamically chosen by `dotnet run` unless configured (e.g., in `launchSettings.json`).

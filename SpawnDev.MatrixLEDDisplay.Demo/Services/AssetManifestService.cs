using SpawnDev.BlazorJS;
using System.Text.Json;

namespace SpawnDev.MatrixLEDDisplay.Demo.Services
{
    public class AssetManifestService : IAsyncBackgroundService
    {
        Task? _Ready;
        public Task Ready => _Ready ??= InitAsync();
        HttpClient HttpClient;
        public AssetManifest? AssetManifest { get; private set; }
        public AssetManifestService(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }
        async Task InitAsync()
        {
            AssetManifest = await GetAssetManifest();
        }
        JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        async Task<AssetManifest?> GetAssetManifest()
        {
            AssetManifest? ret = null;
            var assetManifestFile = "service-worker-assets.js";
            string? assetManifestResp = null;
            try
            {
                assetManifestResp = await HttpClient.GetStringAsync(assetManifestFile);
            }
            catch (Exception ex)
            {
                //JS.Log("Failed to download the asset manifest file", ex.Message);
            }
            if (!string.IsNullOrEmpty(assetManifestResp))
            {
                // http starts is coded to be loaded into a serviceWorker context using importScript
                // trim off the variable assignment so we can process the json
                var start = assetManifestResp.IndexOf('{');
                if (start > -1)
                {
                    var end = assetManifestResp.LastIndexOf('}');
                    if (end > -1)
                    {
                        var json = assetManifestResp.Substring(start, 1 + end - start);
                        try
                        {
                            ret = JsonSerializer.Deserialize<AssetManifest>(json, JsonSerializerOptions);
                        }
                        catch (Exception ex)
                        {
                            //JS.Log("Failed to deserialize the asset manifest file", ex.Message);
                        }
                    }
                }
            }
            return ret;
        }
    }

    /// <summary>
    /// Asset manifest
    /// </summary>
    public class AssetManifest
    {
        /// <summary>
        /// Assets
        /// </summary>
        public List<ManifestAsset> Assets { get; set; }
        /// <summary>
        /// Version
        /// </summary>
        public string Version { get; set; }
    }
    /// <summary>
    /// Manifest asset info
    /// </summary>
    public class ManifestAsset
    {
        /// <summary>
        /// File content hash. This should be the base-64-formatted SHA256 value.
        /// </summary>
        public string Hash { get; set; }
        /// <summary>
        /// Asset URL. Normally this will be relative to the application's base href.
        /// </summary>
        public string Url { get; set; }
    }
}

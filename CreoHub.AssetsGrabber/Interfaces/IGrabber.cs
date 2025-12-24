using CreoHub.AssetsGrabber.Models;

namespace CreoHub.AssetsGrabber.Interfaces;

public interface IGrabber
{
    public Task<List<BundleSpineItem>> GetResultBundleFromUrl(string url);

    public Task<List<string>> CreateDownloadLinks(List<AssetMapItem> baseMap);
}
using CreoHub.AssetsGrabber.Models;

namespace CreoHub.AssetsGrabber;

public class HacksawGrabber
{
    private readonly WebAssetScout _scout;
    public HacksawGrabber(WebAssetScout scout)
    { 
        _scout = scout;   
    }
    
    public async Task<List<string>> CreateAssetList(string url)
    {
        var baseMap = await _scout.CaptureNetworkAssetsAsync(url);

        baseMap = baseMap
            .Where(x => x.Name.EndsWith(".json") || x.Name.EndsWith(".atlas"))
            .ToList();
        
        var resultMap = new List<string>();
        
        foreach (var item in baseMap)
        {
            if(item.Name.EndsWith(".atlas"))
            {
                string fileName = item.Name.Substring(0, item.Name.Length - ".atlas".Length);
                var spineItem = baseMap.Find(x => x.Name == String.Concat([fileName, ".json"]));
                if (spineItem != null)
                {
                    
                    resultMap.Add(item.RemoteUrl);
                    resultMap.Add(spineItem.RemoteUrl);
                    resultMap.Add(spineItem.RemoteUrl.Replace(".json", ".png"));
                }

            }
        }
        
        
        return resultMap;
    }

    public async Task<List<BundleSpineItem>> CreateBundleSpineItemList(string url, string basePath, string folderName)
    {
        var result = new List<BundleSpineItem>();
        var list = await CreateAssetList(url);
        await new AssetDownloader(basePath, folderName).DownloadAllAsync(list);
        foreach (var item in list.Where(x=>x.EndsWith(".atlas")))
        {
            var baseName = item.Split("/")[^1].Replace(".atlas", "");
            result.Add(new  BundleSpineItem()
            {
                name = baseName,
                spine = $"localhost:5242/{folderName}/{baseName}.json",
                atlas = $"localhost:5242/{folderName}/{baseName}.atlas",
                image = new List<string>()
                {
                    $"localhost:5242/{folderName}/{baseName}.png"
                }
            });
        }

        return result;
    }

    public async Task<List<BundleSpineItem>> CreateAssetMap(string url)
    {
        var baseMap = await _scout.CaptureNetworkAssetsAsync(url);

        baseMap = baseMap
            .Where(x => x.Name.EndsWith(".json") || x.Name.EndsWith(".atlas"))
            .ToList();
        
        var resultMap = new List<BundleSpineItem>();
        
        foreach (var item in baseMap)
        {
            if(item.Name.EndsWith(".atlas"))
            {
                string fileName = item.Name.Substring(0, item.Name.Length - ".atlas".Length);
                var spineItem = baseMap.Find(x => x.Name == String.Concat([fileName, ".json"]));
                if (spineItem != null)
                {
                    resultMap.Add(new BundleSpineItem()
                    {
                        name =  fileName,
                        atlas = item.RemoteUrl,
                        spine = spineItem.RemoteUrl,
                        image = new List<string>()
                        {
                            spineItem.RemoteUrl.Replace(".json", ".png")
                        }
                    });
                }

            }
        }
        
        
        return resultMap;
    }
}
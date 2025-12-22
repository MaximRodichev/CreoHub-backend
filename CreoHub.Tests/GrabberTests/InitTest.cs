using System.Text.Json;
using CreoHub.AssetsGrabber;
using Xunit.Abstractions;

namespace CreoHub.Tests.GrabberTests;

public class InitTest : IDisposable
{
    private readonly ITestOutputHelper? _output;

    public InitTest(ITestOutputHelper? output)
    {
        _output = output;
    }
    
    
    [Theory]
    [InlineData("https://static-live.hacksawgaming.com/1348/1.24.1/index.html?language=en&channel=desktop&gameid=1348&mode=2&token=123131&lobbyurl=https%3A%2F%2Fwww.hacksawgaming.com&currency=EUR&partner=demo&env=https://rgs-demo.hacksawgaming.com/api&realmoneyenv=https://rgs-demo.hacksawgaming.com/api")]
    public async Task GrabOnlyUrls_PlaywrightMethod(string url)
    {
        var init = new WebAssetScout();
        
        var result = await init.CaptureNetworkAssetsAsync(url);
        
        string jsonMap = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        
        _output.WriteLine(jsonMap);
        return;
    }
    
    [Theory]
    [InlineData("https://static-live.hacksawgaming.com/1348/1.24.1/index.html?language=en&channel=desktop&gameid=1348&mode=2&token=123131&lobbyurl=https%3A%2F%2Fwww.hacksawgaming.com&currency=EUR&partner=demo&env=https://rgs-demo.hacksawgaming.com/api&realmoneyenv=https://rgs-demo.hacksawgaming.com/api")]
    public async Task GrabAssetsMap_Hacksaw(string url)
    {
        var init = new WebAssetScout();
        var hacksawWorker = new HacksawGrabber(init);

        var result = await hacksawWorker.CreateAssetMap(url);
        
        string jsonMap = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        
        _output.WriteLine(jsonMap);
        return;
    }
    
    [Theory]
    [InlineData("https://static-live.hacksawgaming.com/1348/1.24.1/index.html?language=en&channel=desktop&gameid=1348&mode=2&token=123131&lobbyurl=https%3A%2F%2Fwww.hacksawgaming.com&currency=EUR&partner=demo&env=https://rgs-demo.hacksawgaming.com/api&realmoneyenv=https://rgs-demo.hacksawgaming.com/api")]
    public async Task GrabOnlyUrls_Hacksaw(string url)
    {
        var init = new WebAssetScout();
        var hacksawWorker = new HacksawGrabber(init);

        var result = await hacksawWorker.CreateAssetList(url);
        
        string jsonMap = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        
        _output.WriteLine(jsonMap);
        return;
    }
    
    [Theory]
    [InlineData("https://static-live.hacksawgaming.com/1348/1.24.1/index.html?language=en&channel=desktop&gameid=1348&mode=2&token=123131&lobbyurl=https%3A%2F%2Fwww.hacksawgaming.com&currency=EUR&partner=demo&env=https://rgs-demo.hacksawgaming.com/api&realmoneyenv=https://rgs-demo.hacksawgaming.com/api")]
    public async Task DownloadTest(string url)
    {
        var init = new WebAssetScout();
        var hacksawWorker = new HacksawGrabber(init);

        var result = await hacksawWorker.CreateAssetList(url);

        string jsonMap = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        
        _output.WriteLine(jsonMap);
        
        try
        {
            var downloader = new AssetDownloader("Z:\\temporary\\!AssetsGrabberTest", "hacksaw");
            await downloader.DownloadAllAsync(result);
        }
        catch (Exception ex)
        {
            _output.WriteLine(ex.Message);
        }
        
        return;
    }
    
    public void Dispose()
    {
        // TODO release managed resources here
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Text.Json;
using CreoHub.AssetsGrabber.Models;
using Microsoft.Playwright;

namespace CreoHub.AssetsGrabber;

public class WebAssetScout
{
    private readonly HttpClient _httpClient;

    public WebAssetScout()
    {
        _httpClient = new HttpClient();
        // Добавляем User-Agent, чтобы сайты не думали, что мы злой бот
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }

    private string DetermineLocalPath(string tagName, string fileName)
    {
        string folder = tagName switch
        {
            "img" => "images",
            "link" => "css",
            "script" => "js",
            _ => "misc"
        };
        return $"/assets/{folder}/{fileName}";
    }

    public async Task<List<AssetMapItem>> CaptureNetworkAssetsAsync(string targetUrl)
    {
        var map = new List<AssetMapItem>();
    
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();

        // Подписываемся на все сетевые запросы браузера
        page.Request += (sender, request) =>
        {
            var url = request.Url;
            // Фильтруем только статические файлы, которые нам интересны
            if (IsAsset(url))
            {
                var uri = new Uri(url);
                var fileName = Path.GetFileName(uri.LocalPath);
            
                lock(map) // На всякий случай, запросы идут параллельно
                {
                    if (!map.Any(m => m.RemoteUrl == url))
                    {
                        map.Add(new AssetMapItem
                        {
                            Name = fileName,
                            RemoteUrl = url,
                        });
                    }
                }
            }
        };

        // Переходим на страницу и ждем, пока сеть "успокоится" (все ассеты докачаются)
        await page.GotoAsync(targetUrl, new PageGotoOptions 
        { 
            WaitUntil = WaitUntilState.NetworkIdle, // Ждем, пока запросы утихнут
            Timeout = 100000 
        });
    
        // Даем еще пару секунд на случай ленивой загрузки
        await Task.Delay(10000); 

        return map;
    }

    private bool IsAsset(string url)
    {
        // Игнорируем API запросы и логи, берем только файлы
        //string[] extensions = { ".png", ".jpg", ".jpeg", ".json", ".js", ".css", ".atlas", ".mp3", ".wav", ".woff2" };
        string[] extensions = { ".png", ".jpg", ".jpeg", ".json", ".atlas", ".webp" };
        return extensions.Any(ext => url.Contains(ext, StringComparison.OrdinalIgnoreCase));
    }
}


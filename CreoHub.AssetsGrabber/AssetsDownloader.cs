
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;

namespace CreoHub.AssetsGrabber;

public class AssetDownloader
{
    private readonly HttpClient _httpClient;
    private readonly string _outputFolderName;
    private readonly string _outputRoot;

    public AssetDownloader(string outputRoot, string outputFolderName)
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        _outputRoot = outputRoot;
        _outputFolderName = outputFolderName;
    }

    public async Task DownloadAllAsync(List<string> urls)
    {
        // Качаем параллельно по 5 файлов за раз, чтобы сервер не забанил
        var options = new ParallelOptions { MaxDegreeOfParallelism = 5 };
        
        await Parallel.ForEachAsync(urls, options, async (url, token) =>
        {
            try
            {
                await DownloadFileAsync(url);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке {url}: {ex.Message}");
            }
        });
    }

    private async Task DownloadFileAsync(string url)
    {
        var uri = new Uri(url);
        
        // 1. Создаем локальный путь, повторяя структуру URL после домена
        // Например: /1348/1.24.1/assets/...
        string relativePath = _outputFolderName + "/" + url.Split("/")[^1];
        string fullPath = Path.Combine(_outputRoot, relativePath);
        
        // 2. Создаем директории, если их нет
        string directory = Path.GetDirectoryName(fullPath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // 3. Скачиваем файл
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        using var fs = new FileStream(fullPath, FileMode.Create);
        await response.Content.CopyToAsync(fs);
        
        Console.WriteLine($"[OK] {relativePath}");
    }
}
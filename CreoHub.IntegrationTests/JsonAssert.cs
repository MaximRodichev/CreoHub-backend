using System.Text.Json;

namespace CreoHub.IntegrationTests;

public static class JsonAssert
{
    public static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json);
    }

    public static JsonElement RequiredProperty(this JsonElement element, string name)
    {
        Assert.True(element.TryGetProperty(name, out var property), $"Expected JSON property '{name}'.");
        return property;
    }
}

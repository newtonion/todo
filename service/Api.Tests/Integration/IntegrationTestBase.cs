using System.Text.Json;
using Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests.Integration;

public abstract class IntegrationTestBase
{
    protected static readonly Guid OtherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    protected static async Task<Guid> ReadIdAsync(HttpResponseMessage response)
    {
        using var json = await ReadJsonAsync(response);
        return GetGuid(json.RootElement, "id");
    }

    protected static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }

    protected static async Task<TodoDatabaseContext> CreateDbContextAsync(TodoApiFactory factory)
    {
        var dbContextFactory = factory.Services.GetRequiredService<IDbContextFactory<TodoDatabaseContext>>();
        return await dbContextFactory.CreateDbContextAsync();
    }

    protected static JsonElement GetProperty(JsonElement element, string name)
    {
        if (element.TryGetProperty(name, out var value))
        {
            return value;
        }

        var pascalName = char.ToUpperInvariant(name[0]) + name[1..];
        return element.GetProperty(pascalName);
    }

    protected static string GetString(JsonElement element, string name)
    {
        return GetProperty(element, name).GetString()!;
    }

    protected static int GetInt32(JsonElement element, string name)
    {
        return GetProperty(element, name).GetInt32();
    }

    protected static Guid GetGuid(JsonElement element, string name)
    {
        return GetProperty(element, name).GetGuid();
    }

    protected static bool GetBoolean(JsonElement element, string name)
    {
        return GetProperty(element, name).GetBoolean();
    }

    protected static DateTime GetDateTime(JsonElement element, string name)
    {
        return GetProperty(element, name).GetDateTime();
    }
}

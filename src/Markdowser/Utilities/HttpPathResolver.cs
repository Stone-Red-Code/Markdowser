using Avalonia.Platform;

using Markdown.Avalonia.Utils;
using Markdowser.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Markdowser.Utilities;

public class HttpPathResolver : IPathResolver
{
    private readonly HttpClient httpClient = new();
    public string? AssetPathRoot { get; set; }
    public IEnumerable<string>? CallerAssemblyNames { get; set; }

    public async Task<Stream?>? ResolveImageResource(string relativeOrAbsolutePath)
    {
        if (string.IsNullOrWhiteSpace(relativeOrAbsolutePath))
        {
            return GetLogo();
        }

        if (relativeOrAbsolutePath.StartsWith("avares://"))
        {
            return AssetLoader.Open(new Uri(relativeOrAbsolutePath))!;
        }

        if (!Uri.IsWellFormedUriString(relativeOrAbsolutePath, UriKind.Absolute))
        {
            relativeOrAbsolutePath = new Uri(new Uri(GlobalState.Url), relativeOrAbsolutePath).ToString();
        }

        Debug.WriteLine($"Resolving image: {relativeOrAbsolutePath}");

        HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(relativeOrAbsolutePath)!;

        if (!httpResponseMessage.IsSuccessStatusCode)
        {
            return GetLogo();
        }

        return await httpResponseMessage.Content.ReadAsStreamAsync();
    }

    private static Stream GetLogo()
    {
        if (Settings.Current.DarkMode)
        {
            return AssetLoader.Open(new Uri("avares://Markdowser/Assets/Markdowser-Dark-Transparent.png"))!;
        }

        return AssetLoader.Open(new Uri("avares://Markdowser/Assets/Markdowser-Light-Transparent.png"))!;
    }
}
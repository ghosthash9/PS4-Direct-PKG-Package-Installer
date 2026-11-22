using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DirectPackageInstaller.Tasks;

/// <summary>
/// A package found on a remote file server.
/// </summary>
/// <param name="Url">Full (escaped) download URL.</param>
/// <param name="Path">Readable path relative to the server root (folder/file.pkg).</param>
/// <param name="Size">Size in bytes when the server told us.</param>
public sealed record ServerEntry(string Url, string Path, long? Size)
{
    public string Display => Size is { } S ? $"{Path} ({ServerListing.FormatSize(S)})" : Path;
}

/// <summary>
/// Lists the .pkg files of a remote server. Two formats are understood:
///
/// 1) JSON (recommended), for example:
///    { "files": [ { "path": "God of War/GoW.pkg", "size": 123456, "title": "God of War" } ] }
///    Also accepted: a plain array of strings/objects, nested folders ("children"/"files"/"items"),
///    and absolute "url" values. Relative values are resolved against the server URL and must be RAW
///    (not percent-encoded), because each segment is escaped here.
///
/// 2) HTML directory listing (python http.server, nginx/apache autoindex...): the links are followed
///    recursively (same host, limited depth) and every link ending in .pkg is listed.
/// </summary>
public static class ServerListing
{
    private const int MaxDepth = 4;
    private const int MaxRequests = 300;

    private static readonly string[] FullRefKeys = { "url", "href", "link", "path", "file", "filename", "p" };
    private static readonly string[] SizeKeys = { "size", "bytes", "length", "s" };

    private static readonly Regex HrefRegex = new Regex("href\\s*=\\s*[\"']([^\"']+)[\"']", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly HttpClient Http = CreateClient();

    private static HttpClient CreateClient()
    {
        var Client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All })
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        Client.DefaultRequestHeaders.UserAgent.ParseAdd("DirectPackageInstaller");
        Client.DefaultRequestHeaders.Accept.ParseAdd("application/json, text/html;q=0.9, */*;q=0.1");
        return Client;
    }

    public static async Task<List<ServerEntry>> FetchPackages(string ServerUrl, CancellationToken Token = default)
    {
        if (!Uri.TryCreate(ServerUrl?.Trim(), UriKind.Absolute, out var Start) ||
            (Start.Scheme != Uri.UriSchemeHttp && Start.Scheme != Uri.UriSchemeHttps))
            throw new ArgumentException("Invalid server URL");

        var (Body, Final) = await Get(Start, Token);

        List<ServerEntry> Entries;
        var Trimmed = Body.TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
        if (Trimmed.StartsWith("{") || Trimmed.StartsWith("["))
            Entries = ParseJson(Trimmed, Final);
        else
        {
            Entries = await ParseHtmlTree(Body, Final, Token);

            //Pages built by JavaScript (like an index.html + manifest.json) have no links to follow
            if (Entries.Count == 0)
                Entries = await TryManifest(Final, Token) ?? Entries;
        }

        return Entries
            .GroupBy(x => x.Url)
            .Select(x => x.First())
            .OrderBy(x => x.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Looks for a manifest.json next to the page (or one folder above, which is where a browser would look
    /// when the page is opened without the trailing slash). Format: [{"p": "folder/file.pkg", "s": size}, ...]
    /// </summary>
    private static async Task<List<ServerEntry>?> TryManifest(Uri PageUrl, CancellationToken Token)
    {
        var Dir = AsDirectory(PageUrl);
        var Candidates = new List<Uri> { Dir };

        if (Dir.AbsolutePath.Length > 1)
            Candidates.Add(new Uri(Dir, "../"));

        foreach (var Candidate in Candidates)
        {
            try
            {
                var (Body, Final) = await Get(new Uri(Candidate, "manifest.json"), Token);
                var Entries = ParseJson(Body.TrimStart('\uFEFF'), Candidate);

                if (Entries.Count > 0)
                    return Entries;
            }
            catch (HttpRequestException) { }
            catch (JsonException) { }
        }

        return null;
    }

    private static async Task<(string Body, Uri Final)> Get(Uri Url, CancellationToken Token)
    {
        using var Response = await Http.GetAsync(Url, HttpCompletionOption.ResponseHeadersRead, Token);

        if (!Response.IsSuccessStatusCode)
            throw new HttpRequestException($"The server answered {(int)Response.StatusCode} {Response.ReasonPhrase} for {Url}");

        var Body = await Response.Content.ReadAsStringAsync(Token);
        return (Body, Response.RequestMessage?.RequestUri ?? Url);
    }

    /// <summary>
    /// The "directory" that relative paths are resolved against: always ends with '/'
    /// and drops a trailing file name such as index.html or manifest.json.
    /// </summary>
    public static Uri AsDirectory(Uri Url)
    {
        var Builder = new UriBuilder(Url) { Query = string.Empty, Fragment = string.Empty };
        var Path = Builder.Path;

        if (!Path.EndsWith("/"))
        {
            var Last = Path.Substring(Path.LastIndexOf('/') + 1);
            Path = Last.Contains('.') ? Path.Substring(0, Path.LastIndexOf('/') + 1) : Path + "/";
        }

        Builder.Path = Path;
        return Builder.Uri;
    }

    private static string EscapePath(string Path) =>
        string.Join("/", Path.Split('/').Select(Uri.EscapeDataString));

    private static bool IsPkg(string? Name) =>
        Name != null && Name.EndsWith(".pkg", StringComparison.OrdinalIgnoreCase);

    #region JSON

    public static List<ServerEntry> ParseJson(string Json, Uri ServerUrl)
    {
        var Result = new List<ServerEntry>();
        var Base = AsDirectory(ServerUrl);

        using var Doc = JsonDocument.Parse(Json, new JsonDocumentOptions { AllowTrailingCommas = true });
        WalkJson(Doc.RootElement, string.Empty, Base, Result, 0);
        return Result;
    }

    private static string? GetString(JsonElement Obj, string Key)
    {
        foreach (var Prop in Obj.EnumerateObject())
            if (Prop.Name.Equals(Key, StringComparison.OrdinalIgnoreCase) && Prop.Value.ValueKind == JsonValueKind.String)
                return Prop.Value.GetString();
        return null;
    }

    private static long? GetSize(JsonElement Obj)
    {
        foreach (var Key in SizeKeys)
        foreach (var Prop in Obj.EnumerateObject())
        {
            if (!Prop.Name.Equals(Key, StringComparison.OrdinalIgnoreCase))
                continue;

            if (Prop.Value.ValueKind == JsonValueKind.Number && Prop.Value.TryGetInt64(out var Num))
                return Num;

            if (Prop.Value.ValueKind == JsonValueKind.String && long.TryParse(Prop.Value.GetString(), NumberStyles.None, CultureInfo.InvariantCulture, out Num))
                return Num;
        }
        return null;
    }

    private static string Combine(string Folder, string Name) =>
        string.IsNullOrEmpty(Folder) ? Name : Folder.TrimEnd('/') + "/" + Name.TrimStart('/');

    private static void WalkJson(JsonElement Element, string Folder, Uri Base, List<ServerEntry> Result, int Depth)
    {
        if (Depth > 12)
            return;

        switch (Element.ValueKind)
        {
            case JsonValueKind.Array:
                foreach (var Item in Element.EnumerateArray())
                {
                    if (Item.ValueKind == JsonValueKind.String)
                    {
                        var Text = Item.GetString();
                        if (IsPkg(Text))
                            AddJsonEntry(Text!, false, Folder, null, Base, Result);
                    }
                    else
                        WalkJson(Item, Folder, Base, Result, Depth + 1);
                }
                break;

            case JsonValueKind.Object:
                string? Ref = null;
                bool IsName = false;

                foreach (var Key in FullRefKeys)
                {
                    Ref = GetString(Element, Key);
                    if (!string.IsNullOrWhiteSpace(Ref))
                        break;
                }

                if (string.IsNullOrWhiteSpace(Ref))
                {
                    Ref = GetString(Element, "name");
                    IsName = true;
                }

                if (IsPkg(Ref))
                {
                    AddJsonEntry(Ref!, IsName, Folder, GetSize(Element), Base, Result);
                    return;
                }

                //Not a package, maybe a folder: the child names are relative to it
                var ChildFolder = Folder;
                if (!string.IsNullOrWhiteSpace(Ref) && !Ref!.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    ChildFolder = IsName ? Combine(Folder, Ref) : Ref;

                foreach (var Prop in Element.EnumerateObject())
                    if (Prop.Value.ValueKind is JsonValueKind.Array or JsonValueKind.Object)
                        WalkJson(Prop.Value, ChildFolder, Base, Result, Depth + 1);
                break;
        }
    }

    private static void AddJsonEntry(string Ref, bool IsName, string Folder, long? Size, Uri Base, List<ServerEntry> Result)
    {
        if (Uri.TryCreate(Ref, UriKind.Absolute, out var Absolute) &&
            (Absolute.Scheme == Uri.UriSchemeHttp || Absolute.Scheme == Uri.UriSchemeHttps))
        {
            Result.Add(new ServerEntry(Absolute.AbsoluteUri, RelativeName(Absolute, Base), Size));
            return;
        }

        string Relative;

        if (Ref.StartsWith("/"))
        {
            //Host-relative: /something/file.pkg
            var HostRelative = new Uri(new Uri(Base.GetLeftPart(UriPartial.Authority)), EscapePath(Ref.TrimStart('/')));
            Result.Add(new ServerEntry(HostRelative.AbsoluteUri, Uri.UnescapeDataString(Ref.TrimStart('/')), Size));
            return;
        }

        Relative = IsName ? Combine(Folder, Ref) : Ref;
        Relative = Relative.Replace('\\', '/').TrimStart('/');

        var Url = new Uri(Base, EscapePath(Relative));
        Result.Add(new ServerEntry(Url.AbsoluteUri, Relative, Size));
    }

    private static string RelativeName(Uri Url, Uri Base)
    {
        var BasePath = Base.AbsolutePath;
        var Path = Url.AbsolutePath;

        if (Url.Authority.Equals(Base.Authority, StringComparison.OrdinalIgnoreCase) &&
            Path.StartsWith(BasePath, StringComparison.OrdinalIgnoreCase))
            Path = Path.Substring(BasePath.Length);
        else
            Path = Path.Substring(Path.LastIndexOf('/') + 1);

        return Uri.UnescapeDataString(Path);
    }

    #endregion

    #region HTML

    private static async Task<List<ServerEntry>> ParseHtmlTree(string FirstBody, Uri FirstUrl, CancellationToken Token)
    {
        var Result = new List<ServerEntry>();
        var Base = AsDirectory(FirstUrl);
        var Visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { Base.AbsoluteUri };
        int Requests = 1;

        async Task Crawl(Uri Dir, string? Body, int Depth)
        {
            if (Body == null)
            {
                Requests++;
                try { (Body, _) = await Get(Dir, Token); }
                catch (HttpRequestException) { return; } //A folder that can't be opened is just skipped
            }

            foreach (var Hit in ParseHtmlLinks(Body, Dir, Base))
            {
                if (Hit.IsFolder)
                {
                    if (Depth < MaxDepth && Requests < MaxRequests && Visited.Add(Hit.Url.AbsoluteUri))
                        await Crawl(Hit.Url, null, Depth + 1);
                }
                else
                    Result.Add(new ServerEntry(Hit.Url.AbsoluteUri, RelativeName(Hit.Url, Base), null));
            }
        }

        await Crawl(Base, FirstBody, 0);
        return Result;
    }

    public static List<(Uri Url, bool IsFolder)> ParseHtmlLinks(string Html, Uri Dir, Uri Base)
    {
        var Hits = new List<(Uri, bool)>();

        foreach (Match Match in HrefRegex.Matches(Html))
        {
            var Href = WebUtility.HtmlDecode(Match.Groups[1].Value).Trim();

            if (Href.Length == 0 || Href[0] == '#' || Href[0] == '?' ||
                Href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) ||
                Href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!Uri.TryCreate(Dir, Href, out var Link))
                continue;

            if (Link.Scheme != Uri.UriSchemeHttp && Link.Scheme != Uri.UriSchemeHttps)
                continue;

            if (!Link.Authority.Equals(Base.Authority, StringComparison.OrdinalIgnoreCase))
                continue;

            var Path = Link.AbsolutePath;

            if (Path.EndsWith("/"))
            {
                //Folders: only the ones below the server folder (ignores "parent directory" links)
                if (Path.StartsWith(Base.AbsolutePath, StringComparison.OrdinalIgnoreCase) && Path.Length > Base.AbsolutePath.Length)
                    Hits.Add((new UriBuilder(Link) { Query = string.Empty, Fragment = string.Empty }.Uri, true));
            }
            else if (IsPkg(Uri.UnescapeDataString(Path)))
                Hits.Add((new UriBuilder(Link) { Fragment = string.Empty }.Uri, false));
        }

        return Hits;
    }

    #endregion

    public static string FormatSize(long Bytes)
    {
        string[] Units = { "B", "KB", "MB", "GB", "TB" };
        double Value = Bytes;
        int Unit = 0;

        while (Value >= 1024 && Unit < Units.Length - 1)
        {
            Value /= 1024;
            Unit++;
        }

        return Unit == 0
            ? $"{Bytes} B"
            : string.Format(CultureInfo.InvariantCulture, "{0:0.##} {1}", Value, Units[Unit]);
    }
}

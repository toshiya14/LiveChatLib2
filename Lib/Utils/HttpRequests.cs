namespace LiveChatLib2.Utils;

internal static class HttpRequests
{
    public const string userAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:103.0) Gecko/20100101 Firefox/103.0";

    public static async Task<string> DownloadString(string url, Dictionary<string, string>? headers, CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("user-agent", userAgent);
        //client.DefaultRequestHeaders.Add("content-encoding", "utf-8");
        if (headers != null && headers.Count > 0)
        {
            foreach (var header in headers)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        var response = await client.GetAsync(url, cancellationToken);
        var result = await response.Content.ReadAsStringAsync(cancellationToken);
        return result;
    }

    public static async Task<byte[]> DownloadBytes(string url, Dictionary<string, string>? headers, CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("user-agent", userAgent);
        client.DefaultRequestHeaders.Add("content-encoding", "utf-8");
        if (headers != null && headers.Count > 0)
        {
            foreach (var header in headers)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        var response = await client.GetAsync(url, cancellationToken);
        var result = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        return result;
    }

    public static async Task<string> Post(string url, Dictionary<string, string> formData, Dictionary<string, string> headers, CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("user-agent", userAgent);
        client.DefaultRequestHeaders.Add("content-encoding", "utf-8");
        foreach (var header in headers)
        {
            client.DefaultRequestHeaders.Add(header.Key, header.Value);
        }

        var response = await client.PostAsync(url, new FormUrlEncodedContent(formData), cancellationToken);
        var result = await response.Content.ReadAsStringAsync(cancellationToken);

        return result;
    }
}

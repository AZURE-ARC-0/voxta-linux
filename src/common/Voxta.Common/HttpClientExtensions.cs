﻿using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Voxta.Common;

public interface IEventStreamData
{
    public string GetToken();
}

public static class HttpClientExtensions
{
    public static void ConfigureEventStream(this HttpRequestMessage request)
    {
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
    }

    public static async Task<string> ReadEventStream<TDataMessage>(this HttpResponseMessage response, CancellationToken cancellationToken)
    where TDataMessage : IEventStreamData
    {
        using var reader = new StreamReader(await response.Content.ReadAsStreamAsync(cancellationToken));
        var sb = new StringBuilder();
        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line == null) break;
            if (!line.StartsWith("data:")) continue;
            TDataMessage? json;
            try
            {
                json = JsonSerializer.Deserialize<TDataMessage>(line[5..]);
            }
            catch (JsonException exc)
            {
                throw new JsonException("Failed to deserialize:\n" + line[5..], exc);
            }
            if (json == null) break;
            var token = json.GetToken();
            sb.Append(token);
        }
        reader.Close();
        return sb.ToString();
    }
}
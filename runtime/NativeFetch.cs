
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Jupiter.Runtime;

public sealed class NativeFetch
{
  private static readonly HttpClient Client = new()
  {
    Timeout = Timeout.InfiniteTimeSpan
  };

  public async Task<string> Send(
      string url,
      string method,
      string headersJson,
      string body,
      int timeoutMs)
  {
    var timeout = timeoutMs > 0 ? timeoutMs : 30000;

    using var cts = new CancellationTokenSource(timeout);

    try
    {
      using var request = new HttpRequestMessage(
          new HttpMethod(
              string.IsNullOrWhiteSpace(method) ? "GET" : method
          ),
          url
      );

      var headers =
          JsonSerializer.Deserialize<Dictionary<string, string>>(
              string.IsNullOrWhiteSpace(headersJson)
                  ? "{}"
                  : headersJson
          ) ?? new();

      if (request.Method != HttpMethod.Get &&
          request.Method != HttpMethod.Head &&
          body != null)
      {
        var contentType = headers.FirstOrDefault(h =>
            h.Key.Equals(
                "Content-Type",
                StringComparison.OrdinalIgnoreCase
            )).Value;

        request.Content = new StringContent(
            body,
            Encoding.UTF8,
            string.IsNullOrWhiteSpace(contentType)
                ? "application/json"
                : contentType
        );
      }

      foreach (var header in headers)
      {
        if (header.Key.Equals(
            "Content-Type",
            StringComparison.OrdinalIgnoreCase))
          continue;

        if (!request.Headers.TryAddWithoutValidation(
            header.Key, header.Value))
        {
          if (request.Content == null)
            request.Content = new ByteArrayContent(
                Array.Empty<byte>()
            );

          request.Content.Headers.TryAddWithoutValidation(
              header.Key, header.Value
          );
        }
      }

      using var response = await Client.SendAsync(
          request,
          HttpCompletionOption.ResponseContentRead,
          cts.Token
      ).ConfigureAwait(false);

      var responseBody = response.Content == null
          ? ""
          : await response.Content.ReadAsStringAsync(cts.Token)
              .ConfigureAwait(false);

      var responseHeaders = response.Headers
          .Concat(
              response.Content?.Headers ??
              Enumerable.Empty<
                  KeyValuePair<string, IEnumerable<string>>>()
          )
          .GroupBy(h => h.Key, StringComparer.OrdinalIgnoreCase)
          .ToDictionary(
              g => g.Key,
              g => string.Join(", ", g.SelectMany(x => x.Value)),
              StringComparer.OrdinalIgnoreCase
          );

      return JsonSerializer.Serialize(new
      {
        ok = response.IsSuccessStatusCode,
        status = (int)response.StatusCode,
        statusText = response.ReasonPhrase ?? "",
        url = response.RequestMessage?.RequestUri?.ToString() ?? url,
        headers = responseHeaders,
        body = responseBody,
        error = (string?)null
      });
    }
    catch (OperationCanceledException) when (cts.IsCancellationRequested)
    {
      return MakeError(url, "Request timed out", "TimeoutError");
    }
    catch (Exception ex)
    {
      return MakeError(
          url,
          $"{ex.GetType().Name}: {ex.Message}",
          "NetworkError"
      );
    }
  }

  private static string MakeError(
      string url,
      string message,
      string name)
  {
    return JsonSerializer.Serialize(new
    {
      ok = false,
      status = 0,
      statusText = name,
      url,
      headers = new Dictionary<string, string>(),
      body = "",
      error = message
    });
  }
}

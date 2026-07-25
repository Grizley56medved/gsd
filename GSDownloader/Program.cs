using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

string _apiUniversity = "https://www.gosuslugi.ru/api/university-applicant-list/v1/public/2026";

var builder = WebApplication.CreateSlimBuilder(args);

builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("System.Net.Http", LogLevel.Warning);
builder.Logging.AddFilter("Program", LogLevel.Information);

builder.Services.AddHttpClient("DownstreamClient", client => { client.DefaultRequestHeaders.ConnectionClose = false; })
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(15),
        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
        MaxConnectionsPerServer = 300
    });

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

var app = builder.Build();

app.MapPost("/process", async (int[] ids, IHttpClientFactory httpClientFactory, HttpContext context) =>
{
    if (ids == null || ids.Length == 0)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    var httpClient = httpClientFactory.CreateClient("DownstreamClient");
    var responses = new (ApplicantsResponse?, int)[ids.Length];
    using var semaphore = new SemaphoreSlim(50);
    int processedCount = 0;

    var tasks = ids.Select(async (id, index) =>
    {
        await semaphore.WaitAsync(context.RequestAborted);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiUniversity}/competition/{id}/applicants");
            request.Headers.TryAddWithoutValidation("User-Agent", RandomUserAgent.RandomUa.RandomUserAgent);

            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
            processedCount += 1;
            app.Logger.LogInformation("Processed {0} applicants", processedCount);

            if (response.IsSuccessStatusCode)
            {
                responses[index] = (await response.Content.ReadFromJsonAsync(
                    AppJsonSerializerContext.Default.ApplicantsResponse,
                    context.RequestAborted), id);
            }
        }
        catch
        {
            // Игнорируем ошибки для сохранения стабильности
        }
        finally
        {
            semaphore.Release();
        }
    });

    await Task.WhenAll(tasks);
    
    // 1. Указываем заголовок о том, что тело ответа сжато алгоритмом Brotli.
    // Большинство HTTP-клиентов распакуют его автоматически.
    context.Response.ContentType = "application/octet-stream";
    context.Response.Headers.ContentEncoding = "br";

    using var ms = new MemoryStream();

    // 2. Оборачиваем поток в BrotliStream
    using (var brotliStream = new BrotliStream(ms, CompressionLevel.Fastest, leaveOpen: true))
    using (var writer = new BinaryWriter(brotliStream, System.Text.Encoding.UTF8, leaveOpen: true))
    {
        writer.Write(responses.Length);

        for (int i = 0; i < responses.Length; i++)
        {
            var response = responses[i];
            var id = response.Item2;
            var applicants = response.Item1;
            var applicantsCount = applicants?.Applicants?.Count ?? 0;
            
            writer.Write(id);
            writer.Write(applicantsCount);

            for (int index = 0; index < applicantsCount; index++)
            {
                var applicant = applicants!.Applicants![index];
                
                writer.Write(applicant.Rating);
                
                // ОПТИМИЗАЦИЯ: Пишем Priority как byte вместо int (экономия 3 байта)
                writer.Write(applicant.Priority);

                // ОПТИМИЗАЦИЯ: Упаковка всех флагов в 2 байта (ushort) вместо 8 байт
                ushort flags = 0;
                
                // MainTopPriority (биты 0 и 1)
                if (applicant.MainTopPriority == true) flags |= 1 << 0;
                if (applicant.MainTopPriority == false) flags |= 1 << 1; 
                
                // HighestPassagewayPriority (биты 2 и 3)
                if (applicant.HighestPassagewayPriority == true) flags |= 1 << 2;
                if (applicant.HighestPassagewayPriority == false) flags |= 1 << 3;

                // Булевы переменные (биты 4 и 5)
                if (applicant.WithoutTests) flags |= 1 << 4;
                if (applicant.PaidContract)  flags |= 1 << 5;

                // Флаги наличия nullable-данных (биты 6, 7, 8, 9)
                if (applicant.ConsentDate.HasValue)     flags |= 1 << 6;
                if (applicant.AchievementsMark.HasValue) flags |= 1 << 7;
                if (applicant.Consent != null)          flags |= 1 << 8;
                if (applicant.StatusName != null)       flags |= 1 << 9;

                writer.Write(flags);

                // Пишем строку Consent, только если флаг (бит 8) поднят
                if (applicant.Consent != null)
                {
                    writer.Write(applicant.Consent); // BinaryWriter сам запишет длину строки
                }

                // ОПТИМИЗАЦИЯ: ConsentDate пишем как UnixTimeSeconds (4 байта вместо 8 байт)
                if (applicant.ConsentDate.HasValue)
                {
                    long unixSeconds = new DateTimeOffset(applicant.ConsentDate.Value).ToUnixTimeSeconds();
                    writer.Write((int)unixSeconds); 
                }

                if (applicant.AchievementsMark.HasValue)
                {
                    writer.Write(applicant.AchievementsMark.Value);
                }

                // ОПТИМИЗАЦИЯ: StatusId пишем как ushort (2 байта вместо 4)
                writer.Write((ushort)applicant.StatusId);

                // Пишем StatusName, только если флаг (бит 9) поднят
                // if (applicant.StatusName != null)
                // {
                //     writer.Write(applicant.StatusName);
                // }

                writer.Write(applicant.IdApplication);
            }
        }
    } // Здесь все буферы сжатия сбрасываются в ms
    
    ReadOnlyMemory<byte> buffer = ms.GetBuffer().AsMemory(0, (int)ms.Length);
    await context.Response.Body.WriteAsync(buffer, context.RequestAborted);
});

app.Run();

[JsonSerializable(typeof(ApplicantsResponse))]
[JsonSerializable(typeof(int[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}
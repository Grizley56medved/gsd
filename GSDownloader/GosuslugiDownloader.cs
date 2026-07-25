using System.Net;
using System.Text.Json.Serialization;
using Flurl.Http;
using Flurl.Http.Newtonsoft;
using Newtonsoft.Json;

public class ApplicantsResponse
{
    [JsonPropertyName("applicants")]
    public List<ApplicantEntity>? Applicants { get; set; }
}

public class ApplicantEntity
{
    [JsonPropertyName("rating")]
    public int Rating { get; set; }

    [JsonPropertyName("priority")]
    public int Priority { get; set; }
    
    [JsonPropertyName("mainTopPriority")]
    public bool? MainTopPriority { get; set; }
    
    [JsonPropertyName("highestPassagewayPriority")]
    public bool? HighestPassagewayPriority { get; set; }

    [JsonPropertyName("consent")]
    public string? Consent { get; set; }
    
    [JsonPropertyName("consentDate")]
    public DateTime? ConsentDate { get; set; }
    
    [JsonPropertyName("withoutTests")]
    public bool WithoutTests { get; set; }
    
    [JsonPropertyName("achievementsMark")]
    public double? AchievementsMark { get; set; }

    [JsonPropertyName("statusId")]
    public int StatusId { get; set; }

    [JsonPropertyName("statusName")]
    public string? StatusName { get; set; }

    [JsonPropertyName("idApplication")]
    public int IdApplication { get; set; }
    
    [JsonPropertyName("paidContract")]
    public bool PaidContract { get; set; }
}

//
// public class GosuslugiDownloader
// {
//     private readonly string _apiUniversity = "https://www.gosuslugi.ru/api/university-applicant-list/v1/public/2026";
//     
//     private readonly FlurlClient[] _clients;
//     
//     private static FlurlClient CreateClient(string proxy)
//     {
//         if (proxy == null) return new FlurlClient()
//         {
//             Settings =
//             {
//                 JsonSerializer = new NewtonsoftJsonSerializer(),
//             },
//         };
//         
//         var handler = new HttpClientHandler
//         {
//             Proxy = new WebProxy(proxy, true)
//             {
//                 Credentials = new NetworkCredential("grizley", "creativ")
//             },
//             UseProxy = true
//         };
//
//         return new FlurlClient(new HttpClient(handler))
//         {
//             Settings =
//             {
//                 JsonSerializer = new NewtonsoftJsonSerializer()
//             }
//         };
//     }
//     
//     public GosuslugiDownloader()
//     {
//         _clients = new FlurlClient[]
//         {
//             CreateClient(null),
//             // CreateClient("http://46.243.210.226:3128"),
//             // CreateClient("http://89.169.187.144:3128"),
//             // CreateClient("http://46.243.211.229:3128"),
//         };
//     }
//     private int _proxyIndex = -1;
//     
//     private FlurlClient NextClient()
//     {
//         return _clients[0];
//         var index = Interlocked.Increment(ref _proxyIndex);
//         return _clients[index % _clients.Length];
//     }
//     
//     
//     private async Task<(int, List<ApplicantEntity>)> FetchApplicants(int programId, bool rating = false)
//     {
//         var ua = RandomUserAgent.RandomUa.RandomUserAgent;
//
//         try
//         {
//             if (rating)
//             {
//                 var applicants = await NextClient().Request($"{_apiUniversity}/competition/{programId}/ratings")
//                     .WithHeader("User-Agent", ua)
//                     .GetJsonAsync<ApplicantsResponse>();
//
//                 return (programId, applicants.Applicants);
//             }
//             else
//             {
//                 var applicants = await NextClient().Request($"{_apiUniversity}/competition/{programId}/applicants")
//                     .WithHeader("User-Agent", ua)
//                     .GetJsonAsync<ApplicantsResponse>();
//
//                 return (programId, applicants.Applicants);;
//             }
//
//         }
//         catch (Exception ex)
//         {
//             Console.WriteLine(ex);
//             throw;
//         }
//     }
//     
//     public async Task<List<(int, List<ApplicantEntity>)>> Download(int[] programs, CancellationToken token)
//     {
//         var maxConcurrentTasks = 100;
//         var result = new List<(int, List<ApplicantEntity>)>();
//
//         var tasks = new List<Task<(int, List<ApplicantEntity>)>>();
//
//         int completedCount = 0;
//         foreach (var program in programs)
//         {
//             if (token.IsCancellationRequested) return result;
//             
//             if (tasks.Count >= maxConcurrentTasks)
//             {
//                 try
//                 {
//                     var completed = await Task.WhenAny(tasks);
//                     if (completed.IsFaulted)
//                     {
//                         Console.WriteLine("Faulted task");
//                     }
//                     else
//                     {
//                         completedCount += 1;
//                         Console.WriteLine($"Completed {completedCount}/{programs.Length} programs");
//                         result.Add(completed.Result);
//                     }
//
//                     tasks.Remove(completed);
//                 }
//                 catch (Exception ex)
//                 {
//                     Console.WriteLine("Task wait exception.");
//                     // ???
//                 }
//             }
//             
//             tasks.Add(FetchApplicants(program, false));
//         }
//
//         while(tasks.Count != 0)
//         {
//             if (token.IsCancellationRequested) return result;
//             
//             try
//             {
//                 var completed = await Task.WhenAny(tasks);
//                 if (completed.IsFaulted)
//                 {
//                     Console.WriteLine("Faulted task");
//                 }
//                 else
//                 {
//                     completedCount += 1;
//                     Console.WriteLine($"Completed {completedCount}/{programs.Length} programs");
//                     result.Add(completed.Result);
//                 }
//
//                 tasks.Remove(completed);
//             }
//             catch (Exception ex)
//             {
//                 Console.WriteLine("Task wait exception.");
//                 // ???
//             }
//         }
//
//         return result;
//     }
// }
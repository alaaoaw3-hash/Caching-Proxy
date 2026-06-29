using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace Caching_Proxy
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // FIRST: check if the command/request is full and whole
            bool correctRequest = Tools.CheckRequest(args);
            if(!correctRequest) return;

            // Create JSON file that will hold the caches
            if (!File.Exists("cachedRequests.json"))
            {
                File.WriteAllText("cachedRequests.json", "[]");
            }
            
            // preparing and starting to listen
            int port = Convert.ToInt32(args[1]);
            HttpListener listener = new();
            HttpClient client = new();
            listener.Prefixes.Add($"http://localhost:{port}/");
            listener.Start();
            Console.WriteLine($"Started listening on port {port}");
            Console.WriteLine($"On: http://localhost:{port}");
            Console.WriteLine("Press ctrl + c to terminate program");


            // processing incoming requests
            while (true)
            {
                var context = await listener.GetContextAsync();     // <-- the actual HTTP request will be stored here

                if(context.Request.HttpMethod == "GET")
                {
                    string fullUrl = args[3] + (context.Request.Url?.PathAndQuery ?? "");

                    if(Tools.SendCacheIfPossible(context, fullUrl))
                    {
                        continue;
                    }

                    try
                    {
                        using var HttpResponse = await client.GetAsync(fullUrl);
                        context.Response.StatusCode = (int)HttpResponse.StatusCode;
                        Response MyResponse = new()
                        {
                            Body = await HttpResponse.Content.ReadAsStringAsync(),
                            StatusCode = (int)HttpResponse.StatusCode,
                            Headers = new Dictionary<string, string>()
                        };
                        Tools.PopulateHeadersDictionary(MyResponse, HttpResponse);

                        // Caching the request
                        PreviousRequest previousRequest = new(fullUrl, MyResponse);
                        Tools.SaveToJson(previousRequest, false, fullUrl);
    
                        Tools.WriteToClient(context, MyResponse); 
                    }
                    catch(HttpRequestException)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Couldn't get the IP address of the origin. Possible network problem.");
                        Console.ResetColor();

                        context.Response.StatusCode = 503;
                        Tools.WriteToClient(context, new Response{ Body = "A network error happened while the app was trying to get to the origin.", StatusCode = 503});
                        continue;
                    }

                }
                else
                {
                    // Signal request failure
                    context.Response.StatusCode = 405;
                    // Tell what is the only allowed HTTP request method
                    context.Response.Headers.Add("Allow", "GET");

                    Response response = new()
                    {
                      Body = "We don't handle such HTTP methods at the moment.",
                      StatusCode = 405
                    };

                    Tools.WriteToClient(context, response);
                }
            }
            
        }
    }
}

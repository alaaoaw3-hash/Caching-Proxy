using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Caching_Proxy
{
    public class Tools
    {
        static JsonSerializerOptions option = new() { WriteIndented = true };

        public static bool CheckRequest(string[] args)
        {
            if(args.Length == 1 && args[0] == "clear")
            {
                File.WriteAllText("cachedRequests.json", "[]");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Cache cleared.");
                Console.ResetColor();
                return false;   // <-- so that the program just clears the cache without lunching the server
            }
            else if(args.Length < 4 || args.Length > 4)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid number of arguments. Try again");
                Console.ResetColor();
                return false;
            }
            else if(args[0] != "port")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("First argument must be \"port\"");
                Console.ResetColor();
                return false;
            }
            else if(!int.TryParse(args[1], out int portNumber) || !(portNumber > 0 && portNumber < 65536))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Second argument must be an integer");
                Console.ResetColor();
                return false;
            }
            else if(args[2] != "origin")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Third argument must be \"origin\"");
                Console.ResetColor();
                return false;
            }
            else if(!Uri.TryCreate(args[3], UriKind.Absolute, out _))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Fourth argument must be a URL.");
                Console.ResetColor();
                return false;
            }
            return true;
        }
        public static async void SaveToJson(PreviousRequest previousRequest, bool wannaUpdate, string fullUrl = "")
        {
            // Fetching data
            string? jsonFile = await File.ReadAllTextAsync("cachedRequests.json");
            List<PreviousRequest>? listOfPRs = JsonSerializer.Deserialize<List<PreviousRequest>>(jsonFile);

            bool cacheAlreadyExists = false;

            if (!wannaUpdate)   // <-- add new cache for a request
            {
                // precautionary check for cached requests to avoid dubplicate caches
                if(listOfPRs != null && listOfPRs.Count > 0 && !string.IsNullOrEmpty(fullUrl))
                {
                    foreach(PreviousRequest pr in listOfPRs)
                    {
                        if(pr.Request == fullUrl)
                        {
                            cacheAlreadyExists = true;
                        }
                    }
                }
                if (!cacheAlreadyExists)
                {
                    listOfPRs ??= [];
                    listOfPRs.Add(previousRequest);
                    var listSerialized = JsonSerializer.Serialize(listOfPRs, option);
                    await File.WriteAllTextAsync("cachedRequests.json", listSerialized);
                    Console.WriteLine("Request cached successfully.");
                }
                else
                {
                    return;
                }
            }
            else    // <-- update X-Cache header
            {
                if(listOfPRs != null && listOfPRs?.Count > 0)
                {
                    foreach(PreviousRequest loopPR in listOfPRs)
                    {
                        if(loopPR.Request == previousRequest.Request)
                        {
                            loopPR.Response.Headers["X-Cache"] = previousRequest.Response.Headers["X-Cache"];
                            var listSerialized = JsonSerializer.Serialize(listOfPRs, option);
                            File.WriteAllText("cachedRequests.json", listSerialized);
                        }
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("SaveToJson: Can't find the cached request to update.");
                    Console.ResetColor();
                }
            }
        }
        public static void WriteHeaders(HttpListenerContext context, Response MyResponse)
        {
            // Write headers safely, avoiding NRE and restricted header ArgumentExceptions
            if (MyResponse.Headers != null)
            {
                foreach (var header in MyResponse.Headers)
                {
                    try
                    {
                        if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                        {
                            context.Response.ContentType = header.Value;
                        }
                        else if (header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                        {
                            if (long.TryParse(header.Value, out long length))
                            {
                                context.Response.ContentLength64 = length;
                            }
                        }
                        else if (!header.Key.Equals("Keep-Alive", StringComparison.OrdinalIgnoreCase) &&
                                 !header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase) &&
                                 !header.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase))
                        {
                            context.Response.Headers[header.Key] = header.Value;
                        }
                    }
                    catch (ArgumentException)
                    {
                        // Intentional Exception Swallowing
                        // Ignore restricted headers that cannot be set directly
                    }
                }
            }
        }
        public static void WriteToClient(HttpListenerContext context, Response MyResponse)
        {
            WriteHeaders(context, MyResponse);
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(MyResponse.Body ?? string.Empty);
                context.Response.ContentLength64 = buffer.Length;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                Console.WriteLine("Response sent to client successfully."); 
            }
            catch(ArgumentNullException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("The body being read is null or empty");
                Console.ResetColor();
            }
            catch(HttpListenerException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Network connection problem, the client might have disconnected before receiving the response.");
                Console.ResetColor();
            }
            finally
            {
                context.Response.OutputStream.Close();
            }
        }
        public static bool SendCacheIfPossible(HttpListenerContext context, string fullUrl)
        {
            bool hasFoundCache = false;
            // Fetch data from JSON file
            var jsonFile = File.ReadAllText("cachedRequests.json");
            List<PreviousRequest>? JsonFileDeserialized = JsonSerializer.Deserialize<List<PreviousRequest>>(jsonFile);

            if(JsonFileDeserialized != null && JsonFileDeserialized.Count > 0)
            {
                foreach(PreviousRequest pr in JsonFileDeserialized)
                {
                    if(!hasFoundCache && pr.Request == fullUrl)
                    {
                        Console.WriteLine("Request has a cache. Cache will be sent instead.");
                        pr.Response.Headers["X-Cache"] = "Hit";
                        SaveToJson(pr, true);
                        context.Response.StatusCode = pr.Response.StatusCode;
                        WriteToClient(context, pr.Response);
                        hasFoundCache = true;
                    }
                    else if(hasFoundCache)
                    {
                        break;
                    }
                }
            }

            return hasFoundCache;
        }
        public static void PopulateHeadersDictionary(Response MyResponse, HttpResponseMessage HttpResponse)
        {
            foreach (var header in HttpResponse.Headers)
            {
                MyResponse.Headers[header.Key] = string.Join(", ", header.Value);
            }
            foreach(var contentHeader in HttpResponse.Content.Headers)
            {
                MyResponse.Headers[contentHeader.Key] = string.Join(", ", contentHeader.Value); 
            }
            MyResponse.Headers["X-Cache"] = "Miss";
        }
    }
    public class Response
    {
        public int StatusCode {get; set;}
        public Dictionary<string, string> Headers {get; set;}
        public string? Body {get; set;}

    }
    public class PreviousRequest(string? request, Response response)
    {
        public string? Request {get; set;} = request;   
        public Response Response {get; set;} = response;   
    }
}
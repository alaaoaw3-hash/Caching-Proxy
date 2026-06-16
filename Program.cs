using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
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
            
            // preparing and starting listening
            int port = Convert.ToInt16(args[1]);
            List<PreviousRequest> listOfPRs = [];
            HttpListener listener = new();
            listener.Prefixes.Add($"http://localhost:{port}/");
            listener.Start();
            Console.WriteLine($"Started listening on port {port}");
            Console.WriteLine($"On: http://localhost:{port}");
            Console.WriteLine("Press ctrl + c to terminate program");

            // if the user hits ctrl + c, json file should be cleaned up
            Console.CancelKeyPress += (sender, e) => {
                Console.WriteLine("Cleaning up the JSON file...");
                File.WriteAllText("cachedRequests.json", "[]");
                Console.WriteLine("Done.");
                e.Cancel = false;
            };

            // processing incoming requests
            while (true)
            {
                HttpClient client = new();
                var context = await listener.GetContextAsync();     // <-- the actual HTTP request will be stored here
                    Console.WriteLine("Received a context.");
                if(context.Request.HttpMethod == "GET")
                {
                    Console.WriteLine("It is a GET method.");
                    string fullUrl = args[3] + context.Request.Url.PathAndQuery ?? args[3];
                    var response = await client.GetStringAsync(fullUrl); 
                        Console.WriteLine("Got a response from the origin.");
                    // Caching the request
                    PreviousRequest previousRequest = new(fullUrl, response);

                    // save the data to the JSON file
                    listOfPRs.Add(previousRequest);
                    var option = new JsonSerializerOptions { WriteIndented = true };
                    var listSerialized = JsonSerializer.Serialize(listOfPRs, option);
                    File.WriteAllText("cachedRequests.json", listSerialized);
                        Console.WriteLine("Cached the request URL.");

                    // Write the data back to the client    
                    byte[] buffer = Encoding.UTF8.GetBytes(response);
                    context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                    context.Response.OutputStream.Close();  
                }
                else
                {
                    string response = "We don't handle such HTTP request at the moment.";
                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(response);
                    context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                    context.Response.OutputStream.Close();
                }
            }
            
        }
    }
}

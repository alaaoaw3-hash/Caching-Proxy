using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Caching_Proxy
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if(args.Length < 4 || args.Length > 4)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid number of arguments. Try again");
                Console.ResetColor();
            }
            else if(args[0] != "port")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("First argument must be \"port\"");
                Console.ResetColor();
            }
            else if(!int.TryParse(args[1], out _))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Second argument must be an integer");
                Console.ResetColor();
            }
            else if(args[2] != "origin")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Third argument must be \"origin\"");
                Console.ResetColor();
            }
            else if(!Uri.TryCreate(args[4], UriKind.Absolute, out _))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Fourth argument must be a URL.");
                Console.ResetColor();
            }
            int port = 5000;
            HttpListener listener = new();
            listener.Prefixes.Add($"http://localhost:{port}/");
            listener.Start();
            Console.WriteLine("Started listening on port 5000");
            Console.WriteLine("Press ctrl + c to terminate program");

            while (true)
            {
                HttpClient client = new();
                var context = await listener.GetContextAsync();
                if(context.Request.HttpMethod == "GET")
                {
                    string response = "first time writing this.";
                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(response);
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

namespace Caching_Proxy
{
    public class Tools
    {
        public static bool CheckRequest(string[] args)
        {
            if(args.Length < 4 || args.Length > 4)
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
            else if(!int.TryParse(args[1], out _))
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
    }
    public class PreviousRequest(string? request, string response)
    {
        public string? Request {get; set;} = request;   
        public string Response {get; set;} = response;   
    }
}
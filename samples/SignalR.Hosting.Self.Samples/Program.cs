using System;
using System.Diagnostics;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Web.Http.SelfHost;
using SignalR.Hosting.WebApi;
using SignalR.Samples.Raw;

namespace SignalR.Hosting.Self.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            var listener = new ConsoleTraceListener();
            
            // DefaultSelfHost();

            WebApiSelfHost();

            Log("Press 'h' for a list of commands.");

            while (true)
            {
                ConsoleKeyInfo ki = Console.ReadKey(true);
                if (ki.Key == ConsoleKey.Q)
                {
                    break;
                }

                if (ki.Key == ConsoleKey.H)
                {
                    Console.WriteLine("=========================================");
                    Console.WriteLine("                  COMMANDS               ");
                    Console.WriteLine("=========================================");
                    Console.WriteLine("'q' - Close the server");
                    Console.WriteLine("'h' - Show help");
                    Console.WriteLine("'d' - Show debugging info");
                    Console.WriteLine("'c' - Clear console");
                    Console.WriteLine("'b' - Broacast a message to all clients");
                    Console.WriteLine("=========================================");
                }

                if (ki.Key == ConsoleKey.B)
                {
                    var connection = GlobalHost.ConnectionManager.GetConnectionContext<MyConnection>();
                    Log("Sending ping to all clients.");
                    connection.Connection.Broadcast("server ping").Wait();
                    Log("Broadcast complete.");
                }

                if (ki.Key == ConsoleKey.D)
                {
                    if (Debug.AutoFlush)
                    {
                        Debug.Listeners.Remove(listener);
                    }
                    else
                    {
                        Debug.Listeners.Add(listener);
                    }

                    Debug.AutoFlush = !Debug.AutoFlush;
                    Log("Turning debugging {0}.", Debug.AutoFlush ? "on" : "off");
                }

                if (ki.Key == ConsoleKey.C)
                {
                    Console.Clear();
                }
            }
        }

        private static void Log(string value, params object[] args)
        {
            Console.WriteLine("[" + DateTime.Now + "]: " + value, args);
        }

        private static void Log(string value)
        {
            Console.WriteLine("[" + DateTime.Now + "]: " + value);
        }

        private static void WebApiSelfHost()
        {
            Console.WriteLine("=============================");
            Console.WriteLine("        WebApiSelfHost       ");
            Console.WriteLine("=============================");
            var config = new HttpSelfHostConfiguration("http://localhost:8081");
            config.TransferMode = TransferMode.StreamedResponse;
            config.MapConnection<MyConnection>("Echo", "echo/{*operation}");
            config.MapConnection<Raw>("Raw", "raw/{*operation}");
            config.MapHubs();

            var server = new HttpSelfHostServer(config);
            server.OpenAsync().Wait();

            Console.WriteLine("Server running on {0}", config.BaseAddress);
        }

        private static void DefaultSelfHost()
        {
            Console.WriteLine("=============================");
            Console.WriteLine("        DefaultSelfHost      ");
            Console.WriteLine("=============================");
            string url = "http://*:8081/";
            var server = new Server(url);
            server.Configuration.DisconnectTimeout = TimeSpan.Zero;

            // Map connections
            server.MapConnection<MyConnection>("/echo")
                  .MapConnection<Raw>("/raw")
                  .MapHubs();

            server.Start();

            Console.WriteLine("Server running on {0}", url);
        }

        public class MyConnection : PersistentConnection
        {
            protected override Task OnConnectedAsync(IRequest request, string connectionId)
            {
                Console.WriteLine("{0} connected", connectionId);
                return base.OnConnectedAsync(request, connectionId);
            }

            protected override Task OnReceivedAsync(string connectionId, string data)
            {
                return Connection.Broadcast(data);
            }

            protected override Task OnDisconnectAsync(string connectionId)
            {
                Console.WriteLine("{0} left", connectionId);
                return base.OnDisconnectAsync(connectionId);
            }
        }
    }
}

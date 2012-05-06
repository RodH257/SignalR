using System;
using System.Threading;
using System.Threading.Tasks;
using SignalR.Client.Hubs;
using SignalR.Hosting.Memory;
using System.Diagnostics;

namespace SignalR.Client.Samples
{
    class Program
    {
        static void Main(string[] args)
        {
            // RunInMemoryHost();

            // var hubConnection = new HubConnection("http://localhost:40476/");

            //RunDemoHub(hubConnection);

            Repl(args.Length == 0 ? "http://localhost:8081/echo" : args[0]);

            Console.ReadKey();
        }

        private static void RunInMemoryHost()
        {
            var host = new MemoryHost();
            host.MapConnection<MyConnection>("/echo");

            var connection = new Connection("http://foo/echo");

            connection.Received += data =>
            {
                Console.WriteLine(data);
            };

            connection.Start(host).Wait();

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    while (true)
                    {
                        connection.Send(DateTime.Now.ToString());

                        Thread.Sleep(2000);
                    }
                }
                catch
                {

                }
            });
        }
        private static void RunDemoHub(HubConnection hubConnection)
        {
            var demo = hubConnection.CreateProxy("demo");

            demo.On("invoke", i =>
            {
                Console.WriteLine("{0} client state index -> {1}", i, demo["index"]);
            });

            hubConnection.Start().Wait();


            demo.Invoke("multipleCalls").ContinueWith(task =>
            {
                Console.WriteLine(task.Exception);

            }, TaskContinuationOptions.OnlyOnFaulted);

            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(7000);
                hubConnection.Stop();
            });
        }

        private static void Repl(string endpoint)
        {
            var connection = new Connection(endpoint);

            connection.Received += data =>
            {
                Log("[Received]: " + data);
            };

            connection.Reconnected += () =>
            {
                Log("Connection restablished");
            };

            connection.Error += e =>
            {
                Log("[Error]: " + e);
                Console.WriteLine();
            };

            Log("Connecting to {0}...", endpoint);
            connection.Start().Wait();
            Log("Connected with id '{0}'.", connection.ConnectionId);

            string line = null;
            while ((line = Console.ReadLine()) != null)
            {
                connection.Send(line).Wait();
            }
        }

        private static void Log(string value)
        {
            Console.WriteLine("[" + DateTime.Now + "]: " + value);
        }

        private static void Log(string value, params object[] args)
        {
            Console.WriteLine("[" + DateTime.Now + "]: " + value, args);
        }

        public class MyConnection : PersistentConnection
        {
            protected override Task OnConnectedAsync(Hosting.IRequest request, string connectionId)
            {
                Console.WriteLine("{0} Connected", connectionId);
                return base.OnConnectedAsync(request, connectionId);
            }

            protected override Task OnReconnectedAsync(Hosting.IRequest request, System.Collections.Generic.IEnumerable<string> groups, string connectionId)
            {
                Console.WriteLine("{0} Reconnected", connectionId);
                return base.OnReconnectedAsync(request, groups, connectionId);
            }

            protected override Task OnReceivedAsync(string connectionId, string data)
            {
                return Connection.Broadcast(data);
            }
        }
    }
}

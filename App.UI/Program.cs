using App.Business.Concrete;
using App.Entities.Concrete;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace App.UI
{
    public class Program
    {
        static Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        static List<Socket> clientSockets = new List<Socket>();
        const int BUFFER_SIZE = 2048;
        const int PORT = 27001;
        static byte[] buffer = new byte[BUFFER_SIZE];
        static void Main(string[] args)
        {
             Console.Title = "Server App";
             Console.WriteLine(GetAllServicesAsText());
             SetupServer();
           
             Console.ReadLine();
        }
        public static async void TestCall()
        {

            ProductService p=new ProductService();
            var r =await p.GetAll();
            var s = 10;
        }
        private static void SetupServer()
        {
            Console.WriteLine("Setting up server . . . ");
            serverSocket.Bind(new IPEndPoint(IPAddress.Parse("10.2.13.1"), PORT));
            serverSocket.Listen(5);
            serverSocket.BeginAccept(AcceptCallback, null);
        }

        private static void AcceptCallback(IAsyncResult ar)
        {
            Socket socket;
            try
            {
                socket = serverSocket.EndAccept(ar);
                Console.WriteLine($"Connected {socket.RemoteEndPoint}");
            }
            catch (Exception)
            {
                return;
            }

            SendServiceResponseToClient(socket);
            clientSockets.Add(socket);
            socket.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, socket);

            serverSocket.BeginAccept(AcceptCallback, null);
        }

        private static async void ReceiveCallback(IAsyncResult ar)
        {
            Socket current=(Socket)ar.AsyncState;
            int received;
            try
            {
                received=current.EndReceive(ar);
            }
            catch (Exception)
            {
                Console.WriteLine("Client forcefully disconnected");
                current.Close();
                clientSockets.Remove(current);
                return;
            }

            byte[] recBuf = new byte[received];
            Array.Copy(buffer,recBuf,received);
            string msg=Encoding.ASCII.GetString(recBuf);
            Console.WriteLine("Received Request : "+msg);

            if (msg.ToLower() == "exit")
            {
                current.Shutdown(SocketShutdown.Both);
                current.Close();
                clientSockets.Remove(current);
                Console.WriteLine("Client disconnected");
                return;
            }
            else if(!String.IsNullOrEmpty(msg))
            {
                try
                {
                    var result = msg.Split(new [] { ' ' }, 2);
                    if (result.Length >= 2)
                    {
                        var jsonPart = result[1];
                        var subResult = result[0].Split('\\');

                        var className = subResult[0];
                        var methodName = subResult[1];

                        var myType = Assembly.GetAssembly(typeof(ProductService)).GetTypes()
                           .FirstOrDefault(t => t.Name == className + "Service");

                        var myEntityType = Assembly.GetAssembly(typeof(Product)).GetTypes()
                            .FirstOrDefault(a => a.Name == className);


                        var obj = JsonConvert.DeserializeObject(jsonPart, myEntityType);

                        var methods = myType.GetMethods();
                        MethodInfo myMethod = methods.FirstOrDefault(m => m.Name == methodName);

                        object myInstance = Activator.CreateInstance(myType);
                        myMethod.Invoke(myInstance, new object[] { obj }); 


                    }
                    else
                    {
                        result = msg.Split('\\');
                        var className = result[0];
                        var methodName = result[1];

                        var myType = Assembly.GetAssembly(typeof(ProductService)).GetTypes()
                            .FirstOrDefault(t => t.Name==className+"Service");

                        if (myType!=null)
                        {
                            var methods = myType.GetMethods();
                            MethodInfo myMethod=methods.FirstOrDefault(m=>m.Name==methodName);

                            object myInstance=Activator.CreateInstance(myType);

                            object param = null;
                            var jsonString = String.Empty;
                            object objectResponse = null;

                            if (result.Length == 3)
                            {
                                param = int.Parse(result[2]);
                                objectResponse = myMethod.Invoke(myInstance, new object[] { param });
                            }
                            else if(result.Length<=2)
                            {
                                objectResponse=myMethod.Invoke(myInstance, null);
                            }
                   
                            jsonString=JsonConvert.SerializeObject(objectResponse);
                            byte[]data=Encoding.ASCII.GetBytes(jsonString);
                            current.Send(data);
                        }

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            current.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, current);

        }

        private static void SendServiceResponseToClient(Socket socket)
        {
            var result = GetAllServicesAsText();
            byte[] data = Encoding.ASCII.GetBytes(result);
            socket.Send(data);
        }

        private static string GetAllServicesAsText()
        {
            var myTypes = Assembly.GetAssembly(typeof(ProductService)).GetTypes()
                .Where(t => t.Name.EndsWith("Service") && !t.Name.StartsWith("I"));

            var sb = new StringBuilder();
            foreach (var type in myTypes)
            {
                var className = type.Name.Remove(type.Name.Length - 7, 7);
                var methods = type.GetMethods().Reverse().Skip(4);

                foreach (var m in methods)
                {
                    string responseText = $@"{className}\{m.Name}";
                    var parameters = m.GetParameters();
                    foreach (var param in parameters)
                    {
                        if (param.ParameterType != typeof(string) && param.ParameterType.IsClass)
                        {
                            responseText += $@" {param.Name}[json]";
                        }
                        else
                        {
                            responseText += $@"\{param.Name}";
                        }
                    }
                    sb.AppendLine(responseText);
                }

            }
            return sb.ToString();
        }
    }
}

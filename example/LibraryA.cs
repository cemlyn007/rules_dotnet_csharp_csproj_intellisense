using System;
using Grpc.Net.Client;

namespace Project.LibraryA
{
    public class HelloWorld
    {
        public string GetMessage()
        {
            var channel = Grpc.Net.Client.GrpcChannel.ForAddress("http://localhost:50051");
            channel.ShutdownAsync().Wait();
            return "hello";
        }

    }
}
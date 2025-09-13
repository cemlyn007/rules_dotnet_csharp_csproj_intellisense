using System;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            Example.Library.HelloWorldA helloWorldA = new();
            Console.WriteLine(helloWorldA.GetMessage());
            Example.Library.HelloWorldB helloWorldB = new();
            Console.WriteLine(helloWorldB.GetMessage());
            Example.Proto.Thing thing = new();
            thing.Name = "My Special Thing";
            Console.WriteLine(thing.ToString());
            Example.MainFileA.PrintHelloWorld();
        }
    }
}

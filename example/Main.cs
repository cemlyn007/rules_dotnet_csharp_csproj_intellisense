using System;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            Example.Proto.Thing thing = new();
            thing.Name = "My Special Thing";
            Example.LibraryA.HelloWorld helloWorld = new();
            Console.WriteLine(helloWorld.GetMessage());
            Console.WriteLine(thing.ToString());
        }
    }
}

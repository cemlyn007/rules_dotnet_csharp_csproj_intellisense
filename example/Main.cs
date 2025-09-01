using Project;
using System;
using Examples.Proto;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            Examples.Proto.Thing thing = new();
            thing.Name = "My Special Thing";
            Project.LibraryA.HelloWorld helloWorld = new();
            Console.WriteLine(helloWorld.GetMessage());
            Console.WriteLine(thing.ToString());
        }
    }
}

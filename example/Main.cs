using Project.LibraryA;
using System;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            Project.LibraryA.HelloWorld helloWorld = new();
            Console.WriteLine(helloWorld.GetMessage());
        }
    }
}

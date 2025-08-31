using LibraryA;
using System;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            LibraryA.HelloWorld helloWorld = new();
            Console.WriteLine(helloWorld.GetMessage());
        }
    }
}

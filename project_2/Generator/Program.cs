using System;

namespace Generator
{
    class Program
    {
        public static void Main(string[] args)
        {
            Generator gen = new Generator();

            gen.Generate();
            gen.Print();

            Console.Read();
        }
    }
}

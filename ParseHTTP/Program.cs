using Newtonsoft.Json;
using System;

namespace ParseHTTP
{
    class Program
    {
        static void Main(string[] args)
        {
            string fileLocation = "./example.txt";

            var httpParser = new HttpParser(fileLocation);

            Console.WriteLine($"***Request:\n{JsonConvert.SerializeObject(httpParser.HttpRequest, Formatting.Indented)}\n\n");

            Console.WriteLine($"***Response:\n{JsonConvert.SerializeObject(httpParser.HttpRequest, Formatting.Indented)}\n\n");
        }
    }
}

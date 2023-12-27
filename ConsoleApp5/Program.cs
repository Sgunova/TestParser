using System;
using System.Threading.Tasks;

namespace ConsoleApp5
{
    internal class Program
    {
        private static string message;

        async void Main()
        {
            await SaySomething();
            Console.WriteLine(message); // ?
        }

        static async Task<string> SaySomething()
        {
            await Task.Delay(5);
            message = "Hello world!";
            return "Something";
        }
    }
}

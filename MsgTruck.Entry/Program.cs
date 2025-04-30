// See https://aka.ms/new-console-template for more information
namespace MsgTruck.Entry
{
    public static class Program
    {
        /// <summary>
        /// Starts the console entry of the program.
        /// </summary>
        /// <returns></returns>
        public static async Task Main()
        {
            await (new Truck(new ConsoleLogger())).StartAsync();
        }
    }
}

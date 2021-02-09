using Multiverse.API;

namespace Multiverse
{
    public class Program
    {
        public static Bot Bot { get; set; }

        static void Main(string[] args)
        {
            Paths.CheckAll();
            Bot = new Bot();
        }
    }
}

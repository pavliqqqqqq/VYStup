using System;

namespace wannabepapousek
{
    internal class papousek
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Repeater started. Type text and press Enter to repeat. Type 'exit' to quit.");
            while (true)
            {
                string? input = Console.ReadLine();
                if (input == null)
                {
                    // End of input stream (e.g., Ctrl+Z on Windows)
                    break;
                }

                if (string.Equals(input, "exit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                Console.WriteLine(input);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlugExtract
{
    class ConsoleFace
    {
        public static string InputPrompt(string comment) { Console.WriteLine(comment); return Console.ReadLine(); }
        public static string ChoicePrompt(string comment, string[] options)
        {
            Console.WriteLine(comment);
            for ( ; ; )
            {
                string inp = Console.ReadLine();
                foreach (string option in options)
                {
                    if (inp.Equals(option)) return inp;
                }
                Console.WriteLine("Input not recognized. Please try again.");
            }
        }
    }
}

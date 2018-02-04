using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Allows for easy Console.Write with colors
/// </summary>
namespace RubberSharp {
    class ColorConsole {
        public static void Write(ConsoleColor color, string text) {
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ResetColor();
        }
    }
}

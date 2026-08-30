using System;
using Spectre.Console.Cli;

namespace NukeShare.CLI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var app = new CommandApp();
            app.Run(args);
        }
    }

}
using System;

namespace FileRenamer
{
    public class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: dotnet run -- <directory-path> <string-to-remove>");
                return 1;
            }

            var directory = args[0];
            var toRemove = args[1];

            var renamer = new FileRenamer();
            renamer.RenameFiles(directory, toRemove);

            return 0;
        }
    }
}
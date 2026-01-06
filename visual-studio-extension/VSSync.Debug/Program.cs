// This file exists only to satisfy the compiler for the Debug launcher project.
// The actual debugging is done by launching Visual Studio's Experimental Instance.
// See the .csproj StartAction, StartProgram, and StartArguments properties.

namespace VSSync.Debug
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // This is never executed - Visual Studio launches devenv.exe /rootsuffix Exp instead
            System.Console.WriteLine("This project is only used for debugging the VSSync extension.");
            System.Console.WriteLine("Press F5 in Visual Studio to launch the Experimental Instance.");
        }
    }
}

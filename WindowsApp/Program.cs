namespace Sandaab.WindowsApp
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] commandLineArgs)
        {
            var app = new WindowsApp();
            app.Run(commandLineArgs);
        }
    }
}
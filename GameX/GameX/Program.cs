using System;
using System.Windows.Forms;
using System.IO;

namespace GameX
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string projectPath = args[0];

            if (!Directory.Exists(projectPath))
            {
                MessageBox.Show("Could not find project at path '" + projectPath + "'", "Unable to start GameX Editor", MessageBoxButtons.OK);
                return;
            }

            using EditorWindow window = new EditorWindow(projectPath);
            window.Run();
        }
    }
}

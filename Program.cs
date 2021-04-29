using luncher_epic_de_serveur;
using System;
using System.Windows.Forms;

namespace epic_luncher
{
    static class Program
    {
        /// <summary>
        /// Point d'entrée principal de l'application.
        /// </summary>
        static Form appli;
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(appli = new Form1());
        }
    }
}

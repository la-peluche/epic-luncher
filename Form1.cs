using System;
using System.Diagnostics; //utilisation processus
using System.Threading;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Linq;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using System.IO;
using System.Net;
using System.Text;
namespace luncher_epic_de_serveur
{
    //classe représentant un joueur connecté
    public class JoueurConnecte
    {
        public string name { get; set; }
        public string IP { get; set; }
        public DateTime joinDate { get; set; }
        public Bitmap skinImage { get; set; }
        public bool op { get; set; }
    }
    public class EpicList<T> : List<T>
    {
        public event EventHandler OnAdd;
        public event EventHandler OnDel;
        public new void Add(T item) // "new" to avoid compiler-warnings, because we're hiding a method from base-class
        {
            if (null != OnAdd)
            {
                OnAdd(this, null);
            }
            base.Add(item);
        }

        public new void RemoveAt(int index)
        {
            if(null != OnDel)
            {
                OnDel(this, null);
            }
            base.RemoveAt(index);
        }
    }
    public partial class Form1 : Form
    {
        public static Bitmap emptyBmp = new Bitmap(8, 8); //bitmap en cas d erreur
        public static Rectangle cropRect = new Rectangle(8, 8, 8, 8); //carre de rognage du skin
        public static Pen separatorPen = new Pen(Brushes.White, 1); //stilo de délimitaion
        public static Font playersFont = new Font("Calibri", 20, FontStyle.Bold); //ecritutre
        public static Font dateFont = new Font("Calibri", 10, FontStyle.Bold); //ecritutre
        public static JavaScriptSerializer serializer = new JavaScriptSerializer(); //truc que l on a besoin
        public static EpicList<JoueurConnecte> joueursConnectes = new EpicList<JoueurConnecte>(); //listes de joueurs
        public static TextBox textBox1;
        public static RichTextBox RichTextBox1;
        public static Button bouton1;
        public static VScrollBar ScrollBar_joueur;
        public static Thread reboot_prog;
        public static Thread heure_affi;
        public Form1()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            DoubleBuffered = true;
            // Form1
            MinimumSize = new Size(400, 50);
            this.BackColor = Color.Black;
            ClientSize = new Size(1800, 900);
            MaximizeBox = true;
            Name = "Form1";
            Text = "epic luncher de serveur";
            SizeChanged += new EventHandler(form1_SizeChange);
            joueursConnectes.OnAdd += new EventHandler(JoueursConnectes_CollectionChanged);
            joueursConnectes.OnDel += new EventHandler(JoueursConnectes_CollectionChanged);
            // textbox1
            textBox1 = new TextBox();
            textBox1.AcceptsReturn = false;
            textBox1.AcceptsTab = false;
            textBox1.Multiline = false;
            textBox1.ScrollBars = ScrollBars.None;
            textBox1.BackColor = Color.Black;
            textBox1.ForeColor = Color.White;
            textBox1.KeyDown += touche_appuyee_textbox1;
            textBox1.Visible = true;
            // RichTextBox1
            RichTextBox1 = new RichTextBox();
            RichTextBox1.AcceptsTab = true;
            RichTextBox1.ReadOnly = true;
            RichTextBox1.Multiline = true;
            RichTextBox1.WordWrap = false;
            RichTextBox1.ScrollBars = RichTextBoxScrollBars.Both;
            RichTextBox1.BackColor = Color.Black;
            RichTextBox1.ForeColor = Color.White;
            RichTextBox1.Visible = true;
            //Bouton1
            bouton1 = new Button();
            bouton1.Enabled = true;
            bouton1.MouseClick += bouton1_clik;
            bouton1.BackColor = Color.Black;
            bouton1.ForeColor = Color.White;
            bouton1.Size = new Size(50, 40);
            //ScrollBar_joueur
            ScrollBar_joueur = new VScrollBar();
            ScrollBar_joueur.Dock = DockStyle.Left;
            ScrollBar_joueur.ValueChanged += ValeurChange;
            Controls.Add(RichTextBox1);
            Controls.Add(bouton1);
            Controls.Add(ScrollBar_joueur);
            Controls.Add(textBox1);
            FormClosing += formClosing;
            ScrollBar_joueur.Maximum = joueursConnectes.Count * 60 - ClientSize.Height;
            if (ScrollBar_joueur.Maximum <= 0) { ScrollBar_joueur.Maximum = 0; ScrollBar_joueur.Minimum = 0; }
            textBox1.Font = new Font("Calibri", 20, FontStyle.Bold);
            textBox1.Location = new Point(400, ClientSize.Height - textBox1.Height);
            bouton1.Location = new Point(ClientSize.Width - 50, ClientSize.Height - textBox1.Height);
            bouton1.Font = new Font("Calibri", 20, FontStyle.Bold);
            bouton1.Text = "⇩";
            if (ClientSize.Width <= 850 && bouton1.Width > 25)
            {
                bouton1.Width = 0;
            }
            else if (ClientSize.Width >= 875 && bouton1.Width < 25)
            {
                bouton1.Width = 50;
            }
            textBox1.Width = ClientSize.Width - 400 - bouton1.Width;
            RichTextBox1.Font = new Font("Calibri", 15, FontStyle.Bold);
            RichTextBox1.Location = new Point(400, 0);
            RichTextBox1.Width = ClientSize.Width - 400;
            RichTextBox1.Height = ClientSize.Height - textBox1.Height;
            reboot_prog = new Thread(() =>
            {
                while (true) 
                {
                    TimeSpan sommeil = DateTime.Today.AddDays(1) - DateTime.Now;
                    if (TimeSpan.Compare(sommeil, new TimeSpan(0, 15, 0)) == -1)
                    {
                        sommeil = sommeil.Add(new TimeSpan(1, 0, 0, 0));
                    }
                    Thread.Sleep(sommeil.Add(new TimeSpan(0,-2,0)));
                    Programme.serv_ecriture("tellraw @a {\"text\":\"le serveur va redemarrer dans 2min\",\"color\": \"red\"}");
                    Thread.Sleep(110000);
                    Programme.stop_var = false;
                    Programme.reboot();
                }
            });
            heure_affi = new Thread(() =>
            {
                while (true)
                {
                    TimeSpan sommeil = DateTime.Today.AddHours(DateTime.Now.Hour + 1) - DateTime.Now;
                    Thread.Sleep(sommeil);
                    Programme.serv_ecriture("tellraw @a {\"text\":\"Il est " + DateTime.Now.Hour.ToString() + "h00 actuellement\",\"color\": \"light_purple\"}");
                    Thread.Sleep(1000 * 60 * 10);
                }
            });
            heure_affi.Start();
            reboot_prog.Start();
            Programme.main();
        }
        private void ValeurChange(object sender, EventArgs e)
        {
            Invalidate();
        }
        private void bouton1_clik(object sender, MouseEventArgs e)
        {
            if (Programme.auto_scroll && Programme.var_attente_sortie)
            {
                Programme.auto_scroll = false;
                bouton1.BackColor = Color.White;
                bouton1.ForeColor = Color.Black;
                Programme.RichTextBox1_ecriture("[epic launcher]: AutoScroll désactivé");
            }
            else if (Programme.var_attente_sortie)
            {
                Programme.auto_scroll = true;
                bouton1.BackColor = Color.Black;
                bouton1.ForeColor = Color.White;
                Programme.RichTextBox1_ecriture("[epic launcher]: AutoScroll activé");
            }
            else if (!Programme.var_attente_sortie)
            {
                Programme.var_attente_sortie = true;
            }
        }
        private void touche_appuyee_textbox1(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string input = textBox1.Text;
                while (input.StartsWith(" "))
                {
                    input = input.Remove(0, 1);
                }
                Programme.RichTextBox1_ecriture("[Console]:" + input);
                if (textBox1.Text == "reboot")
                {
                    new Thread(() => { Programme.reboot(); }).Start();
                }
                else
                {
                    Programme.Serveur_Jav.StandardInput.WriteLine(input);
                }
                textBox1.Clear();
            }
        }
        private void formClosing(object sender, EventArgs e)
        {
            try
            {
                Programme.Serveur_Jav.Kill();
                Programme.command.Abort();
            }
            catch { }
            reboot_prog.Abort();
            heure_affi.Abort();
            if (!Programme.var_attente_sortie)
            {
                Programme.attente_arret.Abort();
            }
        }
        private void form1_SizeChange(object sender, EventArgs e)
        {
            //zone de texte
            ScrollBar_joueur.Maximum = joueursConnectes.Count * 60*5 - ClientSize.Height+10;
            if (ScrollBar_joueur.Maximum <= 0) { ScrollBar_joueur.Maximum = 0; ScrollBar_joueur.Minimum = 0; }
            textBox1.Font = new Font("Calibri", 20, FontStyle.Bold);
            textBox1.Location = new Point(400, ClientSize.Height - textBox1.Height);
            bouton1.Location = new Point(ClientSize.Width - 50, ClientSize.Height - textBox1.Height);
            bouton1.Font = new Font("Calibri", 20, FontStyle.Bold);
            if (ClientSize.Width <= 850 && bouton1.Width > 25)
            {
                bouton1.Width = 0;
            }
            else if (ClientSize.Width >= 875 && bouton1.Width < 25)
            {
                bouton1.Width = 50;
            }
            textBox1.Width = ClientSize.Width - 400 - bouton1.Width;

            RichTextBox1.Font = new Font("Calibri", 15, FontStyle.Bold);
            RichTextBox1.Location = new Point(400, 0);
            RichTextBox1.Width = ClientSize.Width - 400;
            RichTextBox1.Height = ClientSize.Height - textBox1.Height;
            Invalidate(); //actualisation   
        }
        private void JoueursConnectes_CollectionChanged(object sender, EventArgs e)
        {
            Invalidate(); //actualisation
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            //affichage joueurs
            if (joueursConnectes.Count != 0)
            {
                List<JoueurConnecte> joueursConnectesSorted = new List<JoueurConnecte>(joueursConnectes.OrderBy(a => a.name));
                e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor; //rend l'image redimensionée non floue
                e.Graphics.PixelOffsetMode = PixelOffsetMode.Half; //permet de ne pas prendre des demis pixels en trop ou en moins 
                int posY = 10-ScrollBar_joueur.Value;
                foreach (JoueurConnecte joueur in joueursConnectesSorted)
                {
                    e.Graphics.DrawImage(joueur.skinImage, new Rectangle(20, posY, 40, 40));  //dessiner l'image : ((image originale comprenant tout le skin), (position et taille de l'image(64 x 64 ici car fois 8)), (prise en compte que de la surface voulue), (unité de pixel utillisée))
                    e.Graphics.DrawString(joueur.name, playersFont, Brushes.White, new Point(85, posY + 2));
                    e.Graphics.DrawString("connecte depuis " + joueur.joinDate, dateFont, Brushes.Gray, new Point(85, posY + 30));
                    e.Graphics.DrawLine(separatorPen, new Point(0, posY + 50), new Point(200, posY + 50));
                    posY += 60;
                }
            }
            base.OnPaint(e);
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public static class Programme
    {
        //autre
        public static Process Serveur_Jav = new Process();
        public static Commandes com = new Commandes();
        //variables et tableau
        public static bool auto_scroll = true;
        public static bool var_attente_sortie = true;
        public static bool stop_var;
        public static string[] limit_text = new string[] { "]: " };
        public static char[] limit_com = new char[] { ' ' };
        public static Thread command;
        public static Thread attente_arret;
        //lancement
        public static void main()
        {
            init();
        }
        public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target) //code de copie
        {
            foreach (DirectoryInfo dir in source.GetDirectories())
                try { CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name)); } catch { }
            foreach (FileInfo file in source.GetFiles())
                try { file.CopyTo(Path.Combine(target.FullName, file.Name)); } catch { }
        }
        public static void serv_ecriture(string text) //écriture sur boite de dialogue ET dans le serveur
        {
            Serveur_Jav.StandardInput.WriteLine(text);
            RichTextBox1_ecriture(text);
        }
        public static void RichTextBox1_ecriture(string console) //ecriture uniquement sur boite de dialogue
        {
            Color color_text = identifi_text(console);
            try
            {
                Form1.RichTextBox1.Invoke(new MethodInvoker(delegate
                {
                    Form1.RichTextBox1.SelectionStart = Form1.RichTextBox1.TextLength;
                    Form1.RichTextBox1.SelectionLength = 0;

                    Form1.RichTextBox1.SelectionColor = color_text;
                    Form1.RichTextBox1.AppendText(console + "\r\n");
                    if (auto_scroll)
                    {
                        Form1.RichTextBox1.ScrollToCaret();
                    }
                }));
            }
            catch
            {
                try
                {
                    Form1.RichTextBox1.SelectionStart = Form1.RichTextBox1.TextLength;
                    Form1.RichTextBox1.SelectionLength = 0;
                    Form1.RichTextBox1.SelectionColor = color_text;
                    Form1.RichTextBox1.AppendText(console + "\r\n");
                    if (auto_scroll)
                    {
                        Form1.RichTextBox1.ScrollToCaret();
                    }
                }
                catch { }
            }
        }
        public static Color identifi_text(string text)
        {
            string[] text_ref = { "tellraw", "[epic launcher]:", "Stopping", "Saving", "left the game" , "joined the game", "<", "[Console]:" , "[Server]" , "Done"};
            string[] non_contenu = { "<", "Done" };
            int[] color_ref = {0xffffff, 0xA9FA48 , 0xffa480, 0xFF1010, 0xFF1010 , 0xFFFF00, 0xFFFF00, 0x77FFFF , 0xFFAF22, 0x70B9FF, 0xe172ad };

            int i = Array.FindIndex(text_ref, Texte => text.StartsWith(Texte));
            if (i==-1) 
            {
                try
                {
                    text = text.Split(limit_text, 2, StringSplitOptions.None)[1];
                    i = Array.FindIndex(text_ref, Ref => text.StartsWith(Ref));
                } 
                catch { }
            }
            if (i == -1)
            {
                i = Array.FindIndex(text_ref, Ref => text.Contains(Ref) && !non_contenu.Contains(Ref));
            }
            return Color.FromArgb(color_ref[i+1]);
        }
        public static void attente_sortie()
        {
            var_attente_sortie = false;
            RichTextBox1_ecriture("[epic launcher]: Appuyez sur le bouton ⇩ de l'application pour fermer la fenetre");
            attente_arret = new Thread(() => { while (!var_attente_sortie) { Thread.Sleep(1); } Environment.Exit(0); });
            attente_arret.Start();
        }
        public static void init() //code d initialisation
        {
            string repertoire = Application.StartupPath;
            string args = "";
            string level_name = "world";
            string rep_backup = repertoire + "\\backup\\";
            //creation dossiers backup
            try //verification lignes args
            {
                string[] lines = File.ReadAllLines("server.properties");
                bool[] found = { false, false, false };
                foreach (string lecture in lines)
                {
                    if (lecture.StartsWith("args="))
                    {
                        args = lecture.Remove(0, 5);
                        found[0] = true;
                    }
                    else if (lecture.StartsWith("level-name="))
                    {
                        level_name = lecture.Remove(0, 11);
                        found[1] = true;
                    }
                    else if (lecture.StartsWith("backup="))
                    {
                        rep_backup = lecture.Remove(0, 11);
                        found[2] = true;
                    }
                }
                if (found[0] == false) //gestion des imprévus
                {
                    File.AppendAllLines("server.properties", new string[] { "args=" });
                    RichTextBox1_ecriture("[epic launcher]: Ligne \"args=\" ajoute au fichier server.properties");
                    RichTextBox1_ecriture("[epic launcher]: relancez pour ajouter des arguments");
                    attente_sortie();
                }
            }
            catch
            {
                RichTextBox1_ecriture("[epic launcher]: Fichier propriete illisible");
                attente_sortie();
            }

            

            //lancement serveur avec argument
            if (File.Exists("server.jar"))
            {
                try { CopyFilesRecursively(new DirectoryInfo(repertoire + "\\" + level_name), new DirectoryInfo(rep_backup + DateTime.Now.ToString("yyyyMMdd"))); } catch { }
                DirectoryInfo[] Dir_backup = new DirectoryInfo(rep_backup).GetDirectories();
                if (Dir_backup.Length > 2)
                {
                    for (int i = 0; i < Dir_backup.Length-2; i++)
                    {
                        Directory.Delete(rep_backup+Dir_backup[i].ToString(),true);
                    }
                }
                Serveur_Jav.StartInfo.RedirectStandardOutput = true; //configuartion 
                Serveur_Jav.StartInfo.RedirectStandardInput = true;
                Serveur_Jav.StartInfo.UseShellExecute = false;
                Serveur_Jav.StartInfo.CreateNoWindow = true;
                Serveur_Jav.StartInfo.FileName = "java.exe";
                Serveur_Jav.StartInfo.Arguments = "-jar " + args + " server.jar -nogui";
                Serveur_Jav.OutputDataReceived += Serveur_Jav_Output;
                try
                {
                    RichTextBox1_ecriture("[epic launcher]: Démarage du Serveur");
                    Serveur_Jav.Start();
                    Serveur_Jav.BeginOutputReadLine();
                    stop_var = true;
                }
                catch (Exception u)
                {
                    stop_var = true;
                    RichTextBox1_ecriture("[epic launcher]: " + u.Message);
                    attente_sortie();
                }
            }
            else
            {
                RichTextBox1_ecriture("[epic launcher]: Fichier server.jar inexistant"); //si abs du fichier
                attente_sortie();
            }
        }
        public static void Serveur_Jav_Output(object sender, DataReceivedEventArgs e)
        {
            try
            {
                if (!String.IsNullOrEmpty(e.Data))
                {
                    RichTextBox1_ecriture(e.Data);
                    //fred des command
                    command = new Thread(() =>
                    {
                        string output = "";
                        try
                        {
                            output = e.Data.Split(limit_text, 2, StringSplitOptions.None)[1];
                        }
                        catch
                        {
                            output = e.Data;
                        }
                        if (output.StartsWith("<")) //log est une discussion
                        {
                            if(output.Split('>')[1].Remove(0, 1).StartsWith("!"))
                            {
                                JoueurConnecte player = Form1.joueursConnectes.Find(joueur => joueur.name == output.Remove(0, 1).Split('>')[0]);
                                try
                                {
                                    string[] outputsplit = output.Split('>')[1].Remove(0, 2).Split(limit_com, 2, StringSplitOptions.None);
                                    System.Reflection.MethodInfo meth = typeof(Commandes).GetMethod(outputsplit[0]);
                                    if (meth != null)
                                    {
                                        object[] argument = new object[meth.GetParameters().Length];
                                        argument[0] = player;
                                        if (argument.Length == 2) { argument[1] = outputsplit[1]; }
                                        if (meth.ReturnType == typeof(string))
                                        {
                                            serv_ecriture(meth.Invoke(com, argument).ToString());
                                        }
                                        else if(meth.ReturnType == typeof(void))
                                        {
                                            meth.Invoke(com, argument);
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                        else if (output == "Stopping server") //serveur arret
                        {
                            if (stop_var)
                            {
                                Form1.heure_affi.Abort();
                                Form1.reboot_prog.Abort();
                                Serveur_Jav.WaitForExit();
                                Application.Exit();
                                
                            }
                        }
                        else if (output.Contains("left the game")) //deconnection du joueur
                        {
                            output = output.Split(' ')[0];
                            int index = Form1.joueursConnectes.FindIndex(joueur => joueur.name == output.Split(' ')[0]);
                            if(index != -1)
                            {
                                Form1.joueursConnectes.RemoveAt(index);
                            }
                        }
                        else if (output.Contains("logged")) //connection au serveur
                        {
                            string[] outputsplit = output.Split(']')[0].Split('[');
                            Bitmap head = new Bitmap(8, 8);
                            try
                            {
                                head = new Bitmap(new MemoryStream(Convert.FromBase64String(new WebClient().DownloadString(@"https://minecraft-api.com/api/skins/" + outputsplit[0] + "/head/0/0/8/json").Split('"')[3])));
                            }
                            catch (Exception u)
                            {
                                RichTextBox1_ecriture("[epic launcher]: "+u.Message);   
                            }
                            bool valide = false;
                            try
                            {
                                string[] operateurs = File.ReadAllText("ops.json").Split('}');
                                foreach (string operateur in operateurs )
                                {
                                    if (operateur.Split('"')[7]== outputsplit[0] && int.Parse(operateur.Split('"')[10].Split(',')[0].Remove(0,2))==4 )
                                    {
                                        valide = true;
                                        break;
                                    }
                                }
                            }
                            catch{ }
                            Form1.joueursConnectes.Add(new JoueurConnecte() { name = outputsplit[0], IP = outputsplit[1].Split(':')[0].Remove(0, 1) , joinDate = DateTime.Now, skinImage = head , op = valide});
                            serv_ecriture("tellraw " + outputsplit[0] + " \"utilise \\\"!help\\\" pour decouvrir toutes les commandes supplementaires \\n"+ (Form1.joueursConnectes.Count-1).ToString() + " joueur(s) connecte(s)\"");
                            serv_ecriture("tellraw @a[name=!"+outputsplit[0]+"] \"" + outputsplit[0] + " a enfin rejoind la partie\"");
                        }
                    });
                    command.Start();
                }
            }
            catch { }  
        }

        public static void reboot()
        {
            for (int i = 9; i >= 0; i--)
            {
                serv_ecriture("tellraw @a {\"text\":\"le serveur va redemarrer dans "+i.ToString()+"s\",\"color\": \"red\"}");
                Thread.Sleep(1000);
            }
            Serveur_Jav.StandardInput.WriteLine("stop");
            Form1.joueursConnectes.Clear();
            stop_var = false;
            Serveur_Jav.WaitForExit();
            Serveur_Jav.OutputDataReceived -= Serveur_Jav_Output;
            Serveur_Jav.CancelOutputRead();
            RichTextBox1_ecriture("\r\n[epic launcher]: Redémarrage serveur\r\n");
            init();
        }
    }
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public class Commandes
    {
        public static DataTable dataTable = new DataTable();
        public static string help(JoueurConnecte index) 
        {
            System.Reflection.MethodInfo[] order = typeof(Commandes).GetMethods();
            string mytext = "";
            for (int i = 0; i < order.Length - 4; i++)
            {
                mytext = mytext + "\\n!" + order[i].ToString().Split(' ')[1].Split('(')[0];
            }
            return "tellraw " + index.name + " \"Les commandes supplementaire sont:"+ mytext + "\"";
        }
        public static string amop(JoueurConnecte index)
        {
            if (index.op)
            {
                return "tellraw " + index.name + " \"tu es un operateur du serveur\"";
            }
            else
            {
                return "tellraw " + index.name + " \"tu es un simple joueur du serveur\n bonne partie x)\"";
            }
        }
        public static void reboot(JoueurConnecte index, string commandes)
        { 
            if (index.op && (DateTime.TryParse(commandes, out DateTime settime) || string.IsNullOrEmpty(commandes)) && DateTime.Compare(DateTime.Now , settime)<0 )
            {
                TimeSpan attente = settime.AddSeconds(-10) - DateTime.Now;
                Programme.serv_ecriture("tellraw @a \"le serveur va redemarrer le " + settime.Date.ToLongDateString() + " a " + TimeSpan.FromSeconds((int)settime.TimeOfDay.TotalSeconds).ToString() + " a la demande de " + index.name + "\"");
                Thread.Sleep(attente);
                Programme.reboot();
            }
            else if (index.op)
            {
                Programme.serv_ecriture("tellraw "+index.name+" \"erreur horaire\"");
            }
            else
            {
                Programme.serv_ecriture("tellraw @a \"" + index.name + " a essayer de redemarrer le serveur sans en avoir les droits\"");
            }
        }
        public static void pcshutdown(JoueurConnecte index, string commandes)
        {
            if(index.op && (DateTime.TryParse(commandes, out DateTime settime)|string.IsNullOrEmpty(commandes)) && DateTime.Compare(DateTime.Now, settime) < 0)
            {
                if (!string.IsNullOrEmpty(commandes))
                {
                    Programme.serv_ecriture("tellraw @a \"le pc va s eteindre le "+ settime.Date.ToLongDateString()+" a "+ TimeSpan.FromSeconds((int)settime.TimeOfDay.TotalSeconds).ToString() + " a la demande de " + index.name + "\"");
                    Thread.Sleep(settime - DateTime.Now);
                }
                Programme.serv_ecriture("tellraw @a \"le pc va s eteindre a la demande de " + index.name + "\"");
                Thread.Sleep(2000);
                Form1.reboot_prog.Abort();
                Programme.stop_var = false;
                Programme.Serveur_Jav.StandardInput.WriteLine("stop");
                Programme.Serveur_Jav.WaitForExit();
                Process process = new Process();
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = "/C shutdown /s";
                process.Start();
            }
            else if (index.op)
            {
                Programme.serv_ecriture("tellraw " + index.name + " \"erreur horaire\"");
            }
            else
            {
                Programme.serv_ecriture("tellraw @a \"" + index.name + " a essayer d eteindre le pc sans en avoir les droits\"");
            }
        }
        public static void ping(JoueurConnecte index)
        {
            Ping pingSender = new Ping();
            string IP = index.IP;
            string Nom = index.name;
            byte[] buffer = Encoding.ASCII.GetBytes("atchouuuuuuuuuuuuuuuuuuuuuuuuuum"); //taille packet (ici 32 octets)
            PingReply reply = pingSender.Send(IP, (ushort)(999 * 2.5)/*pas de questions ok?*/, buffer); //envoie du ping
            if (reply.Status == IPStatus.Success)//si reception
            {
                Programme.serv_ecriture("tellraw @a \"le ping de " + Nom + "est de " + reply.RoundtripTime.ToString() + "ms\"");
                if (reply.RoundtripTime < 31)
                {
                    Programme.serv_ecriture("tellraw " + Nom + " \"tu es un(e) petit(e) rapide\"");
                }
            }
            else
            {
                Programme.serv_ecriture("tellraw @a \"impossible  de ping " + Nom + "\"");
                Programme.serv_ecriture("tellraw " + Nom + " \"pour connaitre ton ping:\nouvre l invite de commande\nexecute la commande \\\"ping [adresse du serveur]\\\"\"");
            }
            GC.Collect();
        }
        public static string calculate(JoueurConnecte index, string commande)
        {
            try
            {
                return "tellraw " + index.name + " \"" + commande.Replace(" ", "") + " = " + dataTable.Compute(commande, null).ToString() + "\"";
            }
            catch
            {
                return "tellraw " + index.name + " \"\\\"" + commande + "\\\" n est pas calculable\"";
            }
        }
        public static string link(JoueurConnecte index, string commande) // a refaire si possible
        {
            string[] commandepart = commande.Split( new char[]  {' '}, StringSplitOptions.RemoveEmptyEntries) ;
            if (!commandepart[commandepart.Length-1].StartsWith("http")) { commandepart[commandepart.Length - 1] = "http://" + commandepart[commandepart.Length - 1]; }
            if (commandepart.Length == 2)
            {
                return "tellraw @a  {\"text\":\"" + commandepart[0] + "\",\"underlined\":true,\"clickEvent\":{ \"action\":\"open_url\",\"value\":\"" + commandepart[1]+"\"}}";
            }
            else if (commandepart.Length == 3)
            {
                return "tellraw @a {\"text\":\""+ commandepart[0] + "\",\"underlined\":true,\"color\":\""+ commandepart[1] + "\",\"clickEvent\":{ \"action\":\"open_url\",\"value\":\""+ commandepart[2] + "\"}}";
            }
            return "tellraw "+index.name+" \"!link [texte] [couleur(en_anglais,optionnal)] [lien]\"";
        }
        public static string nowtime(JoueurConnecte index)
        {
            return "tellraw " + index.name + " \"" + TimeSpan.FromSeconds((int)DateTime.Now.TimeOfDay.TotalSeconds).ToString()+"\"";
        }
        public static string gametime(JoueurConnecte index)
        {
            var joueurs = Form1.joueursConnectes;
            string txt = "";
            for (int i = 0; i < joueurs.Count; i++)
            {
                TimeSpan time = DateTime.Now - joueurs[i].joinDate;
                txt =txt+"\\n"+joueurs[i].name+ " a "+ TimeSpan.FromSeconds((int)time.TotalSeconds).ToString()+ " de jeux";
            }
            return "tellraw @a \"depuis leur derniere connection:" + txt + "\"";
        }
        public static string reminder(JoueurConnecte index, string arguments)
        {
            string[] tab_args = arguments.Split(new char[] { ' ' }, 2, StringSplitOptions.None);
            if (DateTime.TryParse(tab_args[0], out DateTime time2alarm))
            {
                if( DateTime.Compare(DateTime.Now, time2alarm) < 0) 
                {
                    time2alarm.AddDays(1);
                }
                if (DateTime.Compare(DateTime.Now, time2alarm) < 0)
                {
                    Programme.serv_ecriture("tellraw " + index.name + " \"le rappel est enregiste pour le "+time2alarm.Date.ToLongDateString()+" a " + TimeSpan.FromSeconds((int) time2alarm.TimeOfDay.TotalSeconds).ToString() + "\"");
                    Thread.Sleep(time2alarm - DateTime.Now);
                    return "tellraw " + index.name + " \"Rappel : " + tab_args[1] + "\"";
                }
                else
                {
                    return "tellraw " + index.name +" \"veuillez choisir un horaire ulterieur";
                }
            }
            return "tellraw "+index.name+" \"!reminder [heures:minutes] [texte]\"";
        }
    }
}
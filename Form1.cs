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
        public string joinDate { get; set; }
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
        public static JavaScriptSerializer serializer = new JavaScriptSerializer(); //truc que l on a besoin
        public static EpicList<JoueurConnecte> joueursConnectes = new EpicList<JoueurConnecte>(); //listes de joueurs
        public static TextBox textBox1;
        public static TextBox textBox2;
        public static ListBox affi_joueur;

        public Form1()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            SuspendLayout();
            DoubleBuffered = true;
            // Form1
            MinimumSize = new Size(500, 200);
            BackColor = Color.Black;
            ClientSize = new Size(600, 300);
            MaximizeBox = false;
            Name = "Form1";
            Text = "epic luncher de serveur";
            ResumeLayout(false);

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
            // textbox2
            textBox2 = new System.Windows.Forms.TextBox();
            SuspendLayout();
            textBox2.AcceptsReturn = true;
            textBox2.AcceptsTab = true;
            textBox2.ReadOnly = true;
            textBox2.Multiline = true;
            textBox2.WordWrap = false;
            textBox2.ScrollBars = ScrollBars.Both;
            textBox2.BackColor = Color.Black;
            textBox2.ForeColor = Color.White;
            textBox2.Visible = true;

            Controls.Add(textBox1);
            Controls.Add(textBox2);

            FormClosed += formClosed;
            Commandes.prog.main();
        }
        private void touche_appuyee_textbox1(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                Programme.Serveur_Jav.StandardInput.WriteLine(textBox1.Text);

                textBox1.Clear();
            }
        }

        private void formClosed(object sender, EventArgs e)
        {
            try
            {
                Programme.Serveur_Jav.StandardInput.WriteLine("stop");
            }
            catch { }
        }

        private void form1_SizeChange(object sender, EventArgs e)
        {
            Invalidate(); //actualisation   
        }

        private void JoueursConnectes_CollectionChanged(object sender, EventArgs e)
        {
            Invalidate(); //actualisation
            
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            //zone de texte
            textBox1.Font = new Font("Calibri", 20, FontStyle.Bold);
            textBox1.Location = new Point(2 * ClientSize.Width / 5, ClientSize.Height - textBox1.Height);
            textBox1.Width = 3 * ClientSize.Width / 5;

            textBox2.Font = new Font("Calibri", 15, FontStyle.Bold);
            textBox2.Location = new Point(2 * ClientSize.Width / 5, 0);
            textBox2.Width = 3 * ClientSize.Width / 5;
            textBox2.Height = ClientSize.Height - textBox1.Height;

            //affichage joueurs
            if (joueursConnectes.Count != 0)
            {
                List<JoueurConnecte> joueursConnectesSorted = new List<JoueurConnecte>(joueursConnectes.OrderBy(a => a.name));
                e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor; //rend l'image redimensionée non floue
                e.Graphics.PixelOffsetMode = PixelOffsetMode.Half; //permet de ne pas prendre des demis pixels en trop ou en moins 

                int posY = 10;
                foreach (JoueurConnecte joueur in joueursConnectesSorted)
                {
                    e.Graphics.DrawImage(joueur.skinImage, new Rectangle(10, posY, 40, 40));  //dessiner l'image : ((image originale comprenant tout le skin), (position et taille de l'image(64 x 64 ici car fois 8)), (prise en compte que de la surface voulue), (unité de pixel utillisée))
                    e.Graphics.DrawString(joueur.name, playersFont, Brushes.White, new Point(65, posY + 4));
                    e.Graphics.DrawLine(separatorPen, new Point(0, posY + 50), new Point(2 * ClientSize.Width / 5, posY + 50));

                    posY += 60;
                    //Console.WriteLine(joueur.name + " - " + joueur.IP + " - " + joueur.joinDate);
                }
            }
            base.OnPaint(e);
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////
    public class Programme
    {

        //autre
        public static Process Serveur_Jav = new Process();
        public static Commandes com = new Commandes();
        //variables et tableau
        public bool stop_var = true;

        //lancement
        public void main()
        {
            init();
        }

        public void textbox2_ecriture(string console)
        {
            try
            {
                Form1.textBox2.Invoke(new MethodInvoker(delegate
                {
                    Form1.textBox2.AppendText(console + "\r\n");
                }));
            }
            catch { }
        }
        public void init() //code d initialisation
        {
            string line = "";
            try //verification lignes args
            {
                string[] lines = File.ReadAllLines("server.properties");
                bool found = false;
                foreach (string lecture in lines)
                {
                    if (lecture.Contains("args="))
                    {
                        line = lecture.Remove(0, 5);
                        found = true;
                        break;
                    }
                }
                if (found == false)
                {
                    File.AppendAllLines("server.properties", new string[] { "args=" });
                    textbox2_ecriture("line \"args=\" ajouter au fichier server.properties");
                    textbox2_ecriture("appuyez sur entrer pour continuer, relancez pour ajouter des arguments");
                    while (Console.ReadKey().Key != ConsoleKey.Enter) { }
                }
            }
            catch
            {
                textbox2_ecriture(@"fichier propriété illisible");
            }
            //lancement serveur avec argument
            if (File.Exists("server.jar"))
            {
                Serveur_Jav.StartInfo.RedirectStandardOutput = true; //configuartion 
                Serveur_Jav.StartInfo.RedirectStandardInput = true;
                Serveur_Jav.StartInfo.UseShellExecute = false;
                Serveur_Jav.StartInfo.CreateNoWindow = true;
                Serveur_Jav.StartInfo.FileName = "java.exe";
                Serveur_Jav.StartInfo.Arguments = "-jar " + line + " server.jar -nogui";
                Serveur_Jav.OutputDataReceived += Serveur_Jav_Output;
                Serveur_Jav.Start();
                Serveur_Jav.BeginOutputReadLine();
                stop_var = true;
            }
            else
            {
                textbox2_ecriture("fichier server.jar inexistant"); //si abs du fichier
                textbox2_ecriture("appuyez sur entrer pour continuer");
                Environment.Exit(0);
            }
            line = null;
        }
        public void Serveur_Jav_Output(object sender, DataReceivedEventArgs e)
        {
            try
            {
                if (!String.IsNullOrEmpty(e.Data))
                {
                    textbox2_ecriture(e.Data);

                    //fred des command
                    Thread command = new Thread(() =>
                    {
                        string output = e.Data.Remove(0, 33);
                        
                        if (output.StartsWith("<")) //discussion
                        {
                            if(output.Split('>')[1].Remove(0, 1).StartsWith("!"))
                            {
                                JoueurConnecte player = Form1.joueursConnectes.Find(joueur => joueur.name == output.Remove(0, 1).Split('>')[0]);
                                try
                                {
                                    string[] outputsplit = output.Split('>')[1].Remove(0, 2).Split(' ');
                                    System.Reflection.MethodInfo meth = typeof(Commandes).GetMethod(outputsplit[0]);
                                    if (meth != null)
                                    {
                                        object[] args = new object[meth.GetParameters().Length];
                                        textbox2_ecriture(args.Length.ToString());
                                        textbox2_ecriture(outputsplit.Length.ToString());
                                        if (args.Length == outputsplit.Length)
                                        {
                                            args[0] = player;
                                            for (int i = 1; i < meth.GetParameters().Length; i++)
                                            {
                                                args[i] = Convert.ChangeType(outputsplit[i], meth.GetParameters()[i].ParameterType);
                                            }
                                            if (meth.ReturnType == typeof(string))
                                            {
                                                Serveur_Jav.StandardInput.WriteLine(meth.Invoke(com, args));
                                            }
                                            else
                                            {
                                                meth.Invoke(com, args);
                                            }
                                        }
                                        else
                                        {
                                             string text = meth.ToString().Remove(meth.ToString().IndexOf("Int32"), +7).Remove(0, 5);
                                             Serveur_Jav.StandardInput.WriteLine("tellraw " + player.name + " \"" + text + "\"");
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
                                Serveur_Jav.WaitForExit();
                                Environment.Exit(0);
                            }
                        }
                        else if (output.Contains("left the game")) //deconnection du serveur
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
                            string date = DateTime.Now.ToString("dd/MM/yyyy") + " a " + DateTime.Now.ToString("HH:mm:ss");
                            string[] outputsplit = output.Split(']')[0].Split('[');
                            Bitmap head = new Bitmap(8, 8);
                            try
                            {
                                head = new Bitmap(new MemoryStream(Convert.FromBase64String(new WebClient().DownloadString(@"https://minecraft-api.com/api/skins/" + outputsplit[0] + "/head/0/0/8/json").Split('"')[3])));
                            }
                            catch (Exception u)
                            {
                                textbox2_ecriture(u.Message);   
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
                            Form1.joueursConnectes.Add(new JoueurConnecte() { name = outputsplit[0], IP = outputsplit[1].Split(':')[0].Remove(0, 1) , joinDate = date, skinImage = head , op = valide});

                        }
                    });
                    command.Start();
                }
            }
            catch { }
            
        }

    }
    public class Commandes
    {
        public static Programme prog = new Programme();
        public static DataTable dataTable = new DataTable();
        public static string am_op(JoueurConnecte index)
        {
            if (index.op)
            {
                return "tellraw " + index.name + " \"tu es un majestueux opérateur du serveur\"";
            }
            else
            {
                return "tellraw " + index.name + " \"tu es un simple petit joueur du serveur\"";
            }
        }
        public static void reboot(JoueurConnecte index, int sec)
        {
            int min = 0;
            if (sec < 10) { sec = 10; }
            if (index.op)
            {
                Programme.Serveur_Jav.StandardInput.WriteLine("say le serveur va redémarrer dans "+ sec.ToString()+"s");
                Thread.Sleep((min*60+sec-9)*1000);
                for (int i = 9; i >=0; i--)
                {
                    Programme.Serveur_Jav.StandardInput.WriteLine("say le serveur va redémarrer dans " + i.ToString() + "s");
                    Thread.Sleep(1000);
                }
                Programme.Serveur_Jav.StandardInput.WriteLine("say le serveur va redémarrer a la demande de l opérateur " + index.name);
                Programme.Serveur_Jav.StandardInput.WriteLine("stop");
                Form1.joueursConnectes.Clear();
                prog.stop_var = false;
                Programme.Serveur_Jav.WaitForExit();
                Programme.Serveur_Jav.OutputDataReceived -= new Programme().Serveur_Jav_Output;
                Programme.Serveur_Jav.CancelOutputRead();
                prog.textbox2_ecriture("\r\nchargement serveur\r\n");
                prog.init();
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
                Programme.Serveur_Jav.StandardInput.WriteLine("tellraw @a \"le ping de " + Nom + "est de " + reply.RoundtripTime.ToString() + "ms\"");
                if (reply.RoundtripTime < 31)
                {
                    Programme.Serveur_Jav.StandardInput.WriteLine("tellraw " + Nom + " \"tu es un(e) petit(e) rapide\"");
                }
            }
            else
            {
                Programme.Serveur_Jav.StandardInput.WriteLine("tellraw @a \"impossible  de ping " + Nom + "\"");
                Programme.Serveur_Jav.StandardInput.WriteLine("tellraw " + Nom + " \"recommence quand tu pourras ET cette fois deviens bon\"");
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
                return "tellraw " + index.name + " \"\\\"" + commande + "\\\" n est pas une calculable";
            }
        }
        /*public static string link(JoueurConnecte index, string commande)    A REFAIRE
        {
            string[] commandParts;
            //texte retourné si mauvaise syntaxe
            string syntax = "tellraw " + index.name + " {\"text\":\"Syntaxe :\\n:link \\\"texte a afficher\\\" \\\"link\\\" \\\"(optionnel) couleur / #code hexadecimal\\\"\",\"color\":\"red\"}";
            if (commande.Length > 6 && commande.IndexOf('"') != -1)
            {
                commandParts = commande.Split('"');
                if (commandParts.Length == 5 && commandParts[2] == " " && commandParts[4] == "")
                {
                    if (!commandParts[3].StartsWith("http"))
                    {
                        commandParts[3] = "http://" + commandParts[3];
                    }
                    return "tellraw @a [\"\",{\"text\":\"<" + index.name + "> \"},{\"text\":\"" + commandParts[1] + "\",\"underlined\":true,\"clickEvent\":{\"action\":\"open_url\",\"value\":\"" + commandParts[3] + "\"}}]";
                }
                else
                {
                    if (commandParts.Length == 7 && commandParts[2] == " " && commandParts[4] == " " && commandParts[6] == "")
                    {
                        if (!commandParts[3].StartsWith("http"))
                        {
                            commandParts[3] = "http://" + commandParts[3];
                        }
                        return "tellraw @a [\"\",{\"text\":\"<" + index.name + "> \"},{\"text\":\"" + commandParts[1] + "\",\"underlined\":true,\"color\":\"" + commandParts[5] + "\",\"clickEvent\":{\"action\":\"open_url\",\"value\":\"" + commandParts[3] + "\"}}]";
                    }
                    else
                    {
                        return syntax;
                    }
                }
            }
            else
            {
                return syntax;
            }
        }*/
    }
}
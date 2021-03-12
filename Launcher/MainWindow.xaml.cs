using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Launcher
{
    enum UpdateState { search, available, download, update };

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string m_confUrl = "http://aubega.com/launcher/conf.txt";

        //Variable de configuration
        string m_versionUrl = null, m_zipUrl = null, m_unziPath = null;
        private UpdateState m_updateState;

        public MainWindow()
        {
            InitializeComponent();
            m_updateState = UpdateState.search;

            //Si l'évenement this.Close() est appeler.
            this.Closing += delegate
            {
                if (m_updateState == UpdateState.download) //Si le téléchargement est en cours.
                {
                    if (MessageBox.Show("Le téléchargement sera annuler si vous quittez.", "Voulez vous quitter ?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        this.Close();
                    }
                }
            };

            //Téléchargement du fichier de configuration
            WebClient request = new WebClient();
            request.DownloadFile(m_confUrl, "conf.txt");

            //Lecture du fichier
            StreamReader initReader;
            initReader = new StreamReader("conf.txt");

            string lineRead = initReader.ReadLine();
            if (lineRead != null)
            {
                string[] settingLine = lineRead.Split(' ');

                m_versionUrl = settingLine[0];
                m_zipUrl = settingLine[1];
                m_unziPath = settingLine[2];
            }
            else
                MessageBox.Show("Erreur: Fichier de configuration illisible", "ERREUR", MessageBoxButton.OK, MessageBoxImage.Stop);

            initReader.Close();

            //Vérification de la version, mise à jour ou non.
            if (File.Exists("version.txt")) // Voir si le fichier version.txt existe sur l'ordinateur.
            {
                Stream data = request.OpenRead(m_versionUrl); // Lire la version en ligne du version
                StreamReader reader = new StreamReader(data);
                string onlineVersion = reader.ReadLine();
                reader.Close();

                StreamReader localVersionStream;//Local
                localVersionStream = new StreamReader("version.txt");
                string localVersion = localVersionStream.ReadLine();
                localVersionStream.Close();

                if (onlineVersion == localVersion) //Si il existe, vérifier que le client est à jour
                {
                    //Tout est à jour
                    m_updateState = UpdateState.update;
                }
                else
                {
                    //Mise à jour
                    m_updateState = UpdateState.available;
                    Download();
                }
            }
            else //Si il n'existe pas, alors mise à jour.
            {
                //Mise à jour
                m_updateState = UpdateState.available;
                Download();
            }
        }

        //Update
        private bool Download()
        {
            if (m_updateState == UpdateState.available)
            {
                m_updateState = UpdateState.download;
                Play.Content = "Mise à jour en cours ..."; //On change le texte
                Play.Margin = new Thickness(304, 104, 0, 0);

                WebClient webClient = new WebClient();
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(Completed);
                webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressChanged);
                webClient.DownloadFileAsync(new Uri(m_zipUrl), "update.zip");
            }

            return true;
        }

        private void Completed(object sender, AsyncCompletedEventArgs e)
        {
            Play.Margin = new Thickness(602, 104, 0, 0);
            Play.Content = "Jouer";

            if (Directory.Exists(m_unziPath)) //On supprime l'ancienne version.
            {
                string[] filePaths = Directory.GetFiles(m_unziPath);
                foreach (string filePath in filePaths)
                    File.Delete(filePath);
            }

            Decompress("update.zip", m_unziPath, true);

            //Mettre à jour le version.txt local
            WebClient webClient = new WebClient();
            webClient.DownloadFile(new Uri(m_versionUrl), "version.txt");

            m_updateState = UpdateState.update;
        }

        private void ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Play.Content = "Mise à jour en cours (" + e.ProgressPercentage + "%)";
        }

        //Button Event
        private void WebSite_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://minecraft.net/");
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (m_updateState == UpdateState.update)
            {
                //Lancement de votre exécutable
                System.Diagnostics.ProcessStartInfo start = new System.Diagnostics.ProcessStartInfo();
                start.FileName = ".exe";
                start.WorkingDirectory = m_unziPath;
                System.Diagnostics.Process.Start(start);
            }
        }

        //Unzip
        public bool Decompress(string source, string destinationDirectory, bool deleteOriginal = false)
        {
            //Ouverture d'un nouveau contexte ZipInputStream (Librairie)
            using (var zipStream = new ZipInputStream(File.OpenRead(source)))
            {
                ZipEntry entry = null;
                string tempEntry = string.Empty;

                while ((entry = zipStream.GetNextEntry()) != null)
                {
                    string fileName = System.IO.Path.GetFileName(entry.Name);

                    if (destinationDirectory != string.Empty)
                        Directory.CreateDirectory(destinationDirectory);

                    if (!string.IsNullOrEmpty(fileName))
                    {
                        if (entry.Name.IndexOf(".ini") < 0)
                        {
                            string path = destinationDirectory + @"\" + entry.Name;
                            path = path.Replace("\\ ", "\\");
                            string dirPath = System.IO.Path.GetDirectoryName(path);
                            if (!Directory.Exists(dirPath))
                                Directory.CreateDirectory(dirPath);
                            using (var stream = File.Create(path))
                            {
                                int size = 2048;
                                byte[] data = new byte[2048];

                                byte[] buffer = new byte[size];

                                while (true)
                                {
                                    size = zipStream.Read(buffer, 0, buffer.Length);
                                    if (size > 0)
                                        stream.Write(buffer, 0, size);
                                    else
                                        break;
                                }
                            }
                        }
                    }
                }
            }

            if (deleteOriginal)
                File.Delete(source);

            return true;
        }
    }
}

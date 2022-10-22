using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FolderExtractor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.Windows.Forms.DialogResult _dialogResult =
            System.Windows.Forms.DialogResult.None;
        private string _selectedPath = string.Empty;
        private int _numberOfDuplicates = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Choose(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowser = new();

            var result = folderBrowser.ShowDialog();

            _dialogResult = result;
            _selectedPath = folderBrowser.SelectedPath;
        }

        private void Begin(object sender, RoutedEventArgs e)
        {
            if (_dialogResult == System.Windows.Forms.DialogResult.OK
                && !string.IsNullOrWhiteSpace(_selectedPath) && sender is Button button)
            {
                button.IsEnabled = false;
                List<string> files = RemoveDuplicateSongs();
                string newFolderPath = CreateNewDesktopFolder();
                CopyWithOverwrite(newFolderPath, files);
            }
        }

        private void CopyWithOverwrite(string newFolderPath, List<string> files)
        {
            try
            {
                foreach (var file in files)
                {
                    File.Copy(file, newFolderPath + Path.GetFileName(file), true);
                }

                MessageBox.Show($@"
                    {files.Count} files was copied,
                    {_numberOfDuplicates} duplicate-naming files was ignored,
                    new folder size is about {GetFilesWeight(newFolderPath)}Mb
                ",
                "Success!");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error!");
                throw;
            }
            finally
            {
                _dialogResult = System.Windows.Forms.DialogResult.Cancel;
                ShutDown();
            }
        }

        private static string GetFilesWeight(string newFolderPath)
        {
            float filesWeight = 0;
            FileInfo[] filesInfo = new DirectoryInfo(newFolderPath).GetFiles();

            foreach (var info in filesInfo)
            {
                filesWeight += info.Length / 1048576f; //MB
            }

            return filesWeight.ToString("0.0");
        }

        private static string CreateNewDesktopFolder()
        {
            string newFolder = "music";

            string path = Path.Combine(
               Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
               newFolder + "\\"
            );

            if (!Directory.Exists(path))
            {
                try
                {
                    Directory.CreateDirectory(path);
                    return path;
                }
                catch (IOException iex)
                {
                    Console.WriteLine("IO Error: " + iex.Message);
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("General Error: " + ex.Message);
                    throw;
                }
            }

            return path;
        }

        private List<string> RemoveDuplicateSongs()
        {
            List<string> files = Directory.GetFiles(
                _selectedPath, "*.*", SearchOption.AllDirectories).ToList();
            int filesCount = files.Count;
            List<KeyValuePair<int, string>> nameIndexPairs = new();
            List<int> duplicateIndexes = new();

            for (int i = 0; i < filesCount; i++)
            {
                string name = Path.GetFileName(files[i]);

                if (nameIndexPairs.Any(x => x.Value == name))
                {
                    duplicateIndexes.Add(i);
                }

                nameIndexPairs.Add(new KeyValuePair<int, string>(i, name));
            }

            for (int i = 0; i < filesCount; i++)
            {
                for (int y = 0; y < duplicateIndexes.Count; y++)
                {
                    if (i == duplicateIndexes[y])
                    {
                        files[i] = string.Empty;
                    }
                }
            }

            _numberOfDuplicates = duplicateIndexes.Count;
            files.RemoveAll(x => x == string.Empty);

            return files;
        }

        private static void ShutDown()
        {
            Application.Current.Shutdown();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
        }
    }
}

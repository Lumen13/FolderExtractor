using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Forms;

namespace FolderExtractor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DialogResult _dialogResult = System.Windows.Forms.DialogResult.None;
        private string _selectedPath = string.Empty;
        private int _numberOfDuplicates = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Choose(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderBrowser = new();

            var result = folderBrowser.ShowDialog();

            _dialogResult = result;
            _selectedPath = folderBrowser.SelectedPath;

            if (IsDialogResultOk())
            {
                ElBegin.IsEnabled = true;
            }
        }

        private void Begin(object sender, RoutedEventArgs e)
        {
            if (IsDialogResultOk() && !string.IsNullOrWhiteSpace(_selectedPath))
            {
                var currentThread = Thread.CurrentThread;

                ElBegin.IsEnabled = false;
                ElChoose.IsEnabled = false;
                ElCancel.IsEnabled = true;
                ElProgressBar.Visibility = Visibility.Visible;
                ElProgressText.Visibility = Visibility.Visible;

                new Thread(() =>
                {
                    Work();
                }).Start();
            }
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            ShutDown();
        }

        private void Work()
        {
            List<string> files = RemoveDuplicatesByName();
            string newFolderPath = CreateNewDesktopFolder();
            CopyWithOverwrite(newFolderPath, files);
        }

        private void CopyWithOverwrite(string newFolderPath, List<string> files)
        {
            var dispatcher = System.Windows.Application.Current.Dispatcher;
            float _progressBarStepSize = 100f / files.Count;

            try
            {
                dispatcher.BeginInvoke((MethodInvoker)(() =>
                {
                    ElProgressBar.Value += ElProgressBar.Value;
                }));

                foreach (var file in files)
                {
                    File.Copy(file, newFolderPath + Path.GetFileName(file), true);

                    dispatcher.BeginInvoke((MethodInvoker)(() =>
                    {
                        ElProgressBar.Value += _progressBarStepSize;
                        ElProgressText.Text = $"Progress - {Math.Round(ElProgressBar.Value)}%";
                    }));
                }

                System.Windows.MessageBox.Show($@"
                    {files.Count} file(s) was copied,
                    {_numberOfDuplicates} duplicate-naming file(s) was ignored,
                    new folder size is about {GetFilesWeight(newFolderPath)}Mb
                ",
                "Success!");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Error!");
                throw;
            }
            finally
            {
                _dialogResult = System.Windows.Forms.DialogResult.Cancel;
                dispatcher.BeginInvoke((MethodInvoker)(() =>
                {
                    ShutDown();
                }));
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
                    System.Windows.MessageBox.Show("IO Error: " + iex.Message, "Error!");
                    throw;
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("General Error: " + ex.Message, "Error!");
                    throw;
                }
            }

            return path;
        }

        private List<string> RemoveDuplicatesByName()
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

        private bool IsDialogResultOk() => _dialogResult == System.Windows.Forms.DialogResult.OK;

        private static void ShutDown()
        {
            System.Windows.Application.Current.Shutdown();
        }
    }
}

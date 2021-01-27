using System;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.Linq;
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
using System.IO;
using System.Threading;
using Searcher.ViewModels;

namespace Searcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Класс папки
        /// <summary>
        /// Представляет собой коллекцию папок и файлов
        /// </summary>
        private class Data
        {
            public object Content { get; set; }
            public ObservableCollection<Data> Items { get; private set; }

            public Data()
            {
                this.Items = new ObservableCollection<Data>();
            }

            public Data(object content)
            {
                this.Content = content;
                this.Items = new ObservableCollection<Data>();
            }

            public Data(object content, ObservableCollection<Data> items)
            {
                this.Content = content;
                this.Items = items;
            }
        }
        #endregion

        #region Поля
        private string rootFolder;
        private string regexPattern;
        private int scanedFiles;
        private int matchedFiles;
        private int totalSeconds;
        private bool timeIsStopped;
        private bool isWaiting;
        private static CancellationTokenSource tokenSource = new CancellationTokenSource();
        private static CancellationToken token = tokenSource.Token;
        #endregion

        public MainWindow()
        {
            InitializeComponent();
        }

        #region Основная логика
        private async void GetDirectoriesAsync(string path, Regex regex)
        {
            Pause.IsEnabled = true;
            timeIsStopped = false;

            Data startFolder = new Data();
            var origin = new ObservableCollection<Data>() { startFolder };
            DirectoriesTree.ItemsSource = origin;
            try
            {
                await Task.Run(() => GetDirectories(path, startFolder, origin, regex, token));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.Source, MessageBoxButton.OK, MessageBoxImage.Error);
            }

            timeIsStopped = true;
            Pause.IsEnabled = false;
        }

        /// <summary>
        /// Отвечает за получение информации от директорий и вывод ее в файловое дерево
        /// </summary>
        /// <param name="path">Путь для сканирования</param>
        /// <param name="folder">Текущая заполняемая папка</param>
        /// <param name="origin">Изначальная коллекция</param>
        /// <param name="regex">Паттерн для поиска</param>
        /// <param name="token">Токен для отмены асинхронной операции</param>
        private void GetDirectories(
            string path,
            Data folder,
            ObservableCollection<Data> origin,
            Regex regex,
            CancellationToken token)
        {
            do
            {
                if (token.IsCancellationRequested)
                { return; }
                if (!isWaiting)
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(path);

                    folder.Content = directoryInfo.Name;
                    var folders = directoryInfo.GetDirectories();
                    var files = directoryInfo.GetFiles();
                    for (int i = 0; i < folders.Count(); i++)
                    {
                        Dispatcher.Invoke(() => folder.Items.Add(new Data(folders[i].Name)));
                        Dispatcher.Invoke(() => CurrentDirectory.Text = folders[i].FullName);
                        GetDirectories(folders[i].FullName, folder.Items[i], origin, regex, token);
                        Dispatcher.Invoke(() => DirectoriesTree.ItemsSource = origin);
                    }

                    foreach (var item in directoryInfo.GetFiles())
                    {
                        if (regex.IsMatch(item.Name))
                        {
                            Dispatcher.Invoke(() => folder.Items.Add(new Data(item.Name)));
                            Dispatcher.Invoke(() => MatchedFiles.Text = $"Файлов найдено: {++matchedFiles}");
                        }
                        Dispatcher.Invoke(() => ScanedFiles.Text = $"Файлов проверено: {++scanedFiles}");
                    }
                }
            } while (isWaiting);
        }
        #endregion

        #region Таймер
        private async void StartTimeCounterAsync(CancellationToken token)
        {
            await Task.Run(() => StartTimeCounter(token));
        }

        private void StartTimeCounter(CancellationToken token)
        {
            do
            {
                if (token.IsCancellationRequested)
                { return; }
                if (!timeIsStopped)
                {
                    Thread.Sleep(1000);
                    totalSeconds++;
                    Dispatcher.Invoke(() => TimeBox.Text = Convert.ToString(totalSeconds));
                    StartTimeCounter(token);
                }
            } while (timeIsStopped);
        }
        #endregion

        #region Обработчики событий
        /// <summary>
        /// Событие по нажатию кнопки искать
        /// </summary>
        private void Search_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(RootFolderInputField?.Text))
            {
                rootFolder = RootFolderInputField.Text;
                regexPattern = RegexPatternInputField.Text;
                Regex regex = new Regex(regexPattern);
                string[] buffer = new string[2] { rootFolder, regexPattern };
                File.WriteAllLines("DataSave.txt", buffer);
                GetDirectoriesAsync(rootFolder, regex);
                totalSeconds = 0;
                Dispatcher.Invoke(() => TimeBox.Text = Convert.ToString(totalSeconds));
                StartTimeCounterAsync(token);
            }
        }

        /// <summary>
        /// Кнопка над текст боксом
        /// </summary>
        private void RegexPatternInputFieldOverlap_Click(object sender, RoutedEventArgs e)
        {
            string savingsFilePath = "DataSave.txt";
            if (File.Exists(savingsFilePath))
            {
                rootFolder = File.ReadAllLines(savingsFilePath)[1];
                RegexPatternInputField.Text = rootFolder;
            }
            RegexPatternInputFieldOverlap.Content = null;
            RegexPatternInputFieldOverlap.Height = 0;
            RegexPatternInputFieldOverlap.Width = 0;
            RegexPatternInputFieldOverlap.IsEnabled = false;
        }

        /// <summary>
        /// Кнопка над текст боксом
        /// </summary>
        private void RootFolderInputFieldOverlap_Click(object sender, RoutedEventArgs e)
        {
            string savingsFilePath = "DataSave.txt";
            if (File.Exists(savingsFilePath))
            {
                rootFolder = File.ReadAllLines(savingsFilePath)[0];
                RootFolderInputField.Text = rootFolder;
            }
            RootFolderInputFieldOverlap.Content = null;
            RootFolderInputFieldOverlap.Height = 0;
            RootFolderInputFieldOverlap.Width = 0;
            RootFolderInputFieldOverlap.IsEnabled = false;
        }

        /// <summary>
        /// Событие по нажатию на кнопку паузы
        /// </summary>
        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            if (isWaiting)
            { Pause.Content = "Остановить"; timeIsStopped = false; }
            else if (!isWaiting)
            { Pause.Content = "Продолжить"; timeIsStopped = true; }
            isWaiting = !isWaiting;
        }
        #endregion
    }
}
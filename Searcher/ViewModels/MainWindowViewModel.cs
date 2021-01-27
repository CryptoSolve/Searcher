using System;
using System.Collections.Generic;
using System.Text;

namespace Searcher.ViewModels
{
    /// <summary>
    /// Нереализованный задел под MVVM
    /// </summary>
    class MainWindowViewModel : BaseViewModel
    {
        public string ScanedFiles
        {
            get { return scanedFiles; }
            set
            {
                scanedFiles = "Файлов найдено: " + value;
                OnPropertyChanged(nameof(ScanedFiles));
            }
        }
        public string MatchedFiles
        {
            get { return matchedFiles; }
            set
            {
                matchedFiles = "Файлов найдено: " + value;
                OnPropertyChanged(nameof(MatchedFiles));
            }
        }
        private string scanedFiles;
        private string matchedFiles;
    }
}

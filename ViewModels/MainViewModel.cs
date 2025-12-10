using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using System;

namespace HesapMakinesi.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        public CalculatorViewModel CalculatorVM { get; } = new CalculatorViewModel();
        public GraphViewModel GraphVM { get; } = new GraphViewModel();
        public ProgrammerViewModel ProgrammerVM { get; } = new ProgrammerViewModel();

        [ObservableProperty]
        private object _currentView;

        // --- BUTON GÖRÜNÜRLÜKLERİ ---
        [ObservableProperty] private Visibility _historyButtonVisibility = Visibility.Visible;
        [ObservableProperty] private Visibility _graphButtonVisibility = Visibility.Visible;
        [ObservableProperty] private Visibility _programmerButtonVisibility = Visibility.Visible;
        [ObservableProperty] private Visibility _simpleButtonVisibility = Visibility.Collapsed;
        [ObservableProperty] private Visibility _scientificButtonVisibility = Visibility.Visible;

        //Geçmiş Paneli Açık mı? (Dar Mod İçin)
        [ObservableProperty]
        private bool _isHistoryPanelOpen = false;

        public MainViewModel()
        {
            CurrentView = CalculatorVM;
            UpdateModeButtons();
        }

        // --- GÖRÜNÜRLÜK MANTIĞI ---
        public void UpdateModeButtons()
        {
            // 1. Ana Navigasyon Butonları (Hangi moddaysak onun butonunu gizle)
            if (CurrentView == GraphVM)
            {
                HistoryButtonVisibility = Visibility.Collapsed;
                GraphButtonVisibility = Visibility.Collapsed;
                ProgrammerButtonVisibility = Visibility.Visible;
            }
            else if (CurrentView == ProgrammerVM)
            {
                ProgrammerButtonVisibility = Visibility.Collapsed;
                GraphButtonVisibility = Visibility.Visible;
                HistoryButtonVisibility = Visibility.Collapsed;
            }
            else // CalculatorVM (Standart Mod)
            {
                HistoryButtonVisibility = Visibility.Visible;
                GraphButtonVisibility = Visibility.Visible;
                ProgrammerButtonVisibility = Visibility.Visible;
            }

            // 2. Basit/Bilimsel ve Geri Dönüş Butonu Ayarı
            if (CurrentView == CalculatorVM)
            {
                // Hesap makinesindeyiz: Diğer moda geçiş butonunu göster
                if (CalculatorVM.ScientificPanelVisibility == Visibility.Visible)
                {
                    SimpleButtonVisibility = Visibility.Visible;      // Bilimsel açık -> Basit'e geç butonu
                    ScientificButtonVisibility = Visibility.Collapsed;
                }
                else
                {
                    ScientificButtonVisibility = Visibility.Visible;  // Basit açık -> Bilimsel'e geç butonu
                    SimpleButtonVisibility = Visibility.Collapsed;
                }
            }
            else
            {
                // Başka moddayız (Grafik veya Bilgisayar):
                // Burası "GERİ DÖNÜŞ" mantığıdır. 
                // Eğer hesap makinesini en son "Bilimsel" bıraktıysak "Bilimsel" butonu, 
                // "Basit" bıraktıysak "Basit" butonu görünür olsun.

                if (CalculatorVM.ScientificPanelVisibility == Visibility.Visible)
                {
                    ScientificButtonVisibility = Visibility.Visible; // Bilimsel moda geri dön
                    SimpleButtonVisibility = Visibility.Collapsed;
                }
                else
                {
                    SimpleButtonVisibility = Visibility.Visible;     // Basit moda geri dön
                    ScientificButtonVisibility = Visibility.Collapsed;
                }
            }
        }

        // --- KOMUTLAR ---
        [RelayCommand]
        private void ToggleHistory()
        {
            IsHistoryPanelOpen = !IsHistoryPanelOpen;
        }

        [RelayCommand] private void ShowCalculator() { CurrentView = CalculatorVM; UpdateModeButtons(); }
        [RelayCommand] private void ShowGraph() { CurrentView = GraphVM; UpdateModeButtons(); }
        [RelayCommand] private void ShowProgrammer() { CurrentView = ProgrammerVM; UpdateModeButtons(); }

        [RelayCommand] private void SwitchToSimpleFromMain() { ShowCalculator(); CalculatorVM.ScientificPanelVisibility = Visibility.Collapsed; UpdateModeButtons(); }
        [RelayCommand] private void SwitchToScientificFromMain() { ShowCalculator(); CalculatorVM.ScientificPanelVisibility = Visibility.Visible; UpdateModeButtons(); }
        [RelayCommand] private void ToggleHistoryFromMain() { ToggleHistory(); }

        private int _currentThemeIndex = 0;
        //Tema Listesi Dizisi
        private readonly string[] _themeList = { "DarkOrange", "DarkBlue", "DarkPink", "SoftOrange", "SoftBlue", "SoftPink" };
        
        //Tema Değiştirme
        [RelayCommand]
        private void ToggleNextTheme()
        {
            _currentThemeIndex++;
            if (_currentThemeIndex >= _themeList.Length) _currentThemeIndex = 0;
            string nextTheme = _themeList[_currentThemeIndex];
            try { string uriPath = $"pack://application:,,,/Themes/{nextTheme}.xaml"; Application.Current.Resources.MergedDictionaries[0] = new ResourceDictionary { Source = new Uri(uriPath) }; } catch { }
        }
    }
}
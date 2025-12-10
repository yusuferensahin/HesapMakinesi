using HesapMakinesi.ViewModels;
using System.Windows;

namespace HesapMakinesi
{
    public partial class MainWindow : Window
    {
        private bool _wasWide = false;
        // Yataydaki genişliği aşıp aşmadığımızı hafızada tutan değişken.


        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainViewModel();
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            var vm = (MainViewModel)this.DataContext;

            // DURUM 1: GENİŞ MOD (Pencere > 800)
            if (this.ActualWidth >= 800)
            {
                // Sağdaki Sidebar'ı Aç/Kapa yap
                if (SidebarHistoryPanel.Visibility == Visibility.Visible)
                {
                    SidebarHistoryPanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    SidebarHistoryPanel.Visibility = Visibility.Visible;
                }
            }
            // DURUM 2: DAR MOD (Pencere < 800)
            else
            {
                // Alttaki Overlay'i Aç/Kapa yap (ViewModel üzerinden)
                vm.IsHistoryPanelOpen = !vm.IsHistoryPanelOpen;
            }
        }
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var vm = (MainViewModel)this.DataContext;
            bool isWide = this.ActualWidth >= 800;
            // Yataydaki genişliğin belli boyutu aşıp aşmadığını kontrol eden değişken
            

            // Sadece durum değiştiğinde işlem yapılacak
            // Bu sayede kullanıcı küçültürken buton bozulmaz.
            if (isWide != _wasWide)
            {
                if (isWide)
                {
                    // DARDAN -> GENİŞE GEÇİŞ
                    // Overlay'i kapat, Sidebar'ı otomatik aç
                    vm.IsHistoryPanelOpen = false;
                    SidebarHistoryPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    // GENİŞTEN -> DARA GEÇİŞ
                    // Sidebar'ı kapat, Overlay'i kapalı tut (Kullanıcı isterse açsın)
                    SidebarHistoryPanel.Visibility = Visibility.Collapsed;
                    vm.IsHistoryPanelOpen = false;
                }

                _wasWide = isWide; 
                // Durumu kaydet
            }
        }
    }
}
using HesapMakinesi.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HesapMakinesi.Views
{
    /// <summary>
    /// Interaction logic for GraphView.xaml
    /// </summary>
    public partial class GraphView : UserControl
    {
        // Sürükleme durumu takibi için değişkenler
        private bool _isDragging = false;
        private Point _lastMousePosition;

        public GraphView()
        {
            InitializeComponent();
        }

        // Fare Tıklandığında
        private void GraphArea_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _isDragging = true;
                _lastMousePosition = e.GetPosition(GraphArea);
                GraphArea.CaptureMouse(); // Fare dışarı çıksa bile takip et
            }
        }

        // Fare Hareket Ettirildiğinde
        private void GraphArea_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                var currentPosition = e.GetPosition(GraphArea);

                // Ne kadar hareket ettik?
                double deltaX = currentPosition.X - _lastMousePosition.X;
                double deltaY = currentPosition.Y - _lastMousePosition.Y;

                // ViewModel'e bu hareketi gönder
                if (this.DataContext is GraphViewModel vm)
                {
                    vm.PanGraph(deltaX, deltaY);
                }

                _lastMousePosition = currentPosition;
            }
        }

        // Fare Bırakıldığında
        private void GraphArea_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                GraphArea.ReleaseMouseCapture();
            }
        }

        // Fare Alandan Çıktığında (Güvenlik önlemi)
        private void GraphArea_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                GraphArea.ReleaseMouseCapture();
            }
        }
    }
}
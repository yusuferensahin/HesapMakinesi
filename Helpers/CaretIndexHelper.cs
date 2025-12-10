using System;
using System.Windows;
using System.Windows.Controls;

namespace HesapMakinesi.Helpers
{
    // WPF TextBox kontrolünün 'CaretIndex' (imleç pozisyonu) özelliğini MVVM binding'e uygun hale getiren yardımcı sınıf.
    // Normalde CaretIndex bir DependencyProperty olmadığı için doğrudan bind edilemez, bu sınıf bunu sağlar.

    public static class CaretIndexHelper
    {
        // Değer değiştiğinde 'OnCaretIndexChanged' metodu tetiklenir.

        public static readonly DependencyProperty CaretIndexProperty =
            DependencyProperty.RegisterAttached(
                "CaretIndex",
                typeof(int),
                typeof(CaretIndexHelper),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnCaretIndexChanged));

        // XAML veya kod üzerinden bu özelliğin değerini okumak için kullanılan metot.
        public static int GetCaretIndex(DependencyObject obj) => (int)obj.GetValue(CaretIndexProperty);
        
        // XAML veya kod üzerinden bu özelliğe değer atamak için kullanılan metot.
        public static void SetCaretIndex(DependencyObject obj, int value) => obj.SetValue(CaretIndexProperty, value);
        
        // ViewModel'den veya koddan 'CaretIndex' özelliği değiştirildiğinde çalışır.
        private static void OnCaretIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Değişikliğin olduğu nesne bir TextBox mı diye kontrol et.
            if (d is TextBox textBox)
            {
                // Olay dinleyicisini (Event Handler) önce çıkarıp sonra ekleyerek çoklu eklemeyi (memory leak) önlüyoruz.
                textBox.SelectionChanged -= TextBox_SelectionChanged;
                textBox.SelectionChanged += TextBox_SelectionChanged;

                if (textBox.CaretIndex != (int)e.NewValue)
                {
                    // Math.Min kullanımı: Eğer yeni değer metin uzunluğundan büyükse hata vermemesi için
                    // imleci en sona (Text.Length) koyar.
                    textBox.CaretIndex = Math.Min((int)e.NewValue, textBox.Text.Length);
                }
            }
        }
        // Bu metot, güncel imleç konumunu ViewModel'e geri bildirir.
        private static void TextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                SetCaretIndex(textBox, textBox.CaretIndex);
            }
        }
    }
}
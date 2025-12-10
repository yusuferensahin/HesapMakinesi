using System;
using System.Windows;
using System.Windows.Controls;

namespace HesapMakinesi.Helpers
{
    public static class AutoScrollBehavior
    {
        // Önceki durumu hafızada tutmak için değişkenler
        private static int _lastCaretIndex = 0;
        private static int _lastTextLength = 0;

        public static readonly DependencyProperty AutoScrollProperty =
            DependencyProperty.RegisterAttached(
                "AutoScroll",
                typeof(bool),
                typeof(AutoScrollBehavior),
                new PropertyMetadata(false, OnAutoScrollChanged));

        public static bool GetAutoScroll(DependencyObject obj)
        {
            return (bool)obj.GetValue(AutoScrollProperty);
        }

        public static void SetAutoScroll(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoScrollProperty, value);
        }

        private static void OnAutoScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var textBox = d as TextBox;
            if (textBox == null) return;

            if ((bool)e.NewValue)
            {
                textBox.SelectionChanged += TextBox_SelectionChanged;
                textBox.TextChanged += TextBox_TextChanged;
            }
            else
            {
                textBox.SelectionChanged -= TextBox_SelectionChanged;
                textBox.TextChanged -= TextBox_TextChanged;
            }
        }

        // 1. Kullanıcı tıkladıkça veya gezinirken konumu kaydet
        private static void TextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            // Yazı değişimi sırasında burası tetiklenirse kaydetme, yoksa döngüye girer
            if (textBox.IsFocused || !string.IsNullOrEmpty(textBox.Text))
            {
                // Sadece güvenli zamanlarda kayıt al
                if (textBox.Text.Length == _lastTextLength)
                {
                    _lastCaretIndex = textBox.CaretIndex;
                }
            }
        }

        // 2. Yazı değiştiğinde (Klavye veya Buton)
        private static void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            int currentLength = textBox.Text.Length;
            int delta = currentLength - _lastTextLength; // Ne kadar harf eklendi?

            // Eğer yazı tamamen silindiyse (AC tuşu) başa dön
            if (currentLength == 0)
            {
                _lastCaretIndex = 0;
                _lastTextLength = 0;
                return;
            }

            textBox.Dispatcher.InvokeAsync(() =>
            {
                // SENARYO 1: Eğer işlemden önce en sondaydıysak -> Yine en sona git.
                if (_lastCaretIndex >= _lastTextLength)
                {
                    textBox.CaretIndex = currentLength;
                    textBox.ScrollToEnd();
                }
                // SENARYO 2: Araya ekleme yapıldıysa -> Eklenen kadar sağa git.
                else
                {
                    int newIndex = _lastCaretIndex + delta;

                    // Güvenlik kontrolü (Hata vermemesi için)
                    if (newIndex < 0) newIndex = 0;
                    if (newIndex > currentLength) newIndex = currentLength;

                    textBox.CaretIndex = newIndex;

                    // Eğer imleç ekranın dışında kaldıysa oraya odaklan
                    textBox.ScrollToLine(0); // Tek satır olduğu için line 0
                }

                // Son değerleri güncelle
                _lastTextLength = currentLength;
                _lastCaretIndex = textBox.CaretIndex;
            });
        }
    }
}
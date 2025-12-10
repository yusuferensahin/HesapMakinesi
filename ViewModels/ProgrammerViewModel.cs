using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows.Media;

namespace HesapMakinesi.ViewModels
{
    public partial class ProgrammerViewModel : ObservableObject
    {
        // GİRİŞ VE GÖRÜNÜM DEĞİŞKENLERİ
        [ObservableProperty] private string _inputExpression = "0";
        [ObservableProperty] private string _hexValue = "0";
        [ObservableProperty] private string _decValue = "0";
        [ObservableProperty] private string _octValue = "0";
        [ObservableProperty] private string _binValue = "0";

        // RENKLER
        private readonly SolidColorBrush _activeBrush = new SolidColorBrush(Color.FromRgb(255, 140, 0));
        private readonly SolidColorBrush _passiveBrush = new SolidColorBrush(Color.FromRgb(160, 160, 160));

        [ObservableProperty] private SolidColorBrush _hexColor;
        [ObservableProperty] private SolidColorBrush _decColor;
        [ObservableProperty] private SolidColorBrush _octColor;
        [ObservableProperty] private SolidColorBrush _binColor;

        // HESAPLAMA DEĞİŞKENLERİ
        private long _firstOperand = 0;
        private string _currentOperator = "";
        private bool _isNewNumber = true;
        private int _currentBase = 10;

        public ProgrammerViewModel()
        {
            UpdateActiveColor("DEC");
        }


        // --- 1. MOD DEĞİŞTİRME ---


        // [RelayCommand]: Bu metodu arayüzdeki HEX, DEC, OCT, BIN butonlarına bağlar.
        [RelayCommand]
        // Parametre (newBaseName): Hangi butona basıldıysa onun adı gelir (Örn: "BIN").
        private void SwitchBase(string newBaseName)
        {
            // 1.ADIM: Mevcut Durumu Koru
            // Geçiş yapmadan önce, ekranda yazan sayıyı (eski tabana göre) arka planda long tipine çevirip saklıyoruz.
            long currentValue = ParseCurrentInput();

            // 2. ADIM: Modu Güncelle
            // Uygulamanın global "Şu an hangi tabandayız?" değişkenini güncelliyoruz
            switch (newBaseName)
            {
                case "HEX": _currentBase = 16; break;
                case "DEC": _currentBase = 10; break;
                case "OCT": _currentBase = 8; break;
                case "BIN": _currentBase = 2; break;
            }

            // 3. ADIM: Ekrana Yeni Formatı Yansıt
            // Sakladığımız ham değeri (currentValue), seçilen yeni tabana göre String'e çevirip ekrana basıyoruz.
            if (newBaseName == "HEX") InputExpression = currentValue.ToString("X");
            else if (newBaseName == "DEC") InputExpression = currentValue.ToString();
            else if (newBaseName == "OCT") InputExpression = Convert.ToString(currentValue, 8);
            else if (newBaseName == "BIN") InputExpression = Convert.ToString(currentValue, 2);

            // 4. ADIM: Arayüzü yenile
            UpdateActiveColor(newBaseName); // Aktif olan butonun rengini değiştir
            UpdateConversions();            // Küçük önizleme etiketlerini (Label) güncelle
            // Kullanıcı bir sonraki tuşlamayı yaptığında, sayının yanına eklemek yerine 
            // yeni bir sayı girişi başlatması için oluşturduğumuz değişkeni kullanıyoruz.
            _isNewNumber = true;
        }

        private void UpdateActiveColor(string activeBase)
        {
            HexColor = activeBase == "HEX" ? _activeBrush : _passiveBrush;
            DecColor = activeBase == "DEC" ? _activeBrush : _passiveBrush;
            OctColor = activeBase == "OCT" ? _activeBrush : _passiveBrush;
            BinColor = activeBase == "BIN" ? _activeBrush : _passiveBrush;
        }

        // --- 2. TUŞLAMA ---
        [RelayCommand]
        private void NumberPress(string button)
        {
            if (_isNewNumber)
            {
                InputExpression = "";
                _isNewNumber = false;
            }

            // Geçerli giriş kontrolü
            if (!IsValidInput(button)) return;

            if (InputExpression == "0") InputExpression = "";
            InputExpression += button;
            UpdateConversions();
        }

        private bool IsValidInput(string button)
        {
            if (_currentBase == 16) return true;
            if ("ABCDEF".Contains(button)) return false;
            if (int.TryParse(button, out int digit))
            {
                if (digit >= _currentBase) return false;
            }
            return true;
        }

        // --- 3. İKİLİ OPERATÖRLER (+, -, AND, OR...) ---
        [RelayCommand]
        private void OperatorPress(string op)
        {
            if (!string.IsNullOrEmpty(_currentOperator) && !_isNewNumber)
            {
                Calculate();
            }

            _firstOperand = ParseCurrentInput();
            _currentOperator = op;
            _isNewNumber = true;
        }

        // --- 4. TEKİL OPERATÖR (NOT) ---
        [RelayCommand]
        private void UnaryOperator(string op)
        {
            long currentValue = ParseCurrentInput();
            long result = 0;

            if (op == "NOT")
            {
                result = ~currentValue; // Bitwise NOT işlemi
            }

            UpdateDisplayWithResult(result);
            _isNewNumber = true;
        }

        // --- 5. HESAPLAMA ---
        [RelayCommand]
        private void Calculate()
        {
            if (string.IsNullOrEmpty(_currentOperator)) return;

            long secondOperand = ParseCurrentInput();
            long result = 0;

            try
            {
                switch (_currentOperator)
                {
                    // Aritmetik
                    case "+": result = _firstOperand + secondOperand; break;
                    case "-": result = _firstOperand - secondOperand; break;
                    case "X": result = _firstOperand * secondOperand; break;
                    case "÷":
                        if (secondOperand != 0) result = _firstOperand / secondOperand;
                        else result = 0;
                        break;

                    // Mantıksal (Bitwise)
                    case "AND": result = _firstOperand & secondOperand; break;
                    case "OR": result = _firstOperand | secondOperand; break;
                    case "XOR": result = _firstOperand ^ secondOperand; break;
                    case "NAND": result = ~(_firstOperand & secondOperand); break;
                    case "NOR": result = ~(_firstOperand | secondOperand); break;

                    // Shift İşlemleri (İleride eklenebilir)
                    case "<<": result = _firstOperand << (int)secondOperand; break;
                    case ">>": result = _firstOperand >> (int)secondOperand; break;
                }

                UpdateDisplayWithResult(result);

                _currentOperator = "";
                _isNewNumber = true;
            }
            catch
            {
                InputExpression = "Hata";
            }
        }

        // Sonucu ekrana ve panellere yansıtan yardımcı metot
        private void UpdateDisplayWithResult(long result)
        {
            if (_currentBase == 16) InputExpression = result.ToString("X");
            else if (_currentBase == 10) InputExpression = result.ToString();
            else if (_currentBase == 8) InputExpression = Convert.ToString(result, 8);
            else if (_currentBase == 2) InputExpression = Convert.ToString(result, 2);

            UpdateConversions();
        }

        [RelayCommand]
        private void Delete()
        {
            if (_isNewNumber) return;
            if (InputExpression.Length > 0)
                InputExpression = InputExpression.Substring(0, InputExpression.Length - 1);
            if (string.IsNullOrEmpty(InputExpression)) InputExpression = "0";
            UpdateConversions();
        }

        [RelayCommand]
        private void ClearAll()
        {
            InputExpression = "0";
            _firstOperand = 0;
            _currentOperator = "";
            _isNewNumber = true;
            UpdateConversions();
        }

        private long ParseCurrentInput()
        {
            try { return Convert.ToInt64(InputExpression, _currentBase); }
            catch { return 0; }
        }

        private void UpdateConversions()
        {
            long number = ParseCurrentInput();
            HexValue = number.ToString("X");
            DecValue = number.ToString();
            OctValue = Convert.ToString(number, 8);
            BinValue = Convert.ToString(number, 2);
        }
    }
}
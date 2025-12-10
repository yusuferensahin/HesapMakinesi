using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Data;
using System;
using System.Windows;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Collections.ObjectModel;
using System.Linq;

namespace HesapMakinesi.ViewModels
{
    public partial class CalculatorViewModel : ObservableObject
    {
        [ObservableProperty] private string _displayText = "";
        [ObservableProperty] private string _historyText = "";
        [ObservableProperty] private int _caretIndex = 0;
        [ObservableProperty][NotifyPropertyChangedFor(nameof(ScientificRowHeight))] private Visibility _scientificPanelVisibility = Visibility.Collapsed;
        [ObservableProperty] private Visibility _historyPanelVisibility = Visibility.Collapsed;
        public ObservableCollection<string> HistoryLogs { get; } = new ObservableCollection<string>();
        public GridLength ScientificRowHeight => ScientificPanelVisibility == Visibility.Visible ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        [ObservableProperty] private bool _isRadian = false;
        public string AngleModeText => IsRadian ? "RAD" : "DEG";
        [ObservableProperty] private string _lastClickedButton = "";
        private bool _isResultShown = false;
        
        // Geçmişten işlem çağırmak için değişken
        private int _historyIndex = -1; // -1: Şu anki ekran, 0: En son işlem, 1: Ondan önceki...

        // 2nd tuşunun basılı olup olmadığını tutan değişken
        [ObservableProperty]
        private bool _isSecondFunction = false;

        // --- DİNAMİK BUTON YAZILARI ---
        public string SinText => IsSecondFunction ? "sin⁻¹" : "sin";
        public string CosText => IsSecondFunction ? "cos⁻¹" : "cos";
        public string TanText => IsSecondFunction ? "tan⁻¹" : "tan";


        // 2nd tuşuna basınca çalışacak komut
        // Toggle butonuna basılınca arayüzü güncelle
        [RelayCommand]
        private void ToggleSecondFunction()
        {

            IsSecondFunction = !IsSecondFunction;
            // Arayüze "Yazılar değişti, güncelle" haberi veriyoruz
            OnPropertyChanged(nameof(SinText));
            OnPropertyChanged(nameof(CosText));
            OnPropertyChanged(nameof(TanText));
        }

        [RelayCommand]
        private void ToggleHistory()
        {
            HistoryPanelVisibility = HistoryPanelVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        [RelayCommand] private void ClearHistoryLogs() { HistoryLogs.Clear(); }

        private void InsertText(string textToAdd)
        {
            if (_isResultShown) { HistoryText = DisplayText; DisplayText = ""; CaretIndex = 0; _isResultShown = false; }
            if (DisplayText == null) DisplayText = "";
            int safeIndex = Math.Clamp(CaretIndex, 0, DisplayText.Length);
            DisplayText = DisplayText.Insert(safeIndex, textToAdd);
            CaretIndex = safeIndex + textToAdd.Length;
        }

        [RelayCommand] private void NumberPress(string number) { InsertText(number); LastClickedButton = number; }
        [RelayCommand] private void OperatorPress(string op) { InsertText(op); LastClickedButton = op; }
        // --- AKILLI TEMİZLEME (Smart Clear) ---

        // Son tıklama zamanını tutmak için değişken
        private DateTime _lastClearClickTime = DateTime.MinValue;

        [RelayCommand]
        private void ProcessClear()
        {
            var now = DateTime.Now;

            // Eğer son tıklamadan bu yana 500ms'den az geçtiyse -> ÇİFT TIK (AC Modu)
            if ((now - _lastClearClickTime).TotalMilliseconds < 500)
            {
                PerformFullClear(); // Her şeyi sil (Geçmiş dahil)
            }
            else
            {
                // Değilse -> TEK TIK (C Modu)
                PerformEntryClear(); // Sadece ekrandaki sayıyı sil
            }

            _lastClearClickTime = now;
        }

        // SENARYO 1: Tek Tık (C - Clear Entry)
        // Sadece ekrandaki yazıyı siler, geçmişe ve hafızaya dokunmaz.
        private void PerformEntryClear()
        {
            DisplayText = "";
            CaretIndex = 0;
            LastClickedButton = "C";
        }

        // SENARYO 2: Çift Tık (AC - All Clear)
        // Ekranı, geçmiş loglarını ve işlem hafızasını tamamen sıfırlar.
        private void PerformFullClear()
        {
            DisplayText = "";
            HistoryText = "";
            CaretIndex = 0;
            _isResultShown = false;
            HistoryLogs.Clear(); // Listeyi de temizle (Senin isteğin üzerine)
            _historyIndex = -1;  // Geçmiş gezginini sıfırla
            LastClickedButton = "AC";
        }
        [RelayCommand] private void Delete() { if (_isResultShown) { ProcessClear(); return; } if (!string.IsNullOrEmpty(DisplayText) && CaretIndex > 0) { int removeIndex = CaretIndex - 1; DisplayText = DisplayText.Remove(removeIndex, 1); CaretIndex = removeIndex; } LastClickedButton = "DEL"; }
        [RelayCommand] private void ToggleScientific() { ScientificPanelVisibility = ScientificPanelVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible; }
        [RelayCommand] private void ToggleAngleMode() { IsRadian = !IsRadian; OnPropertyChanged(nameof(AngleModeText)); }

        [RelayCommand]
        private void ScientificOperator(string func)
        {
            LastClickedButton = func;
            string textToAdd = "";

            // --- 2nd TUŞU KONTROLÜ (Trigonometri) ---
            if (func == "sin") textToAdd = IsSecondFunction ? "sin⁻¹(" : "sin(";
            else if (func == "cos") textToAdd = IsSecondFunction ? "cos⁻¹(" : "cos(";
            else if (func == "tan") textToAdd = IsSecondFunction ? "tan⁻¹(" : "tan(";
            // ----------------------------------------

            else if (func == "π") textToAdd = "π";
            else if (func == "e") textToAdd = "e";
            else if (func == "^") textToAdd = "^";
            else if (func == "yroot") textToAdd = "√";
            else if (func == "x⁻¹") textToAdd = "^-1";
            else if (func == "n!") textToAdd = "!";
            else if (func == "logy") textToAdd = "lg"; 
            else if (func == "ln") textToAdd = "ln(";  
            else if (func == "Mod") textToAdd = "Mod";
            else if (func == "%") textToAdd = "%";

            else textToAdd = func + "("; // abs, log vb.

            InsertText(textToAdd);
        }

        [RelayCommand]
        private void Calculate()
        {
            try
            {
                string expression = DisplayText;
                if (string.IsNullOrWhiteSpace(expression)) return;


                int openCount = expression.Count(c => c == '(');
                int closeCount = expression.Count(c => c == ')');
                while (openCount > closeCount) { expression += ")"; closeCount++; }


                string rawExpression = expression;

                // Gizli çarpma (100sin -> 100*sin)
                expression = Regex.Replace(expression, @"(\d)(sin|cos|tan|log|ln|√|abs|π|e|\()", "$1*$2");
                expression = Regex.Replace(expression, @"(\))(\d|sin|cos|tan|log|ln|√|abs|π|e|\()", "$1*$2");

                expression = expression.Replace("π", Math.PI.ToString(CultureInfo.InvariantCulture)).Replace("e", Math.E.ToString(CultureInfo.InvariantCulture));
                
                expression = EvaluateScientificPart(expression);

                if (expression == "Syntax Error") // Regex hatası (örn: tek başına "!")
                {
                    DisplayText = "Syntax Error";
                    _isResultShown = true;
                    return;
                }
                if (expression.Contains("Math Error")) // Matematiksel hata (örn: tan(90))
                {
                    DisplayText = "Math Error";
                    _isResultShown = true;
                    return;
                }
                expression = expression.Replace("%", "*0.01");
                expression = expression.Replace("Mod", "%");

                string cleanExpression = expression.Replace("X", "*").Replace("x", "*");
                var result = new DataTable().Compute(cleanExpression, null);
                
                if (result != null)
                {
                    double dResult = Convert.ToDouble(result);
                    if (double.IsInfinity(dResult) || double.IsNaN(dResult))
                    {
                        //Bu kod sıfıra bölme yapıldığında (veya tanımsız bir sonuç çıktığında)
                        //ekrana sonsuzluk sembolü yerine doğrudan **"Math Error"** yazdıracak.
                        DisplayText = "Math Error";
                        _isResultShown = true;
                        return;
                    }
                    string resultString = dResult.ToString("G", CultureInfo.InvariantCulture);
                    DisplayText = resultString;
                    CaretIndex = DisplayText.Length;
                    _isResultShown = true;
                    HistoryLogs.Insert(0, $"{rawExpression} = {resultString}");
                }
                else DisplayText = "";
                LastClickedButton = "=";

            }
            catch
            {
                // DataTable.Compute patlarsa (örn: "+", "*5" gibi eksik ifadeler)
                DisplayText = "Syntax Error";
                _isResultShown = true;
            }
            _historyIndex = -1; // Geçmiş gezginini sıfırla
        }
        // --- NAVİGASYON KOMUTLARI ---

        [RelayCommand]
        private void MoveHistoryUp() // ÖNCEKİ İŞLEM (Yukarı Ok)
        {
            if (HistoryLogs.Count == 0) return;

            // Tarihte geriye git (İndeksi artır)
            if (_historyIndex < HistoryLogs.Count - 1)
            {
                _historyIndex++;
                LoadHistoryToDisplay();
            }
        }

        [RelayCommand]
        private void MoveHistoryDown() // SONRAKİ İŞLEM (Aşağı Ok)
        {
            if (_historyIndex > -1)
            {
                _historyIndex--;

                if (_historyIndex == -1)
                {
                    DisplayText = ""; // En yeniye gelince ekranı temizle (veya eski haline döndür)
                }
                else
                {
                    LoadHistoryToDisplay();
                }
            }
        }

        [RelayCommand]
        private void MoveCaretLeft() // İMLEÇ SOLA
        {
            if (CaretIndex > 0)
            {
                CaretIndex--;
            }
        }

        [RelayCommand]
        private void MoveCaretRight() // İMLEÇ SAĞA
        {
            if (DisplayText != null && CaretIndex < DisplayText.Length)
            {
                CaretIndex++;
            }
        }

        // Yardımcı Metot: Geçmişteki veriyi ekrana yazar
        private void LoadHistoryToDisplay()
        {
            if (_historyIndex >= 0 && _historyIndex < HistoryLogs.Count)
            {
                // Geçmiş formatı: "2 + 2 = 4"
                // Biz sadece işlemi ("2 + 2") almak istiyoruz.
                string log = HistoryLogs[_historyIndex];
                if (log.Contains("="))
                {
                    string expression = log.Split('=')[0].Trim();
                    DisplayText = expression;
                    CaretIndex = DisplayText.Length; // İmleci sona at
                }
            }
        }
        private string EvaluateScientificPart(string expression)
        {
            // 1. FAKTÖRİYEL (!) KORUMALI DÖNGÜ
            while (expression.Contains("!"))
            {
                string oldExpression = expression; // Değişiklik öncesi hali sakla

                expression = Regex.Replace(expression, @"(\d+[\.]?\d*)!", match =>
                {
                    double num = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                    double fact = 1;
                    for (int i = 1; i <= num; i++) fact *= i;
                    return fact.ToString(CultureInfo.InvariantCulture);
                });

                // Eğer Regex hiçbir şeyi değiştirmediyse ama hala "!" varsa, format yanlıştır.
                // Örn: "!5" veya sadece "!"
                if (expression == oldExpression) return "Syntax Error";
            }

            // 2. ÜS ALMA (^) KORUMALI DÖNGÜ
            while (expression.Contains("^"))
            {
                string oldExpression = expression;
                expression = Regex.Replace(expression, @"(\d+[\.]?\d*)\^(\-?\d+[\.]?\d*)", match =>
                {
                    double baseNum = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                    double powerNum = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                    return Math.Pow(baseNum, powerNum).ToString(CultureInfo.InvariantCulture);
                });

                if (expression == oldExpression) return "Syntax Error";
            }

            // 3. LOGARİTMA (lg)
            while (expression.Contains("lg"))
            {
                string oldExpression = expression;
                expression = Regex.Replace(expression, @"(\d+[\.]?\d*)lg(\-?\d+[\.]?\d*)", match =>
                {
                    double baseNum = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                    double argNum = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                    if (baseNum <= 0 || baseNum == 1 || argNum <= 0) return "0";
                    return Math.Log(argNum, baseNum).ToString(CultureInfo.InvariantCulture);
                });

                if (expression == oldExpression) return "Syntax Error";
            }

            // 4. KÖK ALMA (√) KORUMALI DÖNGÜ
            while (expression.Contains("√") && !expression.Contains("sin") && !expression.Contains("cos"))
            {
                string oldExpression = expression;
                expression = Regex.Replace(expression, @"(\d+[\.]?\d*)√(\-?\d+[\.]?\d*)", match =>
                {
                    double baseNum = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                    double rootNum = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                    if (rootNum == 0) return "0";
                    return Math.Pow(baseNum, 1.0 / rootNum).ToString(CultureInfo.InvariantCulture);
                });

                if (expression == oldExpression) return "Syntax Error";

                if (!Regex.IsMatch(expression, @"\d+[\.]?\d*√\d+[\.]?\d*")) break;
            }

            // 5. FONKSİYONLAR (sin, cos, vb.)
            bool foundFunction = true;
            while (foundFunction)
            {
                foundFunction = false;
                string oldExpression = expression; // Fonksiyonlarda da koruma olsun

                expression = Regex.Replace(expression, @"(sin⁻¹|cos⁻¹|tan⁻¹|sin|cos|tan|log|ln|abs)\(([^()]+)\)", match =>
                {
                    foundFunction = true;
                    string funcName = match.Groups[1].Value;
                    string innerExp = match.Groups[2].Value;
                    double innerResult = 0;

                    // İçerisi matematiksel olarak bozuksa (örn: "sin(..)") hata yakala
                    try { innerResult = Convert.ToDouble(new DataTable().Compute(innerExp, null)); }
                    catch { return "Syntax_Error_Signal"; } // Özel sinyal

                    double calcResult = 0;
                    double angleInput = IsRadian ? innerResult : (innerResult * Math.PI / 180);

                    switch (funcName)
                    {
                        //arcsin fonksiyonu
                        case "sin⁻¹":
                            if (innerResult < -1 || innerResult > 1) return "Math Error";
                            calcResult = Math.Asin(innerResult);
                            if (!IsRadian) calcResult = calcResult * 180 / Math.PI;
                            break;
                        //arccos fonksiyonu
                        case "cos⁻¹":
                            if (innerResult < -1 || innerResult > 1) return "Math Error";
                            calcResult = Math.Acos(innerResult);
                            if (!IsRadian) calcResult = calcResult * 180 / Math.PI;
                            break;
                        //arctan fonksiyonu
                        case "tan⁻¹":
                            calcResult = Math.Atan(innerResult);
                            if (!IsRadian) calcResult = calcResult * 180 / Math.PI;
                            break;
                        case "sin": calcResult = Math.Sin(angleInput); break;
                        case "cos": calcResult = Math.Cos(angleInput); break;
                        case "tan":
                            if (Math.Abs(Math.Cos(angleInput)) < 1e-15) return "Math Error";
                            calcResult = Math.Tan(angleInput);
                            break;
                        case "log": calcResult = Math.Log10(innerResult); break;
                        case "ln": calcResult = Math.Log(innerResult); break;
                        //mutlak değer fonksiyonu
                        case "abs": calcResult = Math.Abs(innerResult); break;
                    }

                    //sonsuz değerler vermemesi için bu kod bloğu kuruldu
                    if (Math.Abs(calcResult) < 1e-15) calcResult = 0;
                    return calcResult.ToString("0.################################", CultureInfo.InvariantCulture);
                });

                // Regex içinden özel hata sinyali geldiyse
                if (expression.Contains("Syntax_Error_Signal")) return "Syntax Error";
            }
            return expression;
        }
    }
}
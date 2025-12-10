using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;

namespace HesapMakinesi.ViewModels
{
    // Fonksiyon Çizgisi Modeli
    public partial class FunctionSeries : ObservableObject
    {
        [ObservableProperty] private string _expression = "";
        [ObservableProperty] private string _rawFormula = "";
        [ObservableProperty] private PointCollection _points = new PointCollection();
        [ObservableProperty] private SolidColorBrush _stroke = Brushes.Black;
    }

    // Izgara Çizgisi Modeli (Dikey/Yatay çizgiler için)
    public class GridLine
    {
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }
        public SolidColorBrush Stroke { get; set; } = Brushes.LightGray;
        public double Thickness { get; set; } = 1;
    }

    // Eksen Sayıları Modeli (10, 20, 30...)
    public class AxisLabel
    {
        public string Text { get; set; } = "";
        public double X { get; set; }
        public double Y { get; set; }
    }

    public partial class GraphViewModel : ObservableObject
    {
        [ObservableProperty] private string _functionInput = "x^2";

        // Varsayılan aralığı biraz genişlettik ki 10'ar 10'ar görün
        [ObservableProperty] private double _xMin = -50;
        [ObservableProperty] private double _xMax = 50;
        [ObservableProperty] private double _yMin = -50;
        [ObservableProperty] private double _yMax = 50;

        public ObservableCollection<FunctionSeries> DrawnFunctions { get; } = new ObservableCollection<FunctionSeries>();

        // YENİ: Izgara Çizgileri ve Sayılar
        public ObservableCollection<GridLine> GridLines { get; } = new ObservableCollection<GridLine>();
        public ObservableCollection<AxisLabel> AxisLabels { get; } = new ObservableCollection<AxisLabel>();

        private readonly SolidColorBrush[] _colors =
        {
            new SolidColorBrush(Color.FromRgb(220, 53, 69)),
            new SolidColorBrush(Color.FromRgb(0, 123, 255)),
            new SolidColorBrush(Color.FromRgb(40, 167, 69)),
            new SolidColorBrush(Color.FromRgb(255, 193, 7)),
            new SolidColorBrush(Color.FromRgb(111, 66, 193))
        };
        private int _colorIndex = 0;

        public double CanvasWidth { get; set; } = 1000;
        public double CanvasHeight { get; set; } = 1000;

        public GraphViewModel()
        {
            // Başlangıçta ızgarayı çiz
            CalculateGrid();
        }

        // --- KAYDIRMA (PAN) ---
        public void PanGraph(double pixelsX, double pixelsY)
        {
            double rangeX = XMax - XMin;
            double rangeY = YMax - YMin;

            double shiftX = -(pixelsX / CanvasWidth) * rangeX;
            double shiftY = (pixelsY / CanvasHeight) * rangeY;

            XMin += shiftX;
            XMax += shiftX;
            YMin += shiftY;
            YMax += shiftY;

            RefreshAll();
        }

        [RelayCommand]
        private void Draw()
        {
            if (string.IsNullOrWhiteSpace(FunctionInput)) return;

            var newSeries = new FunctionSeries
            {
                Expression = $"f(x) = {FunctionInput}",
                RawFormula = FunctionInput,
                Stroke = _colors[_colorIndex % _colors.Length],
                Points = new PointCollection()
            };

            DrawnFunctions.Add(newSeries);
            _colorIndex++;
            CalculatePoints(newSeries);
        }

        private void RefreshAll()
        {
            CalculateGrid(); // Önce zemini güncelle
            foreach (var series in DrawnFunctions)
            {
                CalculatePoints(series); // Sonra grafikleri üzerine oturt
            }
        }

        // --- IZGARA VE SAYI HESAPLAMA ---
        private void CalculateGrid()
        {
            GridLines.Clear();
            AxisLabels.Clear();

            double step = 10.0; // İsteğin üzerine 10'ar 10'ar adım

            // 1. DİKEY ÇİZGİLER (X Eksenindeki adımlar)
            // Başlangıç noktasını 10'un katına yuvarla ki çizgiler kayarken titremesin
            double startX = Math.Floor(XMin / step) * step;

            for (double x = startX; x <= XMax; x += step)
            {
                double screenX = MapX(x);

                // Ana Eksen mi yoksa ara çizgi mi?
                bool isAxis = Math.Abs(x) < 0.001;
                var color = isAxis ? Brushes.Black : Brushes.LightGray;
                double thickness = isAxis ? 2 : 1;

                // Çizgi Ekle
                GridLines.Add(new GridLine
                {
                    X1 = screenX,
                    Y1 = 0,
                    X2 = screenX,
                    Y2 = CanvasHeight,
                    Stroke = color,
                    Thickness = thickness
                });

                // Sayı Ekle (Eğer 0 değilse, 0'ı merkezde yazdırırız)
                if (!isAxis)
                {
                    // Sayıyı X ekseni çizgisinin hemen altına koyuyoruz (Y=0 çizgisi neredeyse)
                    double screenY0 = MapY(0);
                    // Eğer X ekseni ekran dışındaysa sayılar en altta veya üstte görünsün (Clamp)
                    if (screenY0 < 20) screenY0 = 20;
                    if (screenY0 > CanvasHeight - 20) screenY0 = CanvasHeight - 20;

                    AxisLabels.Add(new AxisLabel { Text = x.ToString(), X = screenX + 2, Y = screenY0 + 2 });
                }
            }

            // 2. YATAY ÇİZGİLER (Y Eksenindeki adımlar)
            double startY = Math.Floor(YMin / step) * step;

            for (double y = startY; y <= YMax; y += step)
            {
                double screenY = MapY(y);

                bool isAxis = Math.Abs(y) < 0.001;
                var color = isAxis ? Brushes.Black : Brushes.LightGray;
                double thickness = isAxis ? 2 : 1;

                GridLines.Add(new GridLine
                {
                    X1 = 0,
                    Y1 = screenY,
                    X2 = CanvasWidth,
                    Y2 = screenY,
                    Stroke = color,
                    Thickness = thickness
                });

                if (!isAxis)
                {
                    double screenX0 = MapX(0);
                    if (screenX0 < 20) screenX0 = 20;
                    if (screenX0 > CanvasWidth - 40) screenX0 = CanvasWidth - 40;

                    AxisLabels.Add(new AxisLabel { Text = y.ToString(), X = screenX0 + 2, Y = screenY - 20 });
                }
            }
        }


        private void CalculatePoints(FunctionSeries series)
        {
            var newPoints = new PointCollection();
            double step = (XMax - XMin) / 400.0;

            for (double x = XMin; x <= XMax; x += step)
            {
                double y = EvaluateFunction(series.RawFormula, x);
                if (double.IsNaN(y) || double.IsInfinity(y)) continue;

                double screenX = MapX(x);
                double screenY = MapY(y);

                if (screenY >= -CanvasHeight && screenY <= CanvasHeight * 2)
                {
                    newPoints.Add(new Point(screenX, screenY));
                }
            }
            series.Points = newPoints;
        }

        // --- YARDIMCI METOTLAR (Mapping) ---
        private double MapX(double mathX) => (mathX - XMin) / (XMax - XMin) * CanvasWidth;
        private double MapY(double mathY)
        {
            double normalizedY = (mathY - YMin) / (YMax - YMin);
            return CanvasHeight - (normalizedY * CanvasHeight);
        }

        [RelayCommand] private void RemoveFunction(FunctionSeries item) { if (DrawnFunctions.Contains(item)) DrawnFunctions.Remove(item); }
        [RelayCommand] private void ClearGraph() { DrawnFunctions.Clear(); _colorIndex = 0; }

        private double EvaluateFunction(string expression, double xValue)
        {
            try
            {
                string normExp = expression.ToLower().Replace(" ", "").Replace(",", ".");
                normExp = Regex.Replace(normExp, @"(\d)(x)", "$1*$2");
                normExp = Regex.Replace(normExp, @"(x)(\d)", "$1*$2");
                normExp = Regex.Replace(normExp, @"(\d)(\()", "$1*$2");
                normExp = Regex.Replace(normExp, @"(\))(\d)", "$1*$2");

                string xStr = "(" + xValue.ToString(CultureInfo.InvariantCulture) + ")";
                string eval = normExp.Replace("x", xStr);

                eval = EvaluateScientificMath(eval);
                if (eval == "Error") return double.NaN;

                var result = new DataTable().Compute(eval, null);
                return Convert.ToDouble(result, CultureInfo.InvariantCulture);
            }
            catch { return double.NaN; }
        }

        private string EvaluateScientificMath(string expression)
        {
            int safetyCounter = 0;
            while (expression.Contains("^"))
            {
                safetyCounter++;
                if (safetyCounter > 20) return "Error";
                expression = Regex.Replace(expression, @"(\(?[\d\.\-]+\)?)\^(\(?[\d\.\-]+\)?)", match =>
                {
                    try
                    {
                        string baseStr = match.Groups[1].Value.Replace("(", "").Replace(")", "");
                        string powStr = match.Groups[2].Value.Replace("(", "").Replace(")", "");
                        double baseVal = double.Parse(baseStr, CultureInfo.InvariantCulture);
                        double powVal = double.Parse(powStr, CultureInfo.InvariantCulture);
                        return Math.Pow(baseVal, powVal).ToString(CultureInfo.InvariantCulture);
                    }
                    catch { return "Error"; }
                });
            }
            safetyCounter = 0;
            while (Regex.IsMatch(expression, @"(sin|cos|tan)"))
            {
                safetyCounter++;
                if (safetyCounter > 20) break;
                expression = Regex.Replace(expression, @"(sin|cos|tan)\(([^()]+)\)", match =>
                {
                    string func = match.Groups[1].Value;
                    try
                    {
                        double val = Convert.ToDouble(new DataTable().Compute(match.Groups[2].Value, null), CultureInfo.InvariantCulture);
                        double res = 0;
                        if (func == "sin") res = Math.Sin(val);
                        else if (func == "cos") res = Math.Cos(val);
                        else if (func == "tan") res = Math.Tan(val);
                        return res.ToString(CultureInfo.InvariantCulture);
                    }
                    catch { return "Error"; }
                });
            }
            return expression;
        }
    }
}
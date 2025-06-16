using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace Cliente_TFG.Classes
{

    public class Theme : INotifyPropertyChanged
    {
        private Brush fondo;
        private Brush textoPrincipal;
        private Brush textoSecundario;
        private Brush textoPrecio;
        private Brush ratonEncima;
        private Brush fondoDescuento;
        private Brush fondoPanel;
        private Brush bordePanel;
        private Brush gridPrincipal;

        public Brush Fondo { get => fondo; set { fondo = value; OnPropertyChanged(); } }
        public Brush TextoPrincipal { get => textoPrincipal; set { textoPrincipal = value; OnPropertyChanged(); } }
        public Brush TextoPrecio { get => textoPrecio; set { textoPrecio = value; OnPropertyChanged(); } }
        public Brush TextoSecundario { get => textoSecundario; set { textoSecundario = value; OnPropertyChanged(); } }
        public Brush RatonEncima { get => ratonEncima; set { ratonEncima = value; OnPropertyChanged(); } }
        public Brush FondoDescuento { get => fondoDescuento; set { fondoDescuento = value; OnPropertyChanged(); } }
        public Brush FondoPanel { get => fondoPanel; set { fondoPanel = value; OnPropertyChanged(); } }
        public Brush BordePanel { get => bordePanel; set { bordePanel = value; OnPropertyChanged(); } }
        public Brush GridPrincipal { get => gridPrincipal; set { gridPrincipal = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public static class AppTheme
    {
        public static Theme Actual { get; private set; } = new Theme();

        

        private static Theme LoadDark() => new Theme
        {
            Fondo = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E")),
            TextoPrincipal = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")),
            TextoPrecio = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#baee12")),
            TextoSecundario = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CCCCCC")),
            RatonEncima = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#997878")),
            FondoDescuento = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4c6b22")),
            FondoPanel = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#533939"))
        };

        private static Theme LoadLight() => new Theme
        {
            Fondo = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")),
            TextoPrincipal = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#000000")),
            TextoPrecio = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#baee12")),
            TextoSecundario = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333")),
            RatonEncima = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D3BABA")),
            FondoDescuento = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4c6b22")),
            FondoPanel = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"))
        };




        static AppTheme()
        {
            SetDark(); // Tema inicial
        }

        public static void SetDark()
        {
            Actual.Fondo = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"));
            Actual.TextoPrincipal = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            //Actual.TextoPrecio = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#baee12"));
            Actual.TextoPrecio = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#d11f45"));
            Actual.TextoSecundario = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CCCCCC"));
            Actual.RatonEncima = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#997878"));
            //Actual.FondoDescuento = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4c6b22"));
            Actual.FondoDescuento = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#550617"));
            //Actual.FondoPanel = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#533939"));
            Actual.FondoPanel = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#252525"));
            Actual.BordePanel = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#d11f45"));
            Actual.GridPrincipal = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#121212"));
        }

        public static void SetLight()
        {
            Actual.Fondo = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F2F2F2"));
            Actual.TextoPrincipal = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"));
            Actual.TextoPrecio = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#689f38"));
            Actual.TextoSecundario = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666"));
            Actual.RatonEncima = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e0e0e0"));
            Actual.FondoDescuento = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DDEED8"));
            Actual.FondoPanel = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            Actual.BordePanel = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#d11f45"));
            Actual.GridPrincipal = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ededed"));

        }

        public static void Alternar()
        {
            // Asumimos que fondo blanco = tema claro
            if (((SolidColorBrush)Actual.Fondo).Color == Colors.White)
                SetDark();
            else
                SetLight();
        }
    }
}
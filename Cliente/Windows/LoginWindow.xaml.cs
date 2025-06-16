using Cliente_TFG.UserControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Cliente_TFG.Windows
{
    /// <summary>
    /// Lógica de interacción para VentanaLogin.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            MostrarLogin();
        }

        private void MostrarRegistro()
        {
            var sbOut = (Storyboard)FindResource("FadeOutStoryboard");
            sbOut.Completed += (s, e) =>
            {
                var registro = new RegistroControl();
                registro.VolverAlLogin += MostrarLogin;
                MainContent.Content = registro;

                var sbIn = (Storyboard)FindResource("FadeInStoryboard");
                sbIn.Begin();
            };
            sbOut.Begin();
        }

        private void MostrarLogin()
        {
            var sbOut = (Storyboard)FindResource("FadeOutStoryboard");
            sbOut.Completed += (s, e) =>
            {
                var login = new LoginControl();
                login.AbrirRegistro += MostrarRegistro;
                MainContent.Content = login;

                var sbIn = (Storyboard)FindResource("FadeInStoryboard");
                sbIn.Begin();
            };
            sbOut.Begin();
        }

        //METODO PARA PODER MOVER LA VENTANA CON EL RATON
        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Cerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

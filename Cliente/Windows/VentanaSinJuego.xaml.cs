using Cliente_TFG.Pages;
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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Cliente_TFG.Windows
{
    /// <summary>
    /// Lógica de interacción para VentanaSinJuego.xaml
    /// </summary>
    public partial class VentanaSinJuego : Window
    {

        private paginaTienda tienda;

        public VentanaSinJuego()
        {
            InitializeComponent();
        }

        private void Aceptar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Tienda_Click(object sender, RoutedEventArgs e)
        {
            var ventanaPrincipal = Application.Current.MainWindow as MainWindow;

            if (ventanaPrincipal != null)
            {
                // Navegar a la página tienda en el framePrincipal
                ventanaPrincipal.framePrincipal.Navigate(new paginaTienda(ventanaPrincipal));
            }

            this.Close();
        }
    }
}

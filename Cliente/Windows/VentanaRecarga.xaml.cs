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
using System.Windows.Shapes;

namespace Cliente_TFG.Windows
{
    /// <summary>
    /// Lógica de interacción para VentanaRecarga.xaml
    /// </summary>
    public partial class VentanaRecarga : Window
    {
        public VentanaRecarga(string monto, string metodo, string saldo)
        {
            InitializeComponent();

            txtMonto.Text = $"💰 Monto: {monto}";
            txtMetodo.Text = $"💳 Método: {metodo}";
            txtSaldo.Text = $"Saldo actual: {saldo}";
        }

        private void Aceptar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

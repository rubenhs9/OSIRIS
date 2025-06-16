using Cliente_TFG.Classes;
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
    /// Lógica de interacción para ErrorWindow.xaml
    /// </summary>
    public partial class ErrorWindow : Window
    {
        public ErrorWindow(string errorName)
        {
            InitializeComponent();
            this.errorName.Text = errorName;

            //CAMBIAR BOTON
            botonAceptar.Background = AppTheme.Actual.FondoPanel;
            botonAceptar.Foreground = AppTheme.Actual.BordePanel;
            botonAceptar.BorderBrush = AppTheme.Actual.BordePanel;
            botonAceptar.MouseEnter += BotonAceptar_MouseEnter;
            botonAceptar.MouseLeave += BotonAceptar_MouseLeave;
        }

        private void Aceptar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BotonAceptar_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Button btn)
            {
                btn.Background = AppTheme.Actual.BordePanel;
                btn.Foreground = AppTheme.Actual.FondoPanel;

                if (btn.Content is System.Windows.Shapes.Path path)
                {
                    path.Fill = AppTheme.Actual.FondoPanel;
                }
            }
        }

        private void BotonAceptar_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Button btn)
            {
                btn.Background = AppTheme.Actual.FondoPanel;
                btn.Foreground = AppTheme.Actual.BordePanel;

                if (btn.Content is System.Windows.Shapes.Path path)
                {
                    path.Fill = AppTheme.Actual.BordePanel;
                }
            }
        }
    }
}

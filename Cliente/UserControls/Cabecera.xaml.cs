using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
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
using Cliente_TFG.Classes;
using Cliente_TFG.Windows;
using Newtonsoft.Json;

namespace Cliente_TFG.UserControls
{
    /// <summary>
    /// Lógica de interacción para Cabezera.xaml
    /// </summary>
    public partial class Cabecera : UserControl, INotifyPropertyChanged
    {
        //PARA LAS OPCIONES
        public event RoutedEventHandler AtrasPresionado;
        public event RoutedEventHandler AvanzarPresionado;

        public event RoutedEventHandler BibliotecaPresionado;
        public event RoutedEventHandler TiendaPresionado;
        public event RoutedEventHandler AmigosPresionado;


        //PARA EL MENU
        public event PropertyChangedEventHandler PropertyChanged;
        private string _nombreUsuario = "NombreUsuario";
        public int IdUser { get; set; }
        public string NombreUsuario
        {
            get => _nombreUsuario;
            set
            {
                if (_nombreUsuario != value)
                {
                    _nombreUsuario = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NombreUsuario)));
                }
            }
        }


        private string _estadoTexto = "En línea";
        private Color _estadoColor = (Color)ColorConverter.ConvertFromString("#FF5EBD3E");

        private double _dinero = 0;
        public double Dinero
        {
            get => _dinero;
            set
            {
                if (_dinero != value)
                {
                    _dinero = value;
                    OnPropertyChanged(nameof(Dinero));
                }
            }
        }

        private ImageSource _fotoPerfil;
        public ImageSource FotoPerfil
        {
            get => _fotoPerfil;
            set
            {
                if (_fotoPerfil != value)
                {
                    _fotoPerfil = value;
                    OnPropertyChanged(nameof(FotoPerfil));
                }
            }
        }

        public event RoutedEventHandler VerPerfilPresionado;
        public event RoutedEventHandler RecargarSaldoPresionado;

        public Cabecera()
        {
            InitializeComponent();
            this.DataContext = this;

            //CARGAR BOTONES
            botonTienda.Background = AppTheme.Actual.FondoPanel;
            botonTienda.Foreground = AppTheme.Actual.BordePanel;
            botonTienda.BorderBrush = AppTheme.Actual.BordePanel;
            botonTienda.MouseEnter += BotonCabecera_MouseEnter;
            botonTienda.MouseLeave += BotonCabecera_MouseLeave;

            botonBiblioteca.Background = AppTheme.Actual.FondoPanel;
            botonBiblioteca.Foreground = AppTheme.Actual.BordePanel;
            botonBiblioteca.BorderBrush = AppTheme.Actual.BordePanel;
            botonBiblioteca.MouseEnter += BotonCabecera_MouseEnter;
            botonBiblioteca.MouseLeave += BotonCabecera_MouseLeave;

            botonAmigos.Background = AppTheme.Actual.FondoPanel;
            botonAmigos.Foreground = AppTheme.Actual.BordePanel;
            botonAmigos.BorderBrush = AppTheme.Actual.BordePanel;
            botonAmigos.MouseEnter += BotonCabecera_MouseEnter;
            botonAmigos.MouseLeave += BotonCabecera_MouseLeave;

            botonAtras.Background = AppTheme.Actual.FondoPanel;
            pathFlechaIzq.Fill = AppTheme.Actual.BordePanel;
            botonAtras.BorderBrush = AppTheme.Actual.BordePanel;
            botonAtras.MouseEnter += BotonCabecera_MouseEnter;
            botonAtras.MouseLeave += BotonCabecera_MouseLeave;

            botonAdelante.Background = AppTheme.Actual.FondoPanel;
            pathFlechaDer.Fill = AppTheme.Actual.BordePanel;
            botonAdelante.BorderBrush = AppTheme.Actual.BordePanel;
            botonAdelante.MouseEnter += BotonCabecera_MouseEnter;
            botonAdelante.MouseLeave += BotonCabecera_MouseLeave;


            EstadoActual = "Conectado";
        }

        //ATRAS CLICK
        private void BtnAtras_Click(object sender, RoutedEventArgs e)
        {
            //CAMBIAR A LA PÁGINA ANTERIOR
            AtrasPresionado?.Invoke(this, e);
        }

        //ADELANT CLICK
        private void BtnAdelante_Click(object sender, RoutedEventArgs e)
        {
            //CAMBIAR A LA PÁGINA SIGUIENTE
            AvanzarPresionado?.Invoke(this, e);
        }


        //BIBLIOTECA CLICK
        public void biblioteca_click(object sender, RoutedEventArgs e)
        {
            BibliotecaPresionado?.Invoke(this, e);
        }


        //TIENDA CLICK
        public void tienda_click(object sender, RoutedEventArgs e)
        {
            TiendaPresionado?.Invoke(this, e);
        }

        //AMIGOS CLICK
        public void amigos_click(object sender, RoutedEventArgs e)
        {
            AmigosPresionado?.Invoke(this, e);
        }

        private void BotonCabecera_MouseEnter(object sender, MouseEventArgs e)
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

        private void BotonCabecera_MouseLeave(object sender, MouseEventArgs e)
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


        //PARTE PARA EL USUARIO

        public string EstadoTexto
        {
            get => _estadoTexto;
            set { _estadoTexto = value; OnPropertyChanged(nameof(EstadoTexto)); }
        }

        public Color EstadoColor
        {
            get => _estadoColor;
            set { _estadoColor = value; OnPropertyChanged(nameof(EstadoColor)); }
        }


        private void GridPerfilUsuario_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed
                && !StatusMenu.IsOpen)
            {
                StatusMenu.PlacementTarget = gridPerfilUsuario;
                StatusMenu.IsOpen = true;
                e.Handled = true;
            }
        }

        //CLICK EN EL MENU DE LOS ESTADOS
        private async void StatusMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                string nuevoEstado = menuItem.Tag.ToString();
                
                EstadoActual = nuevoEstado;

                // Enviar el estado al servidor
                await ActualizarEstadoServidorAsync(nuevoEstado);
            }
        }

        //METODO PARA CAMBIAR EL ESTADO EN LA BASE DE DATOS
        private async Task ActualizarEstadoServidorAsync(string nuevoEstado)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var url = $"http://" + Config.IP + $":"+Config.Puerto + $"/user_profile/{IdUser}/estado";

                    var json = JsonConvert.SerializeObject(new { estado = nuevoEstado });
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(url, content);

                    if (!response.IsSuccessStatusCode)
                    {
                        string error = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Error al actualizar estado: {error}");
                    }
                    else
                    {
                        Console.WriteLine("Estado actualizado correctamente en el servidor");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Excepción al actualizar estado: {ex.Message}");
            }
        }


        //VER PERFIL CLICK
        public void VerPerfil_Click(object sender, RoutedEventArgs e)
        {
            VerPerfilPresionado?.Invoke(this, e);
        }

        //RECARGAR SALDO CLICK
        private void RecargarSaldo_Click(object sender, RoutedEventArgs e)
        {
            RecargarSaldoPresionado?.Invoke(this, e);
        }


        //PARA CAMBIAR EL ESTADO DE LA CUENTA
        private string _estadoActual;
        public string EstadoActual
        {
            get => _estadoActual;
            set
            {
                _estadoActual = value;
                switch (value)
                {
                    case "Conectado":
                        EstadoTexto = "En línea";
                        EstadoColor = (Color)ColorConverter.ConvertFromString("#FF5EBD3E");
                        break;
                    case "Ausente":
                        EstadoTexto = "Ausente";
                        EstadoColor = (Color)ColorConverter.ConvertFromString("#FFE6B905");
                        break;
                    case "Ocupado":
                        EstadoTexto = "Ocupado";
                        EstadoColor = (Color)ColorConverter.ConvertFromString("#FFBD3E3E");
                        break;
                    default:
                        EstadoTexto = "Invisible";
                        EstadoColor = (Color)ColorConverter.ConvertFromString("#656565");
                        break;
                }

                OnPropertyChanged(nameof(EstadoActual));
            }
        }
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));


        public void CambiarEstado(string nuevoEstado)
        {
            EstadoActual = nuevoEstado;
            _ = ActualizarEstadoServidorAsync(nuevoEstado);
        }

        public void CargarDatosUsuario(Usuario usuario)
        {
            NombreUsuario = usuario.NombreUsuario;
            Dinero = usuario.Dinero;

            try
            {
                var uri = new Uri(usuario.FotoPerfil, UriKind.Absolute);
                FotoPerfil = new BitmapImage(uri);
            }
            catch
            {
                //SI LA URL NO ES VÁLIDA O DA ERROR, USAR UNA IMAGEN POR DEFECTO
                FotoPerfil = new BitmapImage(new Uri("pack://application:,,,/res/imagenes/default.png"));
            }
        }

        //CERRAR SESION CLCIK
        public void CerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            //BORRAMOS EL TOKEN
            string rutaToken = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Osiris", "token.txt"
            );

            if (File.Exists(rutaToken))
            {
                try
                {
                    File.Delete(rutaToken);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al borrar token: " + ex.Message);
                }
            }

            //BORRAMOS LOS JUEGOS AGREGADOS
            string rutaJuegosGuardados = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Osiris", "juegos_agregados_biblioteca.json"
            );

            if (File.Exists(rutaJuegosGuardados))
            {
                try
                {
                    File.Delete(rutaJuegosGuardados);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al borrar token: " + ex.Message);
                }
            }

            //ABRIMOS LA VENTANA DEL LOGIN
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();

            //CERRAMOS LA VENTANA ACTUAL DONDE SE HA HECHO CLICK EN EL BOTON PARA ASI NO TENER ERRORES
            if (sender is DependencyObject dependencyObject)
            {
                Window window = Window.GetWindow(dependencyObject);
                if (window != null)
                {
                    window.Close();
                }
            }
        }


    }

}


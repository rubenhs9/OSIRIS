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
using Cliente_TFG.Classes;
using Cliente_TFG.Pages;
using Cliente_TFG.UserControls;

namespace Cliente_TFG
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public Usuario user;
        private bool online = true;
        public string ip = Config.IP;
        private int idUser = 3;

        public Usuario Usuario
        {
            get => user;
            set => user = value;
        }

        public MainWindow()
        {
            InitializeComponent();
            cargarTODO();

        }

        public MainWindow(int idUser)
        {
            InitializeComponent();
            this.idUser = idUser;
            cargarTODO();
        }


        private void cargarTODO()
        {
            this.DataContext = AppTheme.Actual;
            AppTheme.SetDark();
            cargarTema();

            framePrincipal.Navigated += FramePrincipal_Navigated;

            Cabecera_top.IdUser = this.idUser;
            Cabecera_top.AtrasPresionado += boton_atras_presionado;
            Cabecera_top.AvanzarPresionado += boton_avanzar_presionado;
            Cabecera_top.BibliotecaPresionado += boton_biblioteca_presionado;
            Cabecera_top.TiendaPresionado += boton_tienda_presionado;
            Cabecera_top.AmigosPresionado += boton_amigos_presionado;

            Cabecera_top.VerPerfilPresionado += boton_verPerfil_presionado;
            Cabecera_top.RecargarSaldoPresionado += boton_recargarSaldo_presionado;

            if (online)
            {
                try
                {
                    user = new Usuario(this);
                    user.CargarDatos(idUser); //SOLO CARGAMOS LOS DATOS SI ESTA EN MODO ONLINE
                    //MessageBox.Show(user.estado);
                    Cabecera_top.CambiarEstado(user.estado);
                    //GUARDAMOS TODO EL LOCAL DESPUES DE LA CARGA CORRECTA
                    GuardarDatosLocal();

                }
                catch
                {
                    //SI FALLA LA CARGA, PASAMOS A MODO OFFLINE AUTOMATICAMENTE Y CARGAMOS LOS DATOS GUARDADOS
                    online = false;
                    user = LocalStorage.CargarUsuario() ?? new Usuario(this);
                    user.bibliotecaJuegos = LocalStorage.CargarBiblioteca();

                    MessageBox.Show("No se pudo conectar al servidor. Modo offline activado.");
                }
            }
            else
            {
                user = LocalStorage.CargarUsuario() ?? new Usuario(this);
                user.bibliotecaJuegos = LocalStorage.CargarBiblioteca();

                MessageBox.Show("Modo offline activado.");
            }

            //CARGAMOS LA CABECERA Y LA PRIMERA PAGINA
            Cabecera_top.NombreUsuario = user.NombreUsuario;
            Cabecera_top.Dinero = user.Dinero;
            Cabecera_top.CargarDatosUsuario(user);

            CargarPrimeraVentana();

        }


        public void GuardarDatosLocal()
        {
            LocalStorage.GuardarUsuario(user);
            LocalStorage.GuardarBiblioteca(user.BibliotecaJuegos);
            LocalStorage.GuardarBibliotecaNombreJuegos(user.bibliotecaJuegosNombres);
        }


        private void cargarTema()
        {
            mainGrid.Background = AppTheme.Actual.GridPrincipal;
        }

        public void CargarDatosUsuario(int id)
        {
            user.CargarDatos(id);
            
        }

        private void CargarPrimeraVentana()
        {
            //var paginaTienda = new paginaTienda(this);
            //framePrincipal.Navigate(paginaTienda);

            //var paginaTienda = new paginaJuegoTienda(this, 3017860);
            //framePrincipal.Navigate(paginaTienda);

            var paginaBiblioteca = new paginaBiblioteca(this);
            framePrincipal.Navigate(paginaBiblioteca);
        }

        private void FramePrincipal_Navigated(object sender, NavigationEventArgs e)
        {
            if (e.Content is paginaTienda pagina)
            {
                pagina.RestaurarOpacidad();
            }
        }

        private void boton_atras_presionado(object sender, RoutedEventArgs e)
        {
            if (framePrincipal.CanGoBack) 
                framePrincipal.GoBack();
        }

        private void boton_avanzar_presionado(object sender, RoutedEventArgs e)
        {
            if (framePrincipal.CanGoForward)
                framePrincipal.GoForward();
        }

        private void boton_biblioteca_presionado(object sender, RoutedEventArgs e)
        {
            framePrincipal.Navigate(new paginaBiblioteca(this));
        }

        private void boton_tienda_presionado(object sender, RoutedEventArgs e)
        {
            framePrincipal.Navigate(new paginaTienda(this));
        }

        private void boton_amigos_presionado(object sender, RoutedEventArgs e)
        {
            framePrincipal.Navigate(new paginaAmigos(this));
        }

        private void boton_verPerfil_presionado(object sender, RoutedEventArgs e)
        {
            framePrincipal.Navigate(new paginaPerfil(this));
        }

        private void boton_recargarSaldo_presionado(object sender, RoutedEventArgs e)
        {
            framePrincipal.Navigate(new paginaRecargaSaldo(this));
        }


        public string IP => ip;
        public bool Online => online;


    }
}

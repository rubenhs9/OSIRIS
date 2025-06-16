using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Cliente_TFG.Classes;
using Cliente_TFG.Windows;
using Newtonsoft.Json;
using static System.Net.WebRequestMethods;
using System.IO.Compression;
namespace Cliente_TFG.Pages
{
    /// <summary>
    /// Lógica de interacción para paginaBiblioteca.xaml
    /// </summary>
    public partial class paginaBiblioteca : Page
    {
        private MainWindow ventanaPrincipal;

        //DATOS DE PRUEBA
        private string[] appids;
        private string[] Nombres;

        //PARA EL FONDO
        private List<BitmapImage> imagenesFondo = new List<BitmapImage>();
        private List<BitmapImage> imagenesLogos = new List<BitmapImage>();
        private List<BitmapImage> imagenVerticalJuegos = new List<BitmapImage>();

        //PARA LOS DEMAS JUEGOS DE LA BIBLIOTECA
        private List<int> listaAppids;
        private List<string> nombresJuegos;
        private List<JuegoInfo> juegosGuardados = new List<JuegoInfo>();
        private List<JuegoInfo> juegosAgregadosGuardados = new List<JuegoInfo>();
        private JuegoInfo juegoSteamSeleccionado;
        private BibliotecaResponse bibliotecaTotal = new BibliotecaResponse
        {
            juegos = new List<Juego>()
        };

        //PARA LAS DESCARGAS
        private Dictionary<int, bool> cacheDescargables = new Dictionary<int, bool>();

        //PARA IDENTIFICAR EL JUEGO QUE SE ESTA VIENDO
        private int appidActual;

        public paginaBiblioteca(MainWindow ventanaPrincipal)
        {
            InitializeComponent();
            this.ventanaPrincipal = ventanaPrincipal;

            this.Loaded += async (s, e) =>
            {
                borrarDatos();

                CargarDatosJson();

                //OBTENEMOS LA BIBLIOTECA PRIMERO
                if (ventanaPrincipal.Online)
                    await ObtenerBibliotecaDesdeApiAsync();

                await CargarImagenesFondo();

                AplicarFadeIn(gridPrincipal);
            };
        }


        //METODO PARA BORRAR LOS DATOS CUANDO VUELVAS ATRAS (SI NO SE HACE, SE DUPLICAN LOS JUEGOS)
        private void borrarDatos()
        {
            appids = Array.Empty<string>();
            Nombres = Array.Empty<string>();
            imagenesFondo.Clear();
            imagenesLogos.Clear();
            imagenVerticalJuegos.Clear();
            panelJuegosBiblioteca.Children.Clear();
        }

        private void CargarDatosJson()
        {
            try
            {
                //Cargar la biblioteca local (lista de appids)
                listaAppids = LocalStorage.CargarBiblioteca();
                nombresJuegos = LocalStorage.CargarBibliotecaNombreJuegos();

                if (listaAppids == null || listaAppids.Count == 0)
                {
                    Console.WriteLine("No hay juegos en la biblioteca local.");
                    appids = new string[0];
                    Nombres = new string[0]; 
                    return;
                }

                //Convertir a string[]
                appids = listaAppids.Select(id => id.ToString()).ToArray();

                Nombres = appids.Select(id => $"Juego {id}").ToArray();

                Console.WriteLine($"Se cargaron {appids.Length} appids desde biblioteca local.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR AL CARGAR LA BIBLIOTECA LOCAL: " + ex.Message);
                appids = new string[0];
                Nombres = new string[0];
            }
        }



        public async Task<BibliotecaResponse> ObtenerBibliotecaDesdeApiAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    string url = "http://" + Config.IP + $":"+Config.Puerto+"/library/" + ventanaPrincipal.Usuario.IdUsuario;
                    //MessageBox.Show($"Llamando a API: {url}");

                    string json = await client.GetStringAsync(url);
                    //MessageBox.Show($"Respuesta API: {json.Substring(0, Math.Min(200, json.Length))}");

                    BibliotecaResponse response = JsonConvert.DeserializeObject<BibliotecaResponse>(json);

                    if (response?.juegos != null)
                    {
                        bibliotecaTotal.juegos = response.juegos;
                    }
                    else
                    {
                        bibliotecaTotal.juegos = new List<Juego>();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error obteniendo biblioteca: " + ex.Message);
                    bibliotecaTotal.juegos = new List<Juego>();
                }
            }

            return bibliotecaTotal;
        }







        public class Juego
        {
            public int app_id { get; set; }
            public string captura { get; set; }
            public string nombre { get; set; }
            public string header { get; set; }
        }

        public class BibliotecaResponse
        {
            public List<Juego> juegos { get; set; }
        }

        private void CargarDatosDePrueba()
        {

            appids = new string[]
            {
                "730",
                "570",
                "578080",
                "252490",
                "2507950",
                "1903340",
                "1086940",
                "3017860",
                "2456740",
                "805550",
                "2878980",
                "3159330",
                "2909400",
                "2531310",
                "3058630",
                "2669320",
                "1687950",
                "244210",
                "1551360",
                "3164500",
                "2842040",
                "2488620",
            };

            Nombres = new string[]
            {
                "Counter Strike 2",
                "Dota 2",
                "PUBG: BATTLEGROUNDS",
                "Rust",
                "Delta Force",
                "Clair Obscur: Expedition 33",
                "Baldur's Gate 3",
                "DOOM: The Dark Ages",
                "inZOI",
                "Assetto Corsa Competizione",
                "NBA 2K25",
                "Assassin's Creed Shadows",
                "FINAL FANTASY VII REBIRTH",
                "The Last of Us™ Part II Remastered",
                "Assetto Corsa EVO",
                "EA SPORTS FC 25",
                "Persona 5 Royal",
                "Assetto Corsa",
                "Forza Horizon 5",
                "Schedule I",
                "Star Wars Outlaws",
                "F1® 24",
            };

        }


        //METODO PARA CARGAR LAS IMAGENES DE FONDO DE LA INTERFAZ
        private async Task CargarImagenesFondo()
        {

            imagenesFondo.Clear();
            imagenesLogos.Clear();
            imagenVerticalJuegos.Clear();

            foreach (var appidJuego in appids)
            {
                string nombreFondo = $"{appidJuego}_fondo.jpg";
                string urlFondo = $"https://shared.cloudflare.steamstatic.com/store_item_assets/steam/apps/{appidJuego}/library_hero.jpg";
                var imagenFondo = await ObtenerImagenAsync(urlFondo, nombreFondo);
                if (imagenFondo == null)
                {
                    //IMAGEN INVÁLIDA PARA FORZAR IMAGEFAILED
                    imagenFondo = GenerarImagenFondoFallback(appidJuego, nombreFondo);
                }
                if (imagenFondo != null)
                {

                    imagenesFondo.Add(imagenFondo);
                }

                string nombreLogo = $"{appidJuego}_logo.png";
                string urlLogo = $"https://shared.cloudflare.steamstatic.com/store_item_assets/steam/apps/{appidJuego}/logo.png";
                var imagenLogo = await ObtenerImagenAsync(urlLogo, nombreLogo);
                if (imagenLogo != null)
                {
                    imagenesLogos.Add(imagenLogo);
                }
                else
                {
                    //IMAGEN INVÁLIDA PARA FORZAR IMAGEFAILED
                    imagenesLogos.Add(null);
                }

                string nombreVertical = $"{appidJuego}_vertical.jpg";
                string urlVertical = $"https://shared.cloudflare.steamstatic.com/store_item_assets/steam/apps/{appidJuego}/library_600x900.jpg";
                var imagenVertical = await ObtenerImagenAsync(urlVertical, nombreVertical);

                if (imagenVertical == null)
                {
                    imagenVertical = GenerarImagenFallback(appidJuego, nombreVertical);
                }

                if (imagenVertical != null)
                {
                    
                    imagenVerticalJuegos.Add(imagenVertical);
                }
            }

            if (imagenesFondo.Count == 0)
            {
                //CARGAMOS LA IMAGEN POR DEFECTO POR SI NO HAY IMAGENES
                var fallbackPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "res", "library", "Banner_no_games_library.png");
                var fallbackImage = new BitmapImage(new Uri(fallbackPath));
                imagenesFondo.Add(fallbackImage);
                BotonJugar.Opacity = 0;
            }

            //PONEMOS EL APPID ACTUAL EN EL PRIMER JUEGO PARA PODER INDENTIFICARLO
            if (listaAppids.Count != 0)
                appidActual = listaAppids.First();

            CargarFondo();
            CargarJuegosBibioteca();
            CargarJuegosEnInterfaz();

            try
            {
                //PARTE PARA COMPROBAR SI EL PRIMER JUEGO TIENE RUTA PUESTA
                if (listaAppids != null || listaAppids.Count > 0)
                {
                    string appid = listaAppids.First().ToString();
                    if (TieneRutaAsignada(appid))
                    {
                        var juegoSeleccionado = juegosGuardados.FirstOrDefault(j => j.AppId == appid);
                        if (juegoSeleccionado != null)
                        {
                            ActualizarEstadoBotonJuego(BotonJugar, juegoSeleccionado);
                        }
                    }
                }
            }
            catch { }
        }

        

        private async Task<BitmapImage> ObtenerImagenAsync(string url, string nombreArchivo)
        {
            //PRIMERO INTENTA CARGAR LOCAL
            if (LocalStorage.ExisteImagen(nombreArchivo))
            {
                return LocalStorage.CargarImagenLocal(nombreArchivo);
            }

            // I NO EXISTE LOCAL, INTENTA DESCARGAR Y GUARDAR
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var bytes = await client.GetByteArrayAsync(url);
                    LocalStorage.GuardarImagen(nombreArchivo, bytes);
                    return LocalStorage.CargarImagenLocal(nombreArchivo);
                }
            }
            catch
            {
                //SI FALLA DESCARGAR, DEVUELVE NULL
                return null;
            }
        }

        private BitmapImage GenerarImagenFondoFallback(string appid, string nombreVertical)
        {
            // INTENTA CARGAR LA IMAGEN LOCAL
            if (LocalStorage.ExisteImagen(nombreVertical))
            {
                return LocalStorage.CargarImagenLocal(nombreVertical);
            }

            int id = int.Parse(appid);
            var juego = bibliotecaTotal?.juegos?.FirstOrDefault(j => j.app_id == id);

            if (juego == null)
                return null;

            string fallbackUrl = juego.header ?? juego.captura;

            if (string.IsNullOrEmpty(fallbackUrl))
                return null;

            try
            {
                using (WebClient client = new WebClient())
                {
                    byte[] datosImagen = client.DownloadData(fallbackUrl);

                    // GUARDAR LA IMAGEN EN DISCO
                    string nombreArchivo = $"{appid}_fondo.jpg";
                    LocalStorage.GuardarImagen(nombreArchivo, datosImagen);

                    // CREAR BitmapImage DESDE LOS DATOS EN MEMORIA
                    BitmapImage bmp = new BitmapImage();
                    using (MemoryStream ms = new MemoryStream(datosImagen))
                    {
                        bmp.BeginInit();
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.StreamSource = ms;
                        bmp.EndInit();
                        bmp.Freeze(); // NECESARIO SI USARÁS ESTA IMAGEN EN OTRO HILO
                    }

                    return bmp;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }




        //METODO PARA CARGAR EL FONDO
        private void CargarFondo()
        {
            imgFondo.Source = imagenesFondo.First();
            imgFondo.ImageFailed += (s, e) =>
            {
                Uri fallbackUri = new Uri($"https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/{appids[0]}/header.jpg");
                imgFondo.Source = new BitmapImage(fallbackUri);
            };

            if (imagenesLogos.Count != 0)
            {
                if(imagenesLogos.First() == null)
                {
                    txtFalloLogo.Background = (Brush)(new BrushConverter().ConvertFrom("#80000000"));
                    txtFalloLogo.Text = nombresJuegos[0];
                }
                else
                {
                    ImagenLogo.Source = imagenesLogos.First();
                }
                
            }
        }


        //METODO PARA CARGAR TODOS LOS DEMAS JUEGOS DE LA BIBLIOTECA
        private void CargarJuegosBibioteca()
        {
            var juegosExistentes = CargarJuegosDesdeJson();
            juegosGuardados.Clear();

            for (int i = 0; i < listaAppids.Count; i++)
            {
                int currentIndex = i;
                var juegosBiblioteca = imagenVerticalJuegos[currentIndex];

                string appid = listaAppids[currentIndex].ToString();
                string nombre = nombresJuegos[currentIndex];

                //BUSCAMOS SI YA EXISTE EN EL JSON
                string rutaEjecutable = juegosExistentes
                    .FirstOrDefault(j => j.AppId == appid)?.RutaEjecutable ?? "";

                //AÑADIR A LA LISTA
                juegosGuardados.Add(new JuegoInfo
                {
                    AppId = appid,
                    Nombre = nombre,
                    RutaEjecutable = rutaEjecutable
                });


                var scale = new ScaleTransform(1.0, 1.0);
                double altura = 170;
                double proporcion = 600.0 / 900.0;
                double ancho = altura * proporcion;

                Image img = new Image
                {
                    Source = juegosBiblioteca,
                    Stretch = Stretch.Uniform,
                    Height = altura,
                    Margin = new Thickness(11),
                    Tag = i,
                    RenderTransform = scale,
                    RenderTransformOrigin = new Point(0.5, 0.5)
                };


                //ANIMACIONES SI CARGA BIEN
                img.MouseLeftButtonUp += ImagenJuego_Click;

                //ASIGNAR MENU
                ContextMenu menu = CrearContextMenuJuego(currentIndex);
                img.ContextMenu = menu;
                img.MouseRightButtonUp += (s, e) =>
                {
                    img.ContextMenu.IsOpen = true;
                    img.ContextMenu.PlacementTarget = img;
                };

                img.MouseEnter += (s, e) =>
                {
                    var anim = new DoubleAnimation(1.0, 1.1, TimeSpan.FromMilliseconds(150));
                    scale.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
                    scale.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
                };

                img.MouseLeave += (s, e) =>
                {
                    var anim = new DoubleAnimation(1.1, 1.0, TimeSpan.FromMilliseconds(150));
                    scale.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
                    scale.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
                };

                panelJuegosBiblioteca.Children.Add(img);
                GuardarJuegosEnJson();
            }

        }



        public class JuegoInfo
        {
            public string AppId { get; set; }
            public string Nombre { get; set; }
            public string RutaEjecutable { get; set; }
            public bool EsManual { get; set; }
        }

        private string rutaJsonJuegos = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Osiris", "juegos_biblioteca.json");


        //METODO PARA GUARDAR LOS JUEGOS EN EL JSON
        private void GuardarJuegosEnJson()
        {
            string json = JsonConvert.SerializeObject(juegosGuardados, Formatting.Indented);
            System.IO.File.WriteAllText(rutaJsonJuegos, json);
        }

        //METODO PARA CARGAR LOS JUEGOS DEL JSON (LO USAMOS PARA NO BORRAR LOS DATOS DE LOS JUEGOS ANTERIORES POR SI CAMBIAMOS DE LOGIN)
        private List<JuegoInfo> CargarJuegosDesdeJson()
        {
            if (System.IO.File.Exists(rutaJsonJuegos))
            {
                string json = System.IO.File.ReadAllText(rutaJsonJuegos);
                return JsonConvert.DeserializeObject<List<JuegoInfo>>(json) ?? new List<JuegoInfo>();
            }
            return new List<JuegoInfo>();
        }


        private ContextMenu CrearContextMenuJuego(int index)
        {
            ContextMenu menu = new ContextMenu
            {
                Style = (Style)FindResource("EstiloContextMenu")
            };

            MenuItem jugarItem = new MenuItem
            {
                Header = "Jugar",
                Style = (Style)FindResource("EstiloMenuItem")
            };

            MenuItem cambiarEjecutableItem = new MenuItem
            {
                Header = "Cambiar ejecutable",
                Style = (Style)FindResource("EstiloMenuItem")
            };

            MenuItem detallesItem = new MenuItem
            {
                Header = "Ver detalles",
                Style = (Style)FindResource("EstiloMenuItem")
            };
            MenuItem desinstalarItem = new MenuItem
            {
                Header = "Desinstalar",
                Style = (Style)FindResource("EstiloMenuItem")
            };

            //ASIGNAR OPCIONES
            jugarItem.Click += (s, e) => IniciarJuego(index);
            cambiarEjecutableItem.Click += (s, e) => cambiarEjecutableJuego(index);
            detallesItem.Click += (s, e) => MostrarDetallesJuego(index);
            desinstalarItem.Click += (s, e) => DesinstalarJuego(index);

            menu.Items.Add(jugarItem);
            menu.Items.Add(cambiarEjecutableItem);
            menu.Items.Add(detallesItem);
            menu.Items.Add(desinstalarItem);

            return menu;
        }

        private void IniciarJuego(int index)
        {
            string ruta = juegosGuardados[index].RutaEjecutable;
            //MessageBox.Show(ruta);
            if (!string.IsNullOrEmpty(ruta) && System.IO.File.Exists(ruta))
            {
                Process.Start(ruta);
            }
            else
            {
                var ventanaError = new ErrorWindow("Ruta del ejecutable no válida. Cámbiala primero.");
                //ventanaError.Owner = Application.Current.MainWindow; //opcional para centrar
                ventanaError.ShowDialog();
                //MessageBox.Show("Ruta del ejecutable no válida. Cámbiala primero.");
            }
        }

        //METODO PARA CAMBIAR LA RUTA DEL EJECUTABLE EL CUAL QUEREMOS INICIAR
        private void cambiarEjecutableJuego(int index)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Archivos ejecutables (*.exe)|*.exe";

            if (openFileDialog.ShowDialog() == true)
            {
                juegosGuardados[index].RutaEjecutable = openFileDialog.FileName;
                GuardarJuegosEnJson();
                //MessageBox.Show("Ruta actualizada con éxito.");
                var juegoSeleccionado = juegosGuardados[index];
                ActualizarEstadoBotonJuego(BotonJugar, juegoSeleccionado);

            }

        }

        //METODO PARA COMPROBAR SI UN APPID TIENE RUTA ASIGANADA (SIRVE NADA MAS PARA EL PRIMER JUEGO QUE SE MUESTRA)
        private bool TieneRutaAsignada(string appid)
        {
            var juego = juegosGuardados.FirstOrDefault(j => j.AppId == appid);
            return juego != null && !string.IsNullOrWhiteSpace(juego.RutaEjecutable);
        }

        //METODO PARA MOSTRAR LOS DETALLES DEL JUEGO EN LA TIENDA
        private async void MostrarDetallesJuego(int index)
        {
            await FadeOutATodo();
            paginaJuegoTienda newPage = new paginaJuegoTienda(ventanaPrincipal, listaAppids[index]);
            ventanaPrincipal.framePrincipal.Navigate(newPage);
            
        }

        //METODO PARA DESINSTALAR UN JUEGO PREVIAMENTE INSTALADO
        private void DesinstalarJuego(int index)
        {
            if (index < 0 || index >= juegosGuardados.Count)
            {
                var ventanaError = new ErrorWindow("Índice de juego inválido.");
                ventanaError.Owner = Application.Current.MainWindow;
                ventanaError.ShowDialog();
                //MessageBox.Show("Índice de juego inválido.");
                return;
            }

            var juego = juegosGuardados[index];

            if (string.IsNullOrEmpty(juego.RutaEjecutable) || !System.IO.File.Exists(juego.RutaEjecutable))
            {
                var ventanaError = new ErrorWindow("El juego no está instalado o no se encontró la ruta de instalación.");
                ventanaError.Owner = Application.Current.MainWindow;
                ventanaError.ShowDialog();
                //MessageBox.Show("El juego no está instalado o no se encontró la ruta de instalación.");
                return;
            }

            try
            {
                // Obtener carpeta raíz de la instalación (la carpeta que contiene el ejecutable)
                string carpetaInstalacion = System.IO.Path.GetDirectoryName(juego.RutaEjecutable);

                // Opcional: Confirmar con el usuario
                var resultado = MessageBox.Show($"¿Quieres desinstalar el juego \"{juego.Nombre}\" y eliminar todos sus archivos?",
                                                "Confirmar desinstalación",
                                                MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (resultado != MessageBoxResult.Yes)
                    return;

                // Eliminar carpeta de instalación y todo su contenido
                if (Directory.Exists(carpetaInstalacion))
                {
                    Directory.Delete(carpetaInstalacion, true); // true para borrar recursivamente
                }

                // Limpiar ruta ejecutable
                juego.RutaEjecutable = null;

                // Guardar cambios en JSON
                GuardarJuegosEnJson();

                QuitarHandlersClickDeBoton();

                // Actualizar estado botón a "Instalar"
                ActualizarBotonInstalacion(BotonJugar, juego);

                // Cambiar handler para que ejecute la instalación, si tienes uno
                //BotonJugar.Click += BotonInstalar_Click;

                string mensaje = $"El juego \"{juego.Nombre}\" ha sido desinstalado correctamente.";
                var ventanaError = new ErrorWindow(mensaje);
                ventanaError.Owner = Application.Current.MainWindow;
                ventanaError.ShowDialog();
                //MessageBox.Show($"El juego \"{juego.Nombre}\" ha sido desinstalado correctamente.");
            }
            catch (Exception ex)
            {
                string mensaje = $"Error al desinstalar el juego:\n{ex.Message}";
                var ventanaError = new ErrorWindow(mensaje);
                ventanaError.Owner = Application.Current.MainWindow;
                ventanaError.ShowDialog();
                //MessageBox.Show($"Error al desinstalar el juego:\n{ex.Message}");
            }
        }



        //METODO PARA GENERAR LA IMAGEN SI NO SE CARGA CORRECTAMENTE POR QUE NO EXISTE
        private BitmapImage GenerarImagenFallback(string appid, string nombreVertical)
        {
            //INTENTO CARGAR EL LOCAL PRIMERO
            if (LocalStorage.ExisteImagen(nombreVertical))
            {
                return LocalStorage.CargarImagenLocal(nombreVertical);
            }

            int id = int.Parse(appid);
            var juego = bibliotecaTotal?.juegos?.FirstOrDefault(j => j.app_id == id);

            if (juego == null)
                return null;

            string fallbackUrl = juego.header ?? juego.captura;

            if (string.IsNullOrEmpty(fallbackUrl))
                return null;

            try
            {
                //DECARGAR LA IMAGEN EN MEMORIA
                byte[] imageBytes;
                using (var httpClient = new HttpClient())
                {
                    imageBytes = httpClient.GetByteArrayAsync(fallbackUrl).GetAwaiter().GetResult();
                }


                BitmapImage imagenCargada = new BitmapImage();
                using (var ms = new MemoryStream(imageBytes))
                {
                    imagenCargada.BeginInit();
                    imagenCargada.CacheOption = BitmapCacheOption.OnLoad;
                    imagenCargada.StreamSource = ms;
                    imagenCargada.EndInit();
                    imagenCargada.Freeze();
                }

                if (imagenCargada.PixelWidth == 0 || imagenCargada.PixelHeight == 0)
                {
                    MessageBox.Show("La imagen no se cargó correctamente o está vacía.");
                    return null;
                }

                int anchoFinal = 300;
                int altoFinal = 450;

                double ratioOriginal = (double)imagenCargada.PixelWidth / imagenCargada.PixelHeight;
                double ratioDestino = (double)anchoFinal / altoFinal;

                //CALCULAMOS EL TAMAÑO DE LA IMAGEN PEQUEÑA PARA QUE CUMPLA LA PROPORCION
                int anchoRender;
                int altoRender;
                if (ratioOriginal > ratioDestino)
                {
                    anchoRender = anchoFinal;
                    altoRender = (int)(anchoFinal / ratioOriginal);
                }
                else
                {
                    altoRender = altoFinal;
                    anchoRender = (int)(altoFinal * ratioOriginal);
                }
                int desplazamientoY = (altoFinal - altoRender) / 2;

                //IMAGEN DE FONDO CON BLUR
                double ratioFondo = (double)imagenCargada.PixelWidth / imagenCargada.PixelHeight;

                int anchoFondo, altoFondo;
                if (ratioFondo > (double)anchoFinal / altoFinal)
                {
                    altoFondo = altoFinal;
                    anchoFondo = (int)(altoFinal * ratioFondo);
                }
                else
                {
                    anchoFondo = anchoFinal;
                    altoFondo = (int)(anchoFinal / ratioFondo);
                }

                int offsetX = (anchoFinal - anchoFondo) / 2;
                int offsetY = (altoFinal - altoFondo) / 2;

                DrawingVisual fondoVisual = new DrawingVisual();
                using (DrawingContext dc = fondoVisual.RenderOpen())
                {
                    dc.DrawImage(imagenCargada, new Rect(offsetX, offsetY, anchoFondo, altoFondo));
                }
                RenderTargetBitmap fondoBitmap = new RenderTargetBitmap(anchoFinal, altoFinal, 96, 96, PixelFormats.Pbgra32);
                fondoBitmap.Render(fondoVisual);

                //APLICAR BLUR A LA IMAGEN
                DrawingVisual blurVisual = new DrawingVisual();
                blurVisual.Effect = new BlurEffect { Radius = 15 };
                using (DrawingContext dc = blurVisual.RenderOpen())
                {
                    dc.DrawImage(fondoBitmap, new Rect(0, 0, anchoFinal, altoFinal));
                }
                RenderTargetBitmap fondoBlurred = new RenderTargetBitmap(anchoFinal, altoFinal, 96, 96, PixelFormats.Pbgra32);
                fondoBlurred.Render(blurVisual);

                //CREAMOS LA IAMGEN CENTRADA Y DETRAS LA DE FONDO CON BLUR
                DrawingVisual finalVisual = new DrawingVisual();
                using (DrawingContext dc = finalVisual.RenderOpen())
                {
                    //FONDO BORROSO
                    dc.DrawImage(fondoBlurred, new Rect(0, 0, anchoFinal, altoFinal));

                    //IMAGEN PEQUEÑA
                    dc.DrawImage(imagenCargada, new Rect((anchoFinal - anchoRender) / 2, desplazamientoY, anchoRender, altoRender));
                }

                RenderTargetBitmap renderBitmap = new RenderTargetBitmap(anchoFinal, altoFinal, 96, 96, PixelFormats.Pbgra32);
                renderBitmap.Render(finalVisual);

                //GUARDAR IMAGEN RENDERIZADA
                string nombreArchivoRender = $"{appid}_vertical.jpg";
                LocalStorage.GuardarImagenRenderizada(nombreArchivoRender, renderBitmap);

                var bmp = new BitmapImage();
                using (var stream = new MemoryStream())
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                    encoder.Save(stream);
                    stream.Position = 0;

                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = stream;
                    bmp.EndInit();
                    bmp.Freeze();
                }

                return bmp;
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR: " + ex.Message);
                return null;
            }
        }

        public static BitmapSource RenderVisualToBitmap(UIElement element, int width, int height, double dpi = 96)
        {
            var renderTarget = new RenderTargetBitmap(width, height, dpi, dpi, PixelFormats.Pbgra32);
            element.Measure(new Size(width, height));
            element.Arrange(new Rect(new Size(width, height)));
            renderTarget.Render(element);
            return renderTarget;
        }





        private void ImagenJuego_Click(object sender, MouseButtonEventArgs e)
        {
            int? index = null;

            if (sender is Image img && img.Tag is int i1)
                index = i1;
            else if (sender is Grid grid && grid.Tag is int i2)
                index = i2;

            if (index.HasValue)
            {
                //ANIMACIÓN SOLO PARA imgFondo CON Completed
                var fadeOutImgFondo = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(300)));
                fadeOutImgFondo.Completed += (s, _) =>
                {
                    //QUITAMOS MANEJADORES ANTERIORES
                    imgFondo.ImageFailed -= ImgFondo_ImageFailed;
                    ImagenLogo.ImageFailed -= ImagenLogo_ImageFailed;
                    txtFalloLogo.Background = Brushes.Transparent;
                    txtFalloLogo.Text = "";

                    //ASIGNAMOS TAG PARA SABER QUÉ ÍNDICE ES EN EL EVENTO
                    ImagenLogo.Tag = index;
                    imgFondo.Tag = index;

                    appidActual = listaAppids[index.Value];

                    //COMRPOBAMOS SI EL JUEGO ES DESCARGABLE
                    bool esDescargable = EsDescargable(ventanaPrincipal.Usuario.IdUsuario, appidActual);
                    //MessageBox.Show(esDescargable.ToString());

                    //CAMBIAR ESTADO DEL BOTÓN SEGÚN RUTA
                    var juegoSeleccionado = juegosGuardados[index.Value];
                    ActualizarEstadoBotonJuego(BotonJugar, juegoSeleccionado);

                    //SI YA ESTÁ INSTALADO, NO MOSTRAR BOTÓN DE INSTALACIÓN
                    if (!string.IsNullOrWhiteSpace(juegoSeleccionado.RutaEjecutable) && System.IO.File.Exists(juegoSeleccionado.RutaEjecutable))
                    {
                        //EL JUEGO YA ESTÁ INSTALADO, OCULTAR BOTÓN DE INSTALAR
                        BotonJugar.Content = "JUGAR";
                    }
                    else if (esDescargable)
                    {
                        ActualizarBotonInstalacion(BotonJugar, juegoSeleccionado);
                    }



                    //ASIGNAMOS MANEJADORES DE NUEVO
                    imgFondo.ImageFailed += ImgFondo_ImageFailed;
                    ImagenLogo.ImageFailed += ImagenLogo_ImageFailed;

                    //CAMBIAMOS IMÁGENES
                    imgFondo.Source = imagenesFondo[index.Value];

                    if (imagenesLogos[index.Value] != null)
                        ImagenLogo.Source = imagenesLogos[index.Value];
                    else
                        ImagenLogo.Source = new BitmapImage(new Uri("https://noexiste.este.dominio/logo.png"));

                    //ANIMACIÓN ENTRADA
                    var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
                    imgFondo.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                    ImagenLogo.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                    txtFalloLogo.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                    BotonJugar.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                };

                //EJECUTAMOS fadeOut SOLO EN imgFondo CON Completed (ASI NO SE EJECUTA VARIAS VECES)
                imgFondo.BeginAnimation(UIElement.OpacityProperty, fadeOutImgFondo);

                //OTRAS ANIMACIONES SIN Completed
                var fadeOutSimple = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
                ImagenLogo.BeginAnimation(UIElement.OpacityProperty, fadeOutSimple);
                txtFalloLogo.BeginAnimation(UIElement.OpacityProperty, fadeOutSimple);
                BotonJugar.BeginAnimation(UIElement.OpacityProperty, fadeOutSimple);
            }

        }

        public bool EsDescargable(int userId, int appId)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    string url = $"http://{Config.IP}:{Config.Puerto}/library/{userId}/downloadable/{appId}";
                    var response = client.GetAsync(url).Result; 
                    if (!response.IsSuccessStatusCode) return false;

                    string json = response.Content.ReadAsStringAsync().Result;
                    dynamic result = JsonConvert.DeserializeObject(json);
                    return result.descargable == true;
                }
                catch
                {
                    return false;
                }
            }
        }

        //METODO PARA ACTUALIZAR EL ESTADO DEL BOTON DEPENDIENDO DE SI TIENE RUTA COLOCADA EN EL ARCHIVO O NO
        private void ActualizarEstadoBotonJuego(Button boton, JuegoInfo juego)
        {
            QuitarHandlersClickDeBoton();

            if (!string.IsNullOrWhiteSpace(juego.RutaEjecutable))
            {
                if(appidActual == int.Parse(juego.AppId))
                {
                    boton.Tag = "Play";
                    boton.Content = "Jugar";
                    //GUARDAMOS EL JUEGO PARA QUE LO USE EL HADLER
                    juegoSteamSeleccionado = juego;

                    //ASIGNAMOS EL EVENTO ESPECIAL PARA LOS JUEGOS QUE NO SON AGREGADOS MANUALMENTE
                    boton.Click += BotonJugar_Click;
                }
               
            }
            else
            {
                boton.Tag = "Unavailable";
                boton.Content = "No disponible";
            }
        }

        //METODO PARA CAMBIAR EL BOTON A INSTALAR JUEGO
        private void ActualizarBotonInstalacion(Button boton, JuegoInfo juego)
        {
            QuitarHandlersClickDeBoton();

            boton.Tag = "Install";
            boton.Content = "Instalar";

            boton.Click += BotonInstalar_Click;

            juegoSteamSeleccionado = juego;

        }


        //EVENTO DE CLICK DE LOS JUEGOS AGREGADOS POR LA TIENDA
        private void BotonJugar_Click(object sender, RoutedEventArgs e)
        {
            if (juegoSteamSeleccionado != null)
            {
                int index = juegosGuardados.FindIndex(j => j.AppId == juegoSteamSeleccionado.AppId);
                if (index != -1)
                    IniciarJuego(index);
            }
        }

        //EVENTO PARA DESCARGAR UN JUEGO
        private async void BotonInstalar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (juegoSteamSeleccionado == null)
                {
                    var ventanaError = new ErrorWindow("No se ha seleccionado ningún juego.");
                    ventanaError.Owner = Application.Current.MainWindow;
                    ventanaError.ShowDialog();
                    //MessageBox.Show("No se ha seleccionado ningún juego.");
                    return;
                }

                //PEDIR RUTA DE DESCARGA AL USUARIO
                var dialog = new System.Windows.Forms.FolderBrowserDialog();
                if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;

                string carpetaDestino = dialog.SelectedPath;
                string url = $"http://{Config.IP}:{Config.Puerto}/library/{ventanaPrincipal.Usuario.IdUsuario}/download/{juegoSteamSeleccionado.AppId}";

                string archivoZip = System.IO.Path.Combine(carpetaDestino, $"{juegoSteamSeleccionado.AppId}.zip");
                string rutaDescomprimida = carpetaDestino;


                using (HttpClient client = new HttpClient())
                using (HttpResponseMessage response = await client.GetAsync(url))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        BotonJugar.Tag = "Downloading";
                        BotonJugar.Content = "Descargando...";
                        BotonJugar.IsEnabled = false;

                        using (FileStream fs = new FileStream(archivoZip, FileMode.Create))
                        {
                            await response.Content.CopyToAsync(fs);
                        }


                        ZipFile.ExtractToDirectory(archivoZip, rutaDescomprimida);
                        System.IO.File.Delete(archivoZip);

                        //GUARDAR LA RUTA DEL EJECUTABLE (puedes mejorar esta parte si conoces el nombre exacto)
                        string posibleExe = Directory.GetFiles(rutaDescomprimida, "*.exe", SearchOption.AllDirectories).FirstOrDefault();
                        if (posibleExe != null)
                        {
                            BotonJugar.IsEnabled = true;
                            QuitarHandlersClickDeBoton();
                            juegoSteamSeleccionado.RutaEjecutable = posibleExe;
                            GuardarJuegosEnJson();
                            ActualizarEstadoBotonJuego(BotonJugar, juegoSteamSeleccionado); //Esto pone el botón en modo "JUGAR"

                            //AHORA CAMBIA EL HANDLER PARA QUE EJECUTE EL JUEGO
                            BotonJugar.Click -= BotonInstalar_Click;
                        }

                        var ventanaError = new ErrorWindow("Juego descargado e instalado correctamente.");
                        ventanaError.Owner = Application.Current.MainWindow;
                        ventanaError.ShowDialog();
                        //MessageBox.Show("Juego descargado e instalado correctamente.");
                    }
                    else
                    {
                        string mensaje = $"Error al descargar el juego:\n{response.ReasonPhrase}";
                        var ventanaError = new ErrorWindow(mensaje);
                        ventanaError.Owner = Application.Current.MainWindow;
                        ventanaError.ShowDialog();
                        //MessageBox.Show($"Error al descargar el juego:\n{response.ReasonPhrase}");
                    }
                }
            }
            catch (Exception ex)
            {
                string mensaje = $"Error inesperado:\n{ex.Message}";
                var ventanaError = new ErrorWindow(mensaje);
                ventanaError.Owner = Application.Current.MainWindow;
                ventanaError.ShowDialog();
                //MessageBox.Show($"Error inesperado:\n{ex.Message}");
            }
        }



        private async void ImgFondo_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            if (sender is Image img && img.Tag is int index)
            {
                string nombreFondo = $"{appids[index]}_fondo.jpg";
                string fallbackString = $"https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/{appids[index]}/header.jpg";
                Uri fallbackUri = new Uri($"https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/{appids[index]}/header.jpg");
                img.Source = new BitmapImage(fallbackUri);
                var imagenFondo = await ObtenerImagenAsync(fallbackString, nombreFondo);
                if (imagenFondo != null)
                    imagenesFondo.Add(imagenFondo);
            }
        }

        private void ImagenLogo_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            if (sender is Image img && img.Tag is int idx)
            {
                
                txtFalloLogo.Background = (Brush)(new BrushConverter().ConvertFrom("#80000000"));
                txtFalloLogo.Text = nombresJuegos[idx];
                txtFalloLogo.Visibility = Visibility.Visible;
                
            }
        }


        //PARTE PARA EL BOTON DE AÑADIR JUEGO MANUALMENTE
        private void AgregarJuego_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Ejecutables (*.exe)|*.exe",
                Title = "Selecciona el ejecutable del juego"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string ruta = openFileDialog.FileName;
                string nombreJuego = System.IO.Path.GetFileNameWithoutExtension(ruta); 
                string nuevoAppId = DateTime.Now.Ticks.ToString();

                var nuevoJuego = new JuegoInfo
                {
                    AppId = nuevoAppId,
                    Nombre = nombreJuego,
                    RutaEjecutable = ruta
                };

                juegosAgregadosGuardados.Add(nuevoJuego);
                GuardarJuegosAgregadosEnJson();
                CargarJuegosEnInterfaz();

                var juegoAgregado = new VJuegoAgregadoCorrecto();
                juegoAgregado.Show();
            }
        }




        private string rutaJsonJuegosAgregados = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Osiris", "juegos_agregados_biblioteca.json");







        //METODO PARA GUARDAR LOS JUEGOS EN EL JSON
        private void GuardarJuegosAgregadosEnJson()
        {
            string json = JsonConvert.SerializeObject(juegosAgregadosGuardados, Formatting.Indented);
            System.IO.File.WriteAllText(rutaJsonJuegosAgregados, json);
        }

        //METODO QUE METE LOS JUEGOS AGREGADOS POR NOSOTROSS EN LA INTERFAZ
        private void CargarJuegosEnInterfaz()
        {
            if (!System.IO.File.Exists(rutaJsonJuegosAgregados))
                return;

            string json = System.IO.File.ReadAllText(rutaJsonJuegosAgregados);
            var juegosManual = Newtonsoft.Json.JsonConvert.DeserializeObject<List<JuegoInfo>>(json);

            if (juegosManual == null)
                return;

            juegosAgregadosGuardados.Clear();

            //ELIMINAR SOLO LOS MANUALES
            for (int i = panelJuegosBiblioteca.Children.Count - 1; i >= 0; i--)
            {
                var child = panelJuegosBiblioteca.Children[i] as Grid;
                if (child?.Tag is JuegoInfo info && info.EsManual)
                {
                    panelJuegosBiblioteca.Children.RemoveAt(i);
                }
            }

            foreach (var juego in juegosManual)
            {
                juego.EsManual = true;
                juegosAgregadosGuardados.Add(juego);

                var scale = new ScaleTransform(1.0, 1.0);
                double altura = 170;
                double proporcion = 600.0 / 900.0;
                double ancho = altura * proporcion;

                Grid contenedor = new Grid
                {
                    Height = altura,
                    Width = ancho,
                    Margin = new Thickness(11),
                    Tag = juego,
                    RenderTransform = scale,
                    RenderTransformOrigin = new Point(0.5, 0.5)
                };

                Image img = new Image
                {
                    Source = new BitmapImage(new Uri("pack://application:,,,/res/library/juego_vertical_manual.png")),
                    Stretch = Stretch.UniformToFill,
                    Height = altura
                };


                //TEXTO CON EL NOMBRE DEL JUEGO
                string nombreExe = System.IO.Path.GetFileNameWithoutExtension(juego.RutaEjecutable).ToUpper();
                TextBlock texto = new TextBlock
                {
                    Text = nombreExe,
                    Foreground = AppTheme.Actual.TextoPrincipal,
                    Background = new SolidColorBrush(Color.FromArgb(160, 0, 0, 0)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 12,
                    IsHitTestVisible = false,
                    Padding = new Thickness(4),
                    MaxWidth = ancho,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };

                //AÑADIR AL GRID
                contenedor.Children.Add(img);
                contenedor.Children.Add(texto);

                //ZOOM SOBRE EL CONTENEDOR
                contenedor.MouseEnter += (s, e) =>
                {
                    var anim = new DoubleAnimation(1.0, 1.1, TimeSpan.FromMilliseconds(150));
                    scale.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
                    scale.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
                };

                contenedor.MouseLeave += (s, e) =>
                {
                    var anim = new DoubleAnimation(1.1, 1.0, TimeSpan.FromMilliseconds(150));
                    scale.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
                    scale.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
                };

                contenedor.MouseLeftButtonUp += ImagenJuegoManual_Click;

                //MENÚ CONTEXTUAL
                ContextMenu menu = CrearContextMenuJuegoManual(juego);
                contenedor.ContextMenu = menu;
                contenedor.MouseRightButtonUp += (s, e) =>
                {
                    contenedor.ContextMenu.IsOpen = true;
                    contenedor.ContextMenu.PlacementTarget = contenedor;
                };

                panelJuegosBiblioteca.Children.Add(contenedor);
            }
        }

        private ContextMenu CrearContextMenuJuegoManual(JuegoInfo juego)
        {
            ContextMenu menu = new ContextMenu
            {
                Style = (Style)FindResource("EstiloContextMenu")
            };

            MenuItem jugarItem = new MenuItem
            {
                Header = "Jugar",
                Style = (Style)FindResource("EstiloMenuItem"),
                Tag = juego // ← ASOCIAMOS EL JUEGO AQUÍ
            };

            MenuItem eliminarItem = new MenuItem
            {
                Header = "Eliminar de la biblioteca",
                Style = (Style)FindResource("EstiloMenuItem"),
                Tag = juego //ASOCIAMOS EL JUEGO
            };


            jugarItem.Click += JugarDesdeContextMenu;
            eliminarItem.Click += EliminarDesdeContextMenu;

            menu.Items.Add(jugarItem);
            menu.Items.Add(eliminarItem);

            return menu;
        }

        //METODO PARA CARGAR EL EVENTO DE CLCIK CUANDO HACEMOS CLICK EN UN JUEGO AGREGADO POR EL USER
        private void ImagenJuegoManual_Click(object sender, MouseButtonEventArgs e)
        {
            var grid = sender as Grid;
            if (grid == null || !(grid.Tag is JuegoInfo juego))
                return;

            juegoManualSeleccionado = juego;

            //ANIMACIÓN SOLO PARA imgFondo CON Completed
            var fadeOutImgFondo = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(300)));
            fadeOutImgFondo.Completed += (s, _) =>
            {
                //LIMPIAMOS LOS HANDLER PREVIOS
                QuitarHandlersClickDeBoton();

                //QUITAMOS MANEJADORES ANTERIORES
                imgFondo.ImageFailed -= ImgFondo_ImageFailed;
                ImagenLogo.ImageFailed -= ImagenLogo_ImageFailed;
                txtFalloLogo.Background = Brushes.Transparent;
                string nombreExe = System.IO.Path.GetFileNameWithoutExtension(juego.RutaEjecutable).ToUpper();
                txtFalloLogo.Text = nombreExe;
                txtFalloLogo.Background = new SolidColorBrush(Color.FromArgb(190, 0, 0, 0)); // 160 ≈ 62% opacidad


                //EVENTO ESPECIAL YA QUE ESTOS JUEGOS SIEMPRE VAN A TENER UNA RUTA
                BotonJugar.Tag = "Play";
                BotonJugar.Content = "Jugar";
                BotonJugar.Click += BotonJugar_Click_Manual;

                //CAMBIAMOS IMAGEN DE FONDO A UNA GENÉRICA
                imgFondo.Source = new BitmapImage(new Uri("pack://application:,,,/res/library/Banner_Juego_Manual.png"));

                //QUITAMOS LOGO
                ImagenLogo.Source = null;

                //ANIMACIÓN ENTRADA
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
                imgFondo.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                ImagenLogo.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                txtFalloLogo.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                BotonJugar.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            };

            //EJECUTAMOS fadeOut SOLO EN imgFondo CON Completed (ASI NO SE EJECUTA VARIAS VECES)
            imgFondo.BeginAnimation(UIElement.OpacityProperty, fadeOutImgFondo);

            //OTRAS ANIMACIONES SIN Completed
            var fadeOutSimple = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
            ImagenLogo.BeginAnimation(UIElement.OpacityProperty, fadeOutSimple);
            txtFalloLogo.BeginAnimation(UIElement.OpacityProperty, fadeOutSimple);
            BotonJugar.BeginAnimation(UIElement.OpacityProperty, fadeOutSimple);
        }

        

        //EVENTO PARA EL CLICK DE LOS JUEGOS QUE HAN SIDO AREGADOS MANUALMEENTE
        private JuegoInfo juegoManualSeleccionado;
        private void BotonJugar_Click_Manual(object sender, RoutedEventArgs e)
        {
            if (juegoManualSeleccionado != null)
            {
                if (System.IO.File.Exists(juegoManualSeleccionado.RutaEjecutable))
                    Process.Start(juegoManualSeleccionado.RutaEjecutable);
                else
                {
                    var ventanaError = new ErrorWindow("No se encontró el ejecutable del juego.");
                    ventanaError.Owner = Application.Current.MainWindow;
                    ventanaError.ShowDialog();
                    //MessageBox.Show("No se encontró el ejecutable del juego.");
                }
                
            }
        }

        //EVENETO DE JUGAR DEL CONTEXTMENU
        private void JugarDesdeContextMenu(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item && item.Tag is JuegoInfo juego)
            {
                if (System.IO.File.Exists(juego.RutaEjecutable))
                    Process.Start(juego.RutaEjecutable);
                else
                {
                    var ventanaError = new ErrorWindow("No se encontró el ejecutable del juego.");
                    ventanaError.Owner = Application.Current.MainWindow; //opcional para centrar
                    ventanaError.ShowDialog();
                    //MessageBox.Show("No se encontró el ejecutable del juego.");
                }
                    
            }
        }

        private void EliminarDesdeContextMenu(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item && item.Tag is JuegoInfo juego)
            {
                var ventanaConfirmacion = new VEliminarJuego();
                ventanaConfirmacion.Owner = Application.Current.MainWindow; //opcional para centrar
                ventanaConfirmacion.ShowDialog();

                if (ventanaConfirmacion.Confirmado)
                {
                    //LO ELIMINAMOS DEL LISTADO DE LA MEMORIA
                    juegosAgregadosGuardados.RemoveAll(j => j.AppId == juego.AppId);

                    //LO ELIMINAMOS DEL PANEL VISUAL
                    for (int i = panelJuegosBiblioteca.Children.Count - 1; i >= 0; i--)
                    {
                        if (panelJuegosBiblioteca.Children[i] is Grid grid && grid.Tag is JuegoInfo j && j.AppId == juego.AppId)
                        {
                            panelJuegosBiblioteca.Children.RemoveAt(i);
                            break;
                        }
                    }

                    //LO ELIMINAMOS DEL JSON Y GUARDAMOS
                    string jsonActualizado = Newtonsoft.Json.JsonConvert.SerializeObject(juegosAgregadosGuardados, Newtonsoft.Json.Formatting.Indented);
                    System.IO.File.WriteAllText(rutaJsonJuegosAgregados, jsonActualizado);
                }
            }
        }


        //METODO PARA QUITAR LOS HANDLER ANTERIORES (ESTO SIRVE PARA QUE AL HACER CLICK EN JUGAR, NO SE HABRAN DOS VECES)
        private void QuitarHandlersClickDeBoton()
        {
            BotonJugar.Click -= BotonJugar_Click; 
            BotonJugar.Click -= BotonJugar_Click_Manual;
                                                         
        }






        //PARTE PARA EL BUSCADOR
        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filtro = txtBuscar.Text.ToLower();
            panelJuegosBiblioteca.Children.Clear();

            bool hayCoincidencias = false;

            //BUCLE DE LOS JUEGOS DE LA TIENDA
            for (int i = 0; i < imagenVerticalJuegos.Count; i++)
            {
                var juego = bibliotecaTotal.juegos[i];

                if (!string.IsNullOrWhiteSpace(juego.nombre) && juego.nombre.ToLower().Contains(filtro))
                {
                    hayCoincidencias = true;

                    BitmapImage juegosBiblioteca = imagenVerticalJuegos[i]; // ✅
                    var scale = new ScaleTransform(1.0, 1.0);

                    Image img = new Image
                    {
                        Source = juegosBiblioteca,
                        Stretch = Stretch.Uniform,
                        Height = 170,
                        Margin = new Thickness(11),
                        Tag = i,
                        RenderTransform = scale,
                        RenderTransformOrigin = new Point(0.5, 0.5)
                    };

                    img.MouseLeftButtonUp += ImagenJuego_Click;

                    img.MouseEnter += (s2, e2) =>
                    {
                        var anim = new DoubleAnimation(1.0, 1.1, TimeSpan.FromMilliseconds(150));
                        scale.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
                        scale.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
                    };

                    img.MouseLeave += (s2, e2) =>
                    {
                        var anim = new DoubleAnimation(1.1, 1.0, TimeSpan.FromMilliseconds(150));
                        scale.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
                        scale.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
                    };

                    panelJuegosBiblioteca.Children.Add(img);
                }
            }

            //BUCLE DE LOS JUEGOS AGREGADOS POR NOSOTROS
            foreach (var juego in juegosAgregadosGuardados)
            {
                if (juego.Nombre.ToLower().Contains(filtro))
                {
                    hayCoincidencias = true;

                    var scale = new ScaleTransform(1.0, 1.0);
                    double altura = 170;
                    double proporcion = 600.0 / 900.0;
                    double ancho = altura * proporcion;

                    Grid contenedor = new Grid
                    {
                        Height = altura,
                        Width = ancho,
                        Margin = new Thickness(11),
                        Tag = juego,
                        RenderTransform = scale,
                        RenderTransformOrigin = new Point(0.5, 0.5)
                    };

                    Image img = new Image
                    {
                        Source = new BitmapImage(new Uri("pack://application:,,,/res/library/juego_vertical_manual.png")),
                        Stretch = Stretch.UniformToFill,
                        Height = altura
                    };

                    TextBlock texto = new TextBlock
                    {
                        Text = juego.Nombre.ToUpper(),
                        Foreground = AppTheme.Actual.TextoPrincipal,
                        Background = new SolidColorBrush(Color.FromArgb(160, 0, 0, 0)),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextAlignment = TextAlignment.Center,
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 12,
                        IsHitTestVisible = false,
                        Padding = new Thickness(4),
                        MaxWidth = ancho,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    };

                    contenedor.Children.Add(img);
                    contenedor.Children.Add(texto);

                    contenedor.MouseEnter += (s, e2) =>
                    {
                        var anim = new DoubleAnimation(1.0, 1.1, TimeSpan.FromMilliseconds(150));
                        scale.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
                        scale.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
                    };

                    contenedor.MouseLeave += (s, e2) =>
                    {
                        var anim = new DoubleAnimation(1.1, 1.0, TimeSpan.FromMilliseconds(150));
                        scale.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
                        scale.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
                    };

                    contenedor.MouseLeftButtonUp += ImagenJuegoManual_Click;

                    // CONTEXT MENU
                    ContextMenu menu = CrearContextMenuJuegoManual(juego);
                    contenedor.ContextMenu = menu;
                    contenedor.MouseRightButtonUp += (s, e2) =>
                    {
                        contenedor.ContextMenu.IsOpen = true;
                        contenedor.ContextMenu.PlacementTarget = contenedor;
                    };

                    panelJuegosBiblioteca.Children.Add(contenedor);
                }
            }


            if (!hayCoincidencias)
            {

                cargarMensajeBiblioteca("Ningún juego coincide con tu búsqueda. Quizás lo encuentres en la tienda.");
                

            }
        }

        private void cargarMensajeBiblioteca(string mensaje)
        {
            Grid gridContenedor = new Grid()
            {
                Width = panelJuegosBiblioteca.ActualWidth,
                Height = panelJuegosBiblioteca.ActualHeight,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            TextBlock textoError = new TextBlock()
            {
                Text = mensaje,
                Foreground = Brushes.White,
                FontSize = 16,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MaxWidth = 500
            };

            gridContenedor.Children.Add(textoError);
            panelJuegosBiblioteca.Children.Add(gridContenedor);

            panelJuegosBiblioteca.SizeChanged += (s, ev) =>
            {
                gridContenedor.Width = panelJuegosBiblioteca.ActualWidth;
                gridContenedor.Height = panelJuegosBiblioteca.ActualHeight;
            };
        }


        //EFECTOS
        //METODO PARA HACER EL EFECTO FADEIN EN TODOS LOS ELEMENTOS DE LA INTERFAZ
        private async Task FadeOutATodo()
        {
            await AplicarFadeOutAsync(gridPrincipal);

        }


        //METODO PARA QUE TODO SE VEA MAS NATURAL CON UNA TRASICION FADEIN
        private void AplicarFadeIn(UIElement elemento)
        {
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
            elemento.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }

        private async Task AplicarFadeOutAsync(UIElement elemento)
        {
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
            elemento.BeginAnimation(UIElement.OpacityProperty, fadeOut);

            await Task.Delay(500); //ESPERA A QUE TERMINE LA ANIMACIÓN
            AplicarFadeIn(elemento);
        }


    }
}

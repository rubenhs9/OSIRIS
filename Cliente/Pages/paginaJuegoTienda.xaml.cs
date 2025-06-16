using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Cliente_TFG.Classes;
using HtmlAgilityPack;
using Newtonsoft.Json;
using static Cliente_TFG.Pages.paginaTienda;
using static Cliente_TFG.Classes.Notificacion;
using Newtonsoft.Json.Linq;


namespace Cliente_TFG.Pages
{
    /// <summary>
    /// Lógica de interacción para paginaJuegoTienda.xaml
    /// </summary>
    public partial class paginaJuegoTienda : Page
    {
        private MainWindow ventanaPrincipal;
        private Notificacion notificacion;
        private int appid;

        private int indiceActual = 0;
        private DispatcherTimer timerCarrusel;
        private List<Image> imagenesCarrusel = new List<Image>();

        private int imagenesCargadas = 0; //CONTADOR PARA LAS IMAGENES CARGADAS
        private bool animacionCompletada = false;

        private List<string> miniaturas = new List<string>();
        private List<string> categorias = new List<string>();
        private List<string> generos = new List<string>();
        private string desarrollador;
        private string editores;
        private string descripcionCorta;
        private string descripcionDetallada;
        private string fechaLanzamiento;
        private string imagenCabecera;
        private string nombreJuego;
        private string descuento;
        private string precioInicial;

        private static readonly HttpClient client = new HttpClient();



        public paginaJuegoTienda(MainWindow ventanaPrincipal, int appid)
        {
            InitializeComponent();
            this.ventanaPrincipal = ventanaPrincipal;
            this.appid = appid;
            this.notificacion = new Notificacion(panelNotificaciones);

            //APLICAMOS LOS BORDES A TODOS LOS COMPONENTES
            bordeDatosJuegos.BorderBrush = AppTheme.Actual.BordePanel;
            bordeCarrusel.BorderBrush = AppTheme.Actual.BordePanel;
            bordeDescripccionLarga.BorderBrush = AppTheme.Actual.BordePanel;
            bordeCategoriasGeneros.BorderBrush = AppTheme.Actual.BordePanel;

            AplicarEsquinasRedondeadas(clipContainer, 15);
            AplicarEsquinasRedondeadas(gridCarrusel, 15);
            AplicarEsquinasRedondeadas(gridDescripccionLarga, 15);
            AplicarEsquinasRedondeadas(gridCategoriasGeneros, 15);

            // LLAMADA ASÍNCRONA CORRECTA
            _ = InicializarPaginaAsync();


        }

        private void AplicarEsquinasRedondeadas(FrameworkElement contenedor, double radio)
        {
            contenedor.Loaded += (s, e) =>
            {
                var rect = new RectangleGeometry
                {
                    RadiusX = radio,
                    RadiusY = radio,
                    Rect = new Rect(0, 0, contenedor.ActualWidth, contenedor.ActualHeight)
                };

                contenedor.Clip = rect;

                contenedor.SizeChanged += (s2, e2) =>
                {
                    rect.Rect = new Rect(0, 0, contenedor.ActualWidth, contenedor.ActualHeight);
                };
            };
        }


        private async Task InicializarPaginaAsync()
        {
            //CARGAR TEMA
            cargarTema();

            //CARGAR LOS DATOS
            await CargarDatosJson();

            //CARGAR IMAGEN DE FONDO QUE SERA LA PRIMERA MINIATURA QUE TEGAMOS
            cargarFondo();

            //PARTE DERECHA
            CargarImagenPrincipal();
            botonCompra.Opacity = 1;
            CargarPrecio();
            CargarBotonCompra();
            CargarDescripccionCorta();
            CargarFechaLanzamiento();
            CargarDesarrolador();
            CargarEditor();

            //PARTE IZQUIERDA
            CargarCarrusel();
            CargarDescripccionLarga();
            CargarCategoriasGeneros();

            

        }

        private void cargarTema()
        {
            panelDatosJuego.Background = AppTheme.Actual.FondoPanel;
            gridCarrusel.Background = AppTheme.Actual.FondoPanel;
            gridDescripccionLarga.Background = AppTheme.Actual.FondoPanel;
            gridCategoriasGeneros.Background = AppTheme.Actual.FondoPanel;
        }

        private void cargarFondo()
        {
            if (miniaturas.Count > 0)
            {
                imagenBackground.Source = new BitmapImage(new Uri(miniaturas[0], UriKind.Absolute));
                AplicarFadeIn(imagenBackground);
            }
            else
            {
                imagenBackground.Source = null;
            }
        }

        private void AplicarFadeIn(UIElement elemento)
        {
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 0.1,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
            elemento.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }

        //PARTE DEL CARRUSEL
        private async void CargarCarrusel()
        {
            /*
            string[] capturas = new string[]
            {
                "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/730/ss_796601d9d67faf53486eeb26d0724347cea67ddc.1920x1080.jpg?t=1729703045",
                "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/730/ss_d830cfd0550fbb64d80e803e93c929c3abb02056.1920x1080.jpg?t=1729703045",
                "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/730/ss_13bb35638c0267759276f511ee97064773b37a51.1920x1080.jpg?t=1729703045",
                "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/730/ss_0f8cf82d019c614760fd20801f2bb4001da7ea77.1920x1080.jpg?t=1729703045",
                "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/730/ss_ef82850f036dac5772cb07dbc2d1116ea13eb163.1920x1080.jpg?t=1729703045",
                "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/730/ss_76f6730dbb911650ba1f41c8e5b4bac638b5beea.1920x1080.jpg?t=1729703045",
                "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/730/ss_808cdd373d78c3cf3a78e7026ebb1a15895e0670.1920x1080.jpg?t=1729703045",
                "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/730/ss_ef98db5d5a4d877531a5567df082b0fb62d75c80.1920x1080.jpg?t=1729703045",
                "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/730/ss_2254a50f27951fb9028bc00b93a7f2ed7aac1e13.1920x1080.jpg?t=1729703045",
                "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/730/ss_54b9c26b028c84d5f8a5316f31ae6203953ed84d.1920x1080.jpg?t=1729703045",
                "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/730/ss_1b3b5fd437939a7ed00a2155269e78994cb998d3.1920x1080.jpg?t=1729703045",
                "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/730/ss_352666c1949ce3966bd966d6ea5a1afd532257bc.1920x1080.jpg?t=1729703045",
                "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/730/ss_63d2733b9b4ace01a41d5ba8afd653245d05d54a.1920x1080.jpg?t=1729703045",
                "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/730/ss_fe70d46859593aef623a0614f4686e2814405035.1920x1080.jpg?t=1729703045",
                "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/730/ss_bb2af3e83ac0385ff2055f2ab9697cdd83e351b7.1920x1080.jpg?t=1729703045",
                "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/730/ss_fb8e5e2ae29ce64e2898315c66b5db08989e8f91.1920x1080.jpg?t=1729703045",
                "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/730/ss_0db84c628a798e38ca57d69abda119bee1358008.1920x1080.jpg?t=1729703045",
                "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/730/ss_18e9ea2715f0407ee05e206073927a648db60d73.1920x1080.jpg?t=1729703045",
                "https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/730/ss_2514675f364079b754b820cbc8b2e7c331d56a26.1920x1080.jpg?t=1729703045"
            };
            */

            //LIMPIO PRIMERO EL CONTENIDO POR SI ACASO
            
            panelCarrusel.Children.Clear();
            stackMiniaturas.Children.Clear();
            imagenesCarrusel.Clear();
            

            for (int i = 0; i < miniaturas.Count; i++)
            {
                //IMAGEN PRINCIPAL GRANDE DEL CARRUSEL
                Image imagen = new Image
                {
                    Stretch = Stretch.UniformToFill,
                    Visibility = i == 0 ? Visibility.Visible : Visibility.Hidden
                };

                Grid.SetRow(imagen, 0);
                Grid.SetColumn(imagen, 0);
                panelCarrusel.Children.Add(imagen);
                imagenesCarrusel.Add(imagen);

                //CARGAR IMAGENES DE FORMA ASYNC
                var bitmap = await CargarImagenAsync(miniaturas[i]);
                imagen.Source = bitmap;

                //IMAGENES DE LAS MINIATURAS
                Image miniatura = new Image
                {
                    Width = 100,
                    Height = 56,
                    Margin = new Thickness(5),
                    Cursor = Cursors.Hand,
                    VerticalAlignment = VerticalAlignment.Center
                };

                miniatura.Source = bitmap;

                //EVENTO DE CLICK DE LAS MINIATUAS
                int index = i; 
                miniatura.MouseLeftButtonDown += (sender, e) => CambiarImagenCarruselManual(index);

                stackMiniaturas.Children.Add(miniatura);

                imagenesCargadas++;

                //SI TODAS LAS IAMGENES SE HAN CARGADO, MUESTRO EL STACKPANEL DE LAS MINIATURAS
                if (imagenesCargadas == miniaturas.Count || imagenesCargadas >= 6)
                {
                    if (!animacionCompletada)
                    {
                        FadeIn(stackMiniaturas);
                        FadeIn(ScrollMiniaturas);
                    }
                    

                }
                else
                {
                    stackMiniaturas.Visibility = Visibility.Hidden;
                    ScrollMiniaturas.Visibility = Visibility.Hidden;
                }


            }


            IniciarCarrusel();
        }


        private void IniciarCarrusel()
        {
            timerCarrusel = new DispatcherTimer();
            timerCarrusel.Interval = TimeSpan.FromSeconds(7); //TIEMPO PARA CAMBIAR ENTRE IMAGENES
            timerCarrusel.Tick += CambiarImagenCarrusel;
            timerCarrusel.Start();
        }

        private void CambiarImagenCarrusel(object sender, EventArgs e)
        {
            if (imagenesCarrusel.Count == 0) return;

            imagenesCarrusel[indiceActual].Visibility = Visibility.Hidden;
            indiceActual = (indiceActual + 1) % imagenesCarrusel.Count;
            imagenesCarrusel[indiceActual].Visibility = Visibility.Visible;
        }

        private void CambiarImagenCarruselManual(int indice)
        {
            //COMPRUEBO QUE EL INDICE ES VALIDO
            if (indice < 0 || indice >= imagenesCarrusel.Count)
            {
                return;
            }

            //CAMBIO LA VISIBILIDAD EN TODAS LAS IMAGENES
            foreach (var img in imagenesCarrusel)
            {
                img.Visibility = Visibility.Hidden;
            }

            imagenesCarrusel[indice].Visibility = Visibility.Visible;
            indiceActual = indice;

            ReiniciarTemporizador();
        }

        //METODO PARA REINICIAR EL TEMPORIZADOR UNA VEZ HACES CLICK PARA QUE NO CAMBIE INSTANTANEAMENTE
        private void ReiniciarTemporizador()
        {
            if (timerCarrusel != null)
            {
                timerCarrusel.Stop();
                timerCarrusel.Start();
            }
        }

        private async Task<BitmapImage> CargarImagenAsync(string url)
        {
            var bitmap = new BitmapImage();
            try
            {
                var stream = await DescargarImagenAsync(url); //METODO ASINCRONO PARA DESCARGAR LA IMAGEN
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad; //CACHEAR IMAGEN POR SI SE USA MAS ADELANTE
                bitmap.StreamSource = stream;
                bitmap.EndInit();
            }
            catch (Exception ex)
            {
                notificacion.MostrarNotificacion("Error al cargar la imagen: " + ex.Message,NotificationType.Warning);
            }
            return bitmap;
        }

        private async Task<Stream> DescargarImagenAsync(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetAsync(url);
                return await response.Content.ReadAsStreamAsync();
            }
        }

        //METODO PARA LA ANIMACION DE LAS MINIATURAS
        private void FadeIn(UIElement element)
        {
            animacionCompletada = true;
            element.Opacity = 0;
            element.Visibility = Visibility.Visible;

            //CREO LA ANIMACION DE LA OPACIDAD
            var fadeInAnimation = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(1) //DURACION EN SEGUNDOS DE LA ANIMACION
            };
            //APLICO LA ANIMACION
            element.BeginAnimation(UIElement.OpacityProperty, fadeInAnimation);
        }


        //PARTE PARA LA DESCRIPCCION LARGA
        private void CargarDescripccionLarga()
        {
            panelDescripcionLarga.Children.Clear();
            string html = WebUtility.HtmlDecode(descripcionDetallada);
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            ProcesarNodoRecursivo(doc.DocumentNode);
            //panelDescripcionLarga.Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0));
        }

        private void ProcesarNodoRecursivo(HtmlNode node)
        {
            //SALTAR NODOS QUE NO SON RELEVANTES
            if (node.Name == "script" || node.Name == "style" || node.Name == "head") return;

            //PROCESO LAS IMAGENES PRIMERO
            if (node.Name == "img" && node.Attributes["src"] != null)
            {
                ProcesarImagen(node);
                return;
            }

            //MANEJO LOS SALTOS DE LINEA
            if (node.Name == "br")
            {
                panelDescripcionLarga.Children.Add(new TextBlock { Height = 10 });
                return;
            }

            //PROCESAR NODOS QUE NO TIENEN NODOS HIJOS
            if (node.NodeType == HtmlNodeType.Text || EsElementoTexto(node))
            {
                ProcesarTexto(node);
                return;
            }

            //PROCESO LISTAS Y SUS HIJOS
            if (node.Name == "ul" || node.Name == "ol")
            {
                foreach (var child in node.ChildNodes.Where(n => n.Name == "li"))
                {
                    ProcesarElementoLista(child);
                }
                return;
            }

            //OTROS ELEMENTOS RECURSIVAMENTE
            foreach (var child in node.ChildNodes)
            {
                ProcesarNodoRecursivo(child);
            }
        }

        private bool EsElementoTexto(HtmlNode node)
        {
            return (node.Name == "h1" || node.Name == "h2" || node.Name == "h3" ||
                   node.Name == "p" || node.Name == "strong") &&
                   !node.ChildNodes.Any(n => n.NodeType == HtmlNodeType.Element);
        }

        private void ProcesarImagen(HtmlNode node)
        {
            try
            {
                var img = new Image
                {
                    Source = new BitmapImage(new Uri(node.Attributes["src"].Value)),
                    Margin = new Thickness(20, 20, 20, 10),
                    Stretch = Stretch.Uniform
                };
                panelDescripcionLarga.Children.Add(img);
            }
            catch { /* NO HACER NADA SI NO CARGA LA IMAGEN */ }
        }

        private void ProcesarTexto(HtmlNode node)
        {
            var texto = HtmlEntity.DeEntitize(node.InnerText.Trim());
            if (string.IsNullOrWhiteSpace(texto)) return;

            var tb = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Foreground = AppTheme.Actual.TextoPrincipal,
                Margin = ObtenerMargenPorElemento(node),
                FontSize = ObtenerTamañoFuente(node),
                FontWeight = ObtenerPesoFuente(node),
                Text = FormatearTexto(node, texto)
            };

            panelDescripcionLarga.Children.Add(tb);
        }

        private void ProcesarElementoLista(HtmlNode liNode)
        {
            var texto = HtmlEntity.DeEntitize(liNode.InnerText.Trim());
            if (string.IsNullOrWhiteSpace(texto)) return;

            var tb = new TextBlock
            {
                Text = "   • " + texto,
                TextWrapping = TextWrapping.Wrap,
                Foreground = AppTheme.Actual.TextoPrincipal,
                Margin = new Thickness(15, 2, 10, 2)
            };

            panelDescripcionLarga.Children.Add(tb);
        }

        //METODOS AUX
        private Thickness ObtenerMargenPorElemento(HtmlNode node)
        {
            switch (node.Name)
            {
                case "h1": return new Thickness(10, 20, 10, 15);
                case "h2": return new Thickness(10, 15, 10, 10);
                case "strong": return new Thickness(10, 5, 10, 5);
                default: return new Thickness(10, 2, 10, 2);
            }
        }

        private double ObtenerTamañoFuente(HtmlNode node)
        {
            switch (node.Name)
            {
                case "h1": return 26;
                case "h2": return 20;
                case "h3": return 18;
                case "strong": return 14;
                default: return 12;
            }
        }

        private FontWeight ObtenerPesoFuente(HtmlNode node)
        {
            switch (node.Name)
            {
                case "h1": return FontWeights.Bold;
                case "h2": return FontWeights.SemiBold;
                case "strong": return FontWeights.Bold;
                default: return FontWeights.Normal;
            }
        }

        private string FormatearTexto(HtmlNode node, string texto)
        {
            if (node.Name == "h1" || node.Name == "h2") return texto.ToUpper();
            return texto;
        }



        //PARTE PARA LAS CATEGORIAS Y GENEROS
        private void CargarCategoriasGeneros()
        {
           
            //LIMPIAR EL GRID
            gridCategoriasGeneros.Children.Clear();
            gridCategoriasGeneros.RowDefinitions.Clear();

            //AÑADO UNA FILA PARA ENCABEZADOS
            gridCategoriasGeneros.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            TextBlock headerCategoria = new TextBlock
            {
                Text = "Categorías:",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(5),
                Foreground = AppTheme.Actual.TextoPrincipal,
                Padding = new Thickness(20, 20, 0, 0),
            };
            Grid.SetColumn(headerCategoria, 0);
            Grid.SetRow(headerCategoria, 0);
            gridCategoriasGeneros.Children.Add(headerCategoria);

            TextBlock headerGenero = new TextBlock
            {
                Text = "Géneros:",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(5),
                Foreground = AppTheme.Actual.TextoPrincipal,
                Padding = new Thickness(20, 20, 0, 0),
            };
            Grid.SetColumn(headerGenero, 1);
            Grid.SetRow(headerGenero, 0);
            gridCategoriasGeneros.Children.Add(headerGenero);

            //CALCULAR FILAS NECESARIAS
            int maxFilas = Math.Max(categorias.Count, generos.Count);

            for (int i = 0; i < maxFilas; i++)
            {
                gridCategoriasGeneros.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                if (i < categorias.Count)
                {
                    TextBlock txtCategoria = new TextBlock
                    {
                        Text = categorias[i],
                        Margin = new Thickness(5),
                        Padding = new Thickness(20, 0, 0, 0),
                        Foreground = AppTheme.Actual.TextoPrincipal
                    };
                    Grid.SetColumn(txtCategoria, 0);
                    Grid.SetRow(txtCategoria, i + 1); //+1 PORQUE LA FILA 0 ES PARA ENCABEZADO
                    gridCategoriasGeneros.Children.Add(txtCategoria);
                }

                if (i < generos.Count)
                {
                    TextBlock txtGenero = new TextBlock
                    {
                        Text = generos[i],
                        Margin = new Thickness(5),
                        Padding = new Thickness(20,0,0,0),
                        Foreground = AppTheme.Actual.TextoPrincipal
                    };
                    Grid.SetColumn(txtGenero, 1);
                    Grid.SetRow(txtGenero, i + 1); //+1 PORQUE LA FILA 0 ES PARA ENCABEZADO
                    gridCategoriasGeneros.Children.Add(txtGenero);
                }
            }


            //gridCategoriasGeneros.Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0));
        }


        //PARTE PARA LA IMAGEN PRINCIPAL
        private void CargarImagenPrincipal()
        {

            Image imagenPrincipal = new Image
            {
                Source = new BitmapImage(new Uri(imagenCabecera)),
                Stretch = Stretch.UniformToFill,
                Margin = new Thickness(0)
            };

            panelImagenPrincipalJuego.Children.Add(imagenPrincipal);
        }

        //PARTE PARA EL TEXTO DEL PRECIO
        private void CargarPrecio()
        {
            //Compruebo si el precio inicial está vacío o es igual a "0"
            if (string.IsNullOrEmpty(precioInicial) || precioInicial == "0")
            {
                textoPrecio.Text = "Free To Play";
            }
            else
            {
                //CONVERTO A DECIMAL PARA PODER HACER CALCULOS
                decimal precioInicialDecimal = Convert.ToDecimal(precioInicial);
                decimal descuentoDecimal = Convert.ToDecimal(descuento);

                //CARLCULAR EL PRECIO FINAL SIEMPRE Y CUANDO HAYA DESCUENTO
                decimal precioFinal = precioInicialDecimal;
                if (descuentoDecimal > 0)
                {
                    precioFinal = precioInicialDecimal * (1 - descuentoDecimal / 100);
                }

                //CONVIERTO EL PRECIO FINAL A EUROS
                textoPrecio.Text = (precioFinal / 100m).ToString("0.00") + " €";

                if (descuentoDecimal > 0)
                {
                    textoDescuento.Text = "-" + descuento.ToString() + "%";
                    textoDescuento.Padding = new Thickness(10);
                }
                    
                
            }

            textoPrecio.Foreground = AppTheme.Actual.TextoPrincipal;
            textoPrecio.FontSize = 22;
            textoPrecio.FontWeight = FontWeights.Bold;

            
            textoDescuento.Foreground = AppTheme.Actual.TextoPrecio;
            textoDescuento.Background = AppTheme.Actual.FondoDescuento;
            textoDescuento.FontSize = 22;
            textoDescuento.FontWeight = FontWeights.Bold;
        }




        //PARTE PARA EL BOTON DE COMPRA
        private void CargarBotonCompra()
        {
            string textoBoton;

            if (ventanaPrincipal.Usuario.BibliotecaJuegos.Contains(appid))
                textoBoton = "EN BIBLIOTECA";
            else if (string.IsNullOrEmpty(precioInicial) || precioInicial == "0")
                textoBoton = "AÑADIR A BIBLIOTECA";
            else
                textoBoton = "COMPRAR";

            botonCompra.Content = textoBoton;
            botonCompra.Height = 40;
        }

        private async void botonCompra_Click(object sender, RoutedEventArgs e)
        {
            int userId = ventanaPrincipal.Usuario.IdUsuario;
            int gameId = appid;

            bool exito = await ComprarJuegoAsync(userId, gameId);

            if (exito)
            {
                botonCompra.Tag = "biblioteca"; // SI ESTA EN BIBLIOTECA, SE PONE EL BOTON EN VERDE
            }
        }

        //METODO PARA COMPRAR UN JUEGO HACIENDO UNA PETICION POST AL SERVER
        private async Task<bool> ComprarJuegoAsync(int userId, int gameId)
        {
            try
            {
                string baseUrl = "http://" + Config.IP + ":"+Config.Puerto+"/store/buy";

                string url = $"{baseUrl}?user={userId}&game={gameId}";

                //HACER POST
                HttpResponseMessage response = await client.PostAsync(url, null);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();

                    //PARSING DEL DINERO NUEVO 
                    var resultado = Newtonsoft.Json.JsonConvert.DeserializeObject<RespuestaCompra>(jsonResponse);

                    if (resultado != null)
                    {
                        double nuevoDinero = resultado.DineroRestante;
                        ventanaPrincipal.Cabecera_top.Dinero = nuevoDinero;
                        notificacion.MostrarNotificacion(resultado.Mensaje, NotificationType.Success);

                        ventanaPrincipal.Usuario.CargarBiblioteca(ventanaPrincipal.Usuario.IdUsuario);
                        ventanaPrincipal.GuardarDatosLocal();
                        CargarBotonCompra();
                        botonCompra.Content = "EN BIBLIOTECA";

                        return true;
                    }

                    notificacion.MostrarNotificacion("Compra realizada con éxito.", NotificationType.Success);

                    //ventanaPrincipal.Usuario.CargarBiblioteca(ventanaPrincipal.Usuario.IdUsuario);
                    //ventanaPrincipal.GuardarDatosLocal();
                    CargarBotonCompra();

                    return true;
                }

                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();

                    try
                    {
                        var errorJson = JObject.Parse(errorResponse);
                        string mensajeError = errorJson["message"]?.ToString() ?? "Error desconocido.";
                        notificacion.MostrarNotificacion("Error: " + mensajeError, NotificationType.Error);
                    }
                    catch
                    {
                        // Por si la respuesta no es un JSON válido
                        notificacion.MostrarNotificacion("Error al comprar el juego.", NotificationType.Error);
                        MessageBox.Show("Error al comprar el juego.");
                    }
                    return false;
                }

                
            }
            catch (Exception ex)
            {
                notificacion.MostrarNotificacion($"Excepción al comprar: {ex.Message}", NotificationType.Error);
                return false;
            }
        }

        public class RespuestaCompra
        {
            [JsonProperty("dinero_restante")]
            public double DineroRestante { get; set; }

            [JsonProperty("message")]
            public string Mensaje { get; set; }

            [JsonProperty("success")]
            public bool Success { get; set; }
        }


        //PARTE PARA LA DESCRIPCCION CORTA
        private void CargarDescripccionCorta()
        {
            // Convertir los saltos de línea "<br>" a "\n" y las listas "<ul>" a un formato adecuado
            string formattedText = descripcionCorta.Replace("<br>", "\n").Replace("<ul class=\"bb_ul\">", "").Replace("</ul>", "").Replace("<li>", "   • ").Replace("</li>", "\n");

            // Mostrar el texto formateado en el TextBlock
            textDescripccionCorta.Text = formattedText;
            textDescripccionCorta.Foreground = AppTheme.Actual.TextoPrincipal;

        }

        //PARTE PARA LA FECHA DE LANZAMIENTO CORTA
        private void CargarFechaLanzamiento()
        {
            
            //MOSTRAR TEXTOS
            textLanzamiento.Text = "Fecha de lanzamiento: ";
            textLanzamiento.Foreground = AppTheme.Actual.TextoPrincipal;

            textFechaLanzamiento.Text += fechaLanzamiento;
            textFechaLanzamiento.Foreground = AppTheme.Actual.TextoPrincipal;

        }

        //PARTE PARA LOS DESARROLLADORES
        private void CargarDesarrolador()
        {

            //MOSTRAR TEXTOS
            textEtiquetaDesarrollador.Text = "Desarrollador: ";
            textEtiquetaDesarrollador.Foreground = AppTheme.Actual.TextoPrincipal;

            textDesarrollador.Text += desarrollador;
            textDesarrollador.Foreground = AppTheme.Actual.TextoPrincipal;

        }

        //PARTE PARA LOS EDITORES
        private void CargarEditor()
        {
            //MOSTRAR TEXTOS
            textEtiquetaEditor.Text = "Editor: ";
            textEtiquetaEditor.Foreground = AppTheme.Actual.TextoPrincipal;

            textEditor.Text += editores;
            textEditor.Foreground = AppTheme.Actual.TextoPrincipal;
        }



        //METODO PARA CARGAR LOS DATOS DEL JSON
        private async Task CargarDatosJson()
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    string json = await client.GetStringAsync("http://" + Config.IP + ":"+Config.Puerto+"/games/" + appid + "?full");
                    var response = JsonConvert.DeserializeObject<CarruselResponse>(json);

                    if (response?.juegos != null && response.juegos.Count > 0)
                    {
                        var juego = response.juegos[0];

                        //MINIATURAS
                        miniaturas = juego.capturas_miniatura ?? new List<string>();


                        //CATEGORÍAS
                        categorias = juego.categorias ?? new List<string>();

                        //GENEROS
                        generos = juego.generos ?? new List<string>();

                        //DESARROLLADORES
                        desarrollador = juego.desarrolladores != null ? string.Join(", ", juego.desarrolladores) : "";

                        //EDITORES
                        editores = juego.editores != null ? string.Join(", ", juego.editores) : "";

                        //DESCRIPCIONES
                        descripcionCorta = juego.descripcion_corta;
                        descripcionDetallada = juego.descripcion_detallada;

                        //FECHA DE LANZAMIENTO
                        fechaLanzamiento = juego.fecha_de_lanzamiento;

                        //IMAGEN DE CABECERA
                        imagenCabecera = juego.imagen_cabecera;

                        //NOMBRE
                        nombreJuego = juego.nombre;

                        //PRECIO
                        descuento = juego.precio?.descuento;
                        precioInicial = juego.precio?.precio_inicial;
                    }
                }
                catch (Exception ex)
                {
                    notificacion.MostrarNotificacion($"Error al cargar datos del juego: {ex.Message}", NotificationType.Warning);
                }
            }
        }

        public class Precio
        {
            public string descuento { get; set; }
            public string precio_inicial { get; set; }
        }

        public class Juego
        {
            public int app_id { get; set; }
            public List<string> capturas { get; set; }
            public List<string> capturas_miniatura { get; set; }
            public List<string> categorias { get; set; }
            public List<string> desarrolladores { get; set; }
            public string descripcion_corta { get; set; }
            public string descripcion_detallada { get; set; }
            public List<string> editores { get; set; }
            public bool f2p { get; set; }
            public string fecha_de_lanzamiento { get; set; }
            public List<string> generos { get; set; }
            public string imagen_cabecera { get; set; }
            public string imagen_capsula { get; set; }
            public string imagen_capsula_v5 { get; set; }
            public string nombre { get; set; }
            public int numero_de_ventas { get; set; }
            public string pagina_web { get; set; }
            public Precio precio { get; set; }
            public string tipo { get; set; }
        }

        public class CarruselResponse
        {
            public List<Juego> juegos { get; set; }
        }


    }
}

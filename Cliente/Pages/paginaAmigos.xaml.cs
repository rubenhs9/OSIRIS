using Cliente_TFG.Classes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static Cliente_TFG.Classes.Notificacion;
using static Cliente_TFG.Pages.paginaAmigos;
using static Cliente_TFG.Pages.paginaRecargaSaldo;
using SocketIOClient;
using System.Windows.Markup;

namespace Cliente_TFG.Pages
{
    /// <summary>
    /// Lógica de interacción para paginaAmigos.xaml
    /// </summary>
    public partial class paginaAmigos : Page
    {
        private MainWindow ventanaPrincipal;
        private string amigoActual = null;
        private Dictionary<string, List<MensajeChat>> historialChat = new Dictionary<string, List<MensajeChat>>();
        private List<SolicitudAmistad> solicitudesPendientes = new List<SolicitudAmistad>();
        private List<Amigo> listaDeAmigos = new List<Amigo>();
        private static readonly HttpClient client = new HttpClient();
        private Notificacion notificacion;
        private int idChatActual;
        private SocketIOClient.SocketIO cliente;

        private ClientWebSocket _webSocket = new ClientWebSocket();
        //private bool _isConnected = false;

        // Timer para actualizar solicitudes automáticamente
        private DispatcherTimer timerActualizacionSolicitudes;

        public paginaAmigos(MainWindow mainWindow)
        {
            InitializeComponent();
            ventanaPrincipal = mainWindow;
            notificacion = new Notificacion(panelNotificaciones);

            InicializarDatosReales();
            //InicializarDatosEjemplo();
            InicializarTimerActualizacion();
            AplicarTemaActual();

            // Actualizar la UI después de inicializar los datos
            ActualizarSolicitudesPendientes();
            _ = InicializarAsync();
        }

        private async Task InicializarAsync()
        {
            await ActualizarSolicitudesPendientesAsync();
            await ObtenerAmigosDelServidorAsync();
            await ConectarSocketIOAsync();
        }

        private async Task ConectarSocketIOAsync()
        {
            try
            {
                //notificacion.MostrarNotificacion("Creando cliente Socket.IO con transporte polling...", NotificationType.Info);

                var options = new SocketIOClient.SocketIOOptions
                {
                    Transport = SocketIOClient.Transport.TransportProtocol.Polling,
                    AutoUpgrade = false
                };
                cliente = new SocketIOClient.SocketIO($"http://{Config.IP}:{Config.Puerto}", options);

                

                cliente.OnDisconnected += (sender, reason) =>
                {
                    //notificacion.MostrarNotificacion($"❌ Desconectado de Socket.IO: {reason}", NotificationType.Warning);
                };

                // Escuchar eventos del servidor
                cliente.On("status", response =>
                {
                    String mensaje = Newtonsoft.Json.JsonConvert.SerializeObject(response.GetValue<MensajeDTO>());
                    //MessageBox.Show($"[Status]: {mensaje}");
                });

                cliente.On("error", response =>
                {
                    String error = Newtonsoft.Json.JsonConvert.SerializeObject(response.GetValue<MensajeDTO>());
                    //MessageBox.Show($"[Error servidor]: {error}");
                });

                cliente.On("nuevo_mensaje", response =>
                {

                    try
                    {
                        String rawJson = Newtonsoft.Json.JsonConvert.SerializeObject(response.GetValue<MensajeDTO>());
                        //MessageBox.Show($"[Mensaje bruto]: {rawJson}");

                        var jsonObject = JObject.Parse(rawJson);
                        String mensaje = (string)jsonObject["mensaje"];
                        String nombre_usuario = (string)jsonObject["nombre_usuario"];
                        String id_chat= (string)jsonObject["id_chat"];
                        //DateTime timestamp = (DateTime)jsonObject["mensaje"];
                        DateTime timestamp = DateTime.Now; //mejor que ponga la hora de cuando lo ha recibido

                        if (nombre_usuario == ventanaPrincipal.Usuario.nombre_usuario)
                            nombre_usuario = "Yo";
                        var mensajeRecibido = new MensajeChat(nombre_usuario, mensaje, timestamp);

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (idChatActual.ToString() == id_chat)
                            {
                                AgregarMensajeAChat(mensajeRecibido);
                                scrollMensajes.ScrollToEnd();
                            }
                        });
                    }
                    catch (Exception)
                    {
                        //MessageBox.Show($"❌ Error al procesar mensaje: {ex.Message}");
                    }
                });


                cliente.On("status", response =>
                {
                    if (response != null)
                    {
                        string msg = Newtonsoft.Json.JsonConvert.SerializeObject(response);
                        //MessageBox.Show($"✅ STATUS: {msg}");
                    }
                });

                cliente.On("error", response =>
                {
                    String error = Newtonsoft.Json.JsonConvert.SerializeObject(response);
                    //MessageBox.Show($"❌ ERROR: {error}");
                });

                //notificacion.MostrarNotificacion("Intentando conectar (polling)...", NotificationType.Info);
                await cliente.ConnectAsync();
            }
            catch (Exception ex)
            {
                notificacion.MostrarNotificacion($"Error al conectar con Socket.IO: {ex.Message}", NotificationType.Error);
            }
        }


        private async void EnviarMensaje()
        {
            string contenido = txtMensaje.Text.Trim();

            if (!string.IsNullOrEmpty(contenido) && !string.IsNullOrEmpty(amigoActual) && idChatActual != 0)
            {
                if (cliente.Connected)
                {
                    await cliente.EmitAsync("enviar_mensaje", new
                    {
                        id_usuario_remitente = ventanaPrincipal.Usuario.id_usuario,
                        id_chat = idChatActual, // Usar ID dinámico
                        mensaje = contenido
                    });
                    txtMensaje.Text = ""; // Limpiar campo
                }
                else
                {
                    notificacion.MostrarNotificacion("⚠️ No conectado al servidor", NotificationType.Error);
                }
            }
        }


        public class MensajeDTO
        {
            public int id_mensaje { get; set; }
            public int id_chat { get; set; }
            public string mensaje { get; set; }
            public string timestamp { get; set; }
            public int id_usuario_remitente { get; set; }
            public string nombre_usuario { get; set; }
        }

        //public async Task Conectar()
        //{
        //    cliente = new SocketIOClient.SocketIO("http://26.75.134.118:50000");

        //    cliente.OnConnected += async (sender, e) =>
        //    {
        //        MessageBox.Show("Conectado");
        //        await cliente.EmitAsync("join_chat", new { id_chat = 1, id_usuario = 5 });
        //    };

        //    await cliente.ConnectAsync();
        //}

        private void InicializarTimerActualizacion()
        {
            // Timer para actualizar solicitudes cada 5 segundos
            timerActualizacionSolicitudes = new DispatcherTimer();
            timerActualizacionSolicitudes.Interval = TimeSpan.FromSeconds(5);
            timerActualizacionSolicitudes.Tick += TimerActualizacionSolicitudes_Tick;
            timerActualizacionSolicitudes.Start();
        }

               private async void TimerActualizacionSolicitudes_Tick(object sender, EventArgs e)
        {
            await ActualizarSolicitudesPendientesAsync();
            await ObtenerAmigosDelServidorAsync();
        }

        private void InicializarDatosReales()
        {
            InicializarCodigoDeAmigoDeUsuarioActual();
        }

        private void InicializarCodigoDeAmigoDeUsuarioActual()
        {
            if (ventanaPrincipal.Usuario.codigo_amigo != null)
            {
                txtIdAmigoUsuarioActual.Text = ventanaPrincipal.Usuario.codigo_amigo;
            }
            else
            {
                txtIdAmigoUsuarioActual.Text = "No tienes codigo de amigo";
            }
        }

        private void AplicarTemaActual()
        {
            // Aplicar colores a los elementos principales
            btnEnviar.Background = AppTheme.Actual.FondoPanel;
            btnEnviar.Foreground = AppTheme.Actual.BordePanel;
        }

        private void InicializarDatosEjemplo()
        {
            // Inicializar solicitudes de amistad
            //solicitudesPendientes.Add(new SolicitudAmistad(123456, "Juan"));
            //solicitudesPendientes.Add(new SolicitudAmistad(123, "Antonia"));

            // Inicializar amigos
            listaDeAmigos.Add(new Amigo("Pepe", "100001", EstadoAmigo.Conectado, ""));
            listaDeAmigos.Add(new Amigo("Manolo", "100002", EstadoAmigo.Conectado, ""));
            listaDeAmigos.Add(new Amigo("Javi", "100003", EstadoAmigo.Conectado, ""));


            // Inicializar historial de chat
            //historialChat["Pepe"] = new List<MensajeChat>
            //{
            //    new MensajeChat { Remitente = "Pepe", Contenido = "Hola, ¿cómo estás?", Fecha = DateTime.Now.AddHours(-2) },
            //    new MensajeChat { Remitente = "Yo", Contenido = "¡Muy bien! ¿Y tú?", Fecha = DateTime.Now.AddHours(-1) }
            //};

            //historialChat["Manolo"] = new List<MensajeChat>
            //{
            //    new MensajeChat { Remitente = "Manolo", Contenido = "¿Jugamos una partida?", Fecha = DateTime.Now.AddDays(-1) }
            //};

            //historialChat["Javi"] = new List<MensajeChat>
            //{
            //    new MensajeChat { Remitente = "Javi", Contenido = "¿Has visto las nuevas ofertas?", Fecha = DateTime.Now.AddDays(-2) },
            //    new MensajeChat { Remitente = "Yo", Contenido = "Sí, están geniales", Fecha = DateTime.Now.AddDays(-2).AddMinutes(5) }
            //};
        }

        private void AceptarSolicitudAmistad(SolicitudAmistad solicitud)
        {
            try
            {
                // Eliminar la solicitud
                solicitudesPendientes.Remove(solicitud);

                Dispatcher.Invoke(() => AceptarSolicitudAmistadEnServidorAsync(solicitud));
                Dispatcher.Invoke(() => CrearGrupoChatEnServidorAsync(solicitud.IdUsuario));


                // Actualizar la UI
                ActualizarSolicitudesPendientes();

            }
            catch (Exception ex)
            {
                notificacion.MostrarNotificacion($"Error al aceptar solicitud: {ex.Message}", NotificationType.Error);
            }
        }

        private async Task CrearGrupoChatEnServidorAsync(string idAmigo)
        {
            string url = $"http://{Config.IP}:{Config.Puerto}/chats/crear_chat_privado";

            try
            {
                String bodyString = "{\r\n    \"id_usuario_1\": "+ventanaPrincipal.user.id_usuario+",\r\n    \"id_usuario_2\": "+idAmigo+"\r\n}";
                var content = new StringContent(bodyString, Encoding.UTF8, "application/json");
                // Hacer la solicitud
                HttpResponseMessage response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    //notificacion.MostrarNotificacion($"Se ha creado un chat con el usuario con id {idAmigo}", NotificationType.Info);
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    notificacion.MostrarNotificacion($"Error del servidor: {errorResponse} (Código: {response.StatusCode})", NotificationType.Error);
                }
            }
            catch (Exception ex)
            {
                notificacion.MostrarNotificacion($"Hubo un error intentando crear un chat con el usuario con id {idAmigo}: " + ex, NotificationType.Error);
                return;
            }
        }

        private async Task AceptarSolicitudAmistadEnServidorAsync(SolicitudAmistad Solicitud)
        {
            string url = $"http://{Config.IP}:{Config.Puerto}/friend_list/solicitudes_amistad/aceptar";

            try
            {
                String bodyString = "{\r\n   \"solicitud_id\": " + Solicitud.Id + "\r\n}";
                var content = new StringContent(bodyString, Encoding.UTF8, "application/json");
                // Hacer la solicitud
                HttpResponseMessage response = await client.PostAsync(url,content);

                if (response.IsSuccessStatusCode)
                {
                    notificacion.MostrarNotificacion($"Has aceptado la solicitud de {Solicitud.NombreUsuario}", NotificationType.Success);
                    ActualizarSolicitudesPendientes();
                    await ObtenerAmigosDelServidorAsync();
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    notificacion.MostrarNotificacion($"Error del servidor: {errorResponse} (Código: {response.StatusCode})", NotificationType.Error);
                }
            }
            catch (Exception ex)
            {
                notificacion.MostrarNotificacion("Hubo un error obteniendo solucitudes de amistad: " + ex, NotificationType.Error);
                return;
            }
        }

        private void RechazarSolicitudAmistad(SolicitudAmistad solicitud)
        {
            try
            {
                // Eliminar la solicitud
                solicitudesPendientes.Remove(solicitud);

                // Actualizar la UI
                ActualizarSolicitudesPendientes();

                Dispatcher.Invoke(() => RechazarSolicitudAmistadEnServidorAsync(solicitud));
            }
            catch (Exception ex)
            {
                notificacion.MostrarNotificacion($"Error al rechazar solicitud: {ex.Message}", NotificationType.Error);
            }
        }

        private async Task RechazarSolicitudAmistadEnServidorAsync(SolicitudAmistad solicitud)
        {
            string url = $"http://{Config.IP}:{Config.Puerto}/friend_list/solicitudes_amistad/rechazar";

            try
            {
                String bodyString = "{\r\n   \"solicitud_id\": " + solicitud.Id + "\r\n}";
                var content = new StringContent(bodyString, Encoding.UTF8, "application/json");
                // Hacer la solicitud
                HttpResponseMessage response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    notificacion.MostrarNotificacion($"Se ha rechazado con exito a {solicitud.NombreUsuario}", NotificationType.Warning);
                    ActualizarSolicitudesPendientes();
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    notificacion.MostrarNotificacion($"Error del servidor: {errorResponse} (Código: {response.StatusCode})", NotificationType.Error);
                }
            }
            catch (Exception ex)
            {
                notificacion.MostrarNotificacion("Hubo un error obteniendo solucitudes de amistad: " + ex, NotificationType.Error);
                return;
            }
        }

        private void ActualizarSolicitudesPendientes()
        {
            // Limpiar el panel de solicitudes
            contenedorSolicitudes.Children.Clear();

            // Actualizar el contador en el título
            textoSolicitudesTitulo.Text = $"Solicitudes de amigo ({solicitudesPendientes.Count})";

            // Si no hay solicitudes, mostrar mensaje
            if (solicitudesPendientes.Count == 0)
            {
                TextBlock mensajeVacio = new TextBlock
                {
                    Text = "No hay solicitudes pendientes",
                    Foreground = AppTheme.Actual.TextoSecundario,
                    FontStyle = FontStyles.Italic,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(10)
                };
                contenedorSolicitudes.Children.Add(mensajeVacio);
                return;
            }

            // Crear elementos para cada solicitud
            foreach (var solicitud in solicitudesPendientes)
            {
                CrearElementoSolicitud(solicitud);
            }
        }

        private void CrearElementoSolicitud(SolicitudAmistad solicitud)
        {
            // Crear el contenedor principal
            Border bordeSolicitud = new Border
            {
                Name = "id_"+solicitud.IdUsuario,
                Background = new SolidColorBrush(Color.FromRgb(37, 37, 37)), // #252525
                BorderBrush = AppTheme.Actual.BordePanel,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Margin = new Thickness(5),
                Tag = solicitud // Guardar referencia a la solicitud
            };

            Grid gridSolicitud = new Grid { Height = 40 };
            gridSolicitud.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            gridSolicitud.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            gridSolicitud.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Icono de usuario
            Border iconoUsuario = new Border
            {
                Width = 30,
                Height = 30,
                Background = Brushes.Transparent,
                BorderBrush = AppTheme.Actual.BordePanel,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(15),
                Margin = new Thickness(5,0,5, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            TextBlock iconoTexto = new TextBlock
            {
                Text = "👤",
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            iconoUsuario.Child = iconoTexto;
            Grid.SetColumn(iconoUsuario, 0);
            gridSolicitud.Children.Add(iconoUsuario);

            // Nombre de usuario
            TextBlock nombreUsuario = new TextBlock
            {
                Text = solicitud.NombreUsuario,
                Foreground = AppTheme.Actual.TextoPrincipal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5,0,5, 0)
            };
            Grid.SetColumn(nombreUsuario, 1);
            gridSolicitud.Children.Add(nombreUsuario);

            // Panel de botones
            StackPanel panelBotones = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 5, 0)
            };

            // Botón aceptar
            Button botonAceptar = new Button
            {
                Content = "✓",
                Width = 25,
                Height = 25,
                Margin = new Thickness(5,0,5, 0),
                Background = AppTheme.Actual.FondoPanel,
                BorderBrush = AppTheme.Actual.BordePanel,
                BorderThickness = new Thickness(1),
                Foreground = AppTheme.Actual.TextoPrincipal,
                Cursor = Cursors.Hand,
                Tag = solicitud,
            };
            botonAceptar.Click += (sender, e) =>
            {
                Button btn = sender as Button;
                if (btn?.Tag is SolicitudAmistad sol)
                {
                    AceptarSolicitudAmistad(sol);
                }
            };
            panelBotones.Children.Add(botonAceptar);

            // Botón rechazar
            Button botonRechazar = new Button
            {
                Content = "✕",
                Width = 25,
                Height = 25,
                Margin = new Thickness(5,0,5, 0),
                Background = AppTheme.Actual.FondoPanel,
                BorderBrush = AppTheme.Actual.BordePanel,
                BorderThickness = new Thickness(1),
                Foreground = AppTheme.Actual.TextoPrincipal,
                Cursor = Cursors.Hand,
                Tag = solicitud
            };
            botonRechazar.Click += (sender, e) =>
            {
                Button btn = sender as Button;
                if (btn?.Tag is SolicitudAmistad sol)
                {
                    RechazarSolicitudAmistad(sol);
                }
            };
            panelBotones.Children.Add(botonRechazar);

            Grid.SetColumn(panelBotones, 2);
            gridSolicitud.Children.Add(panelBotones);

            bordeSolicitud.Child = gridSolicitud;

            // Agregar animación de entrada
            AplicarAnimacionEntrada(bordeSolicitud);

            // Agregar al panel
            contenedorSolicitudes.Children.Add(bordeSolicitud);
        }

        private void AplicarAnimacionEntrada(UIElement elemento)
        {
            // Animación de fade in y slide
            elemento.Opacity = 0;
            elemento.RenderTransform = new TranslateTransform(0, -20);

            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            var slideIn = new DoubleAnimation
            {
                From = -20,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            elemento.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            ((TranslateTransform)elemento.RenderTransform).BeginAnimation(TranslateTransform.YProperty, slideIn);
        }

        // Método para actualizar solicitudes desde el servidor
        private async Task ActualizarSolicitudesPendientesAsync()
        {
            try
            {
                await ObtenerSolicitudesDelServidorAsync();
            }
            catch (Exception ex)
            {
                notificacion.MostrarNotificacion($"Error al actualizar solicitudes: {ex.Message}", NotificationType.Error);
            }
        }

        private async Task ObtenerSolicitudesDelServidorAsync()
        {
            string url = $"http://{Config.IP}:{Config.Puerto}/users/?id={ventanaPrincipal.Usuario.id_usuario}&solicitudes&estado_solicitudes=pendiente";

            try
            {

                // Hacer la solicitud
                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    //MostrarNotificacion(jsonResponse,NotificationType.Success);
                    var jsonObj = JObject.Parse(jsonResponse);
                    var recibidas = jsonObj["recibidas"];

                    foreach (var solicitud in recibidas)
                    {
                        int solicitud_id = (int)solicitud["id_solicitud"];
                        int solicitud_id_usuario = (int)solicitud["id_usuario"];
                        string solicitud_nombre_usuario = (string)solicitud["nombre_usuario"];

                        SolicitudAmistad nuevaSolicitud = (new SolicitudAmistad(solicitud_id, solicitud_id_usuario, solicitud_nombre_usuario));
                        if (!solicitudesPendientes.Exists(s => s.IdUsuario == nuevaSolicitud.IdUsuario))
                        {
                            solicitudesPendientes.Add(nuevaSolicitud);
                            ActualizarSolicitudesPendientes();
                            notificacion.MostrarNotificacion($"Nueva solicitud de amistad de {solicitud_nombre_usuario}", NotificationType.Info);
                        }
                        
                    }
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    notificacion.MostrarNotificacion($"Error del servidor: {errorResponse} (Código: {response.StatusCode})", NotificationType.Error);
                }
            }
            catch (Exception ex)
            {
                notificacion.MostrarNotificacion("Hubo un error obteniendo solucitudes de amistad: "+ ex, NotificationType.Error);
                return;
            }
        }

        private async Task ObtenerAmigosDelServidorAsync()
        {
            string url = $"http://{Config.IP}:{Config.Puerto}/users/?id={ventanaPrincipal.Usuario.id_usuario}&amigos";

            try
            {

                // Hacer la solicitud
                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    //notificacion.MostrarNotificacion("Leyendo amigos: "+jsonResponse, NotificationType.Success);
                    var amigos = JArray.Parse(jsonResponse);

                    foreach (var amigo in amigos)
                    {
                        string nombre = (string)amigo["nombre_usuario"];
                        string estadoString = (string)amigo["estado"];
                        estadoString = estadoString ?? "desconocido";
                        int id = (int)amigo["id_usuario"];
                        string foto = (string)amigo["foto_perfil"];
                        EstadoAmigo estado;

                        switch (estadoString.ToLower())
                        {
                            case "conectado":
                                estado = EstadoAmigo.Conectado;
                                break;
                            case "ausente":
                                estado = EstadoAmigo.Ausente;
                                break;
                            case "ocupado":
                                estado = EstadoAmigo.Ocupado;
                                break;
                            case "invisible":
                                estado = EstadoAmigo.Desconectado;
                                break;
                            case "desconectado":
                                estado = EstadoAmigo.Desconectado;
                                break;
                            default:
                                estado = EstadoAmigo.Desconectado;
                                break;
                        }

                        Amigo nuevoAmigo= (new Amigo(nombre,id.ToString(),estado,foto));
                        if (!listaDeAmigos.Exists(s => s.IdUsuario == nuevoAmigo.IdUsuario))
                        {
                            listaDeAmigos.Add(nuevoAmigo);
                            AgregarAmigoALista(nuevoAmigo);
                            //notificacion.MostrarNotificacion($"Se ha agregado el usuario {nuevoAmigo.NombreUsuario} a la lista de amigos", NotificationType.Info);
                        }
                        else if (listaDeAmigos.Exists(s => s.IdUsuario == nuevoAmigo.IdUsuario))
                        {
                            ActualizarEstadoDeLista(nuevoAmigo);
                        }

                    }
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    notificacion.MostrarNotificacion($"Error del servidor: {errorResponse} (Código: {response.StatusCode})", NotificationType.Error);
                }
            }
            catch (Exception ex)
            {
                notificacion.MostrarNotificacion("Hubo un error obteniendo la lista de amigos: " + ex, NotificationType.Error);
                return;
            }
        }

        private void ActualizarEstadoDeLista(Amigo nuevoAmigo)
        {
            Border border = ObtenerIndicadorEstado(nuevoAmigo.IdUsuario);
            border.Background = getColorEstado(nuevoAmigo.Estado);
        }

        // ESte es un metodo auxiliar para poder actualizar el estado de la lista
        private Border ObtenerIndicadorEstado(string idUsuario)
        {
            // Buscar el Border correspondiente al amigo en listaAmigos
            foreach (UIElement elemento in listaAmigos.Children)
            {
                if (elemento is Border bordeAmigo && bordeAmigo.Tag?.ToString() == idUsuario)
                {
                    // Obtener el Grid dentro del Border
                    if (bordeAmigo.Child is Grid gridAmigo)
                    {
                        // El indicadorEstado es el primer hijo del Grid
                        if (gridAmigo.Children.Count > 0 && gridAmigo.Children[0] is Border indicadorEstado)
                        {
                            return indicadorEstado;
                        }
                    }
                }
            }
            return null; // Retorna null si no se encuentra el indicador
        }

        private void AgregarSolicitudSimulada()
        {
            var nombresSimulados = new[] { "Carlos", "María", "Pedro", "Ana", "Luis", "Sofia" };
            var random = new Random();
            var nombre = nombresSimulados[random.Next(nombresSimulados.Length)];
            var iduser = random.Next(100000, 999999);
            var id = random.Next(100000, 999999);

            var nuevaSolicitud = new SolicitudAmistad(id,iduser, nombre);

            // Verificar que no exista ya
            if (!solicitudesPendientes.Exists(s => s.IdUsuario == nuevaSolicitud.IdUsuario))
            {
                solicitudesPendientes.Add(nuevaSolicitud);
                ActualizarSolicitudesPendientes();
                notificacion.MostrarNotificacion($"Nueva solicitud de amistad de {nombre}", NotificationType.Info);
            }
        }

        
        private void AgregarAmigoALista(Amigo amigo)

        {
            // Crear un nuevo elemento en la lista de amigos
            Border bordeAmigo = new Border
            {
                Background = AppTheme.Actual.FondoPanel,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Margin = new Thickness(5, 2, 5, 2)
            };

            bordeAmigo.MouseEnter += amigo_MouseEnter;
            bordeAmigo.MouseLeave += amigo_MouseLeave;
            bordeAmigo.MouseLeftButtonDown += amigo_MouseLeftButtonDown;
            bordeAmigo.Tag = amigo.IdUsuario;

            Grid gridAmigo = new Grid { Height = 40 };
            gridAmigo.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            gridAmigo.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Indicador de estado
            Border indicadorEstado = new Border
            {
                Width = 10,
                Height = 10,
                Background = getColorEstado(amigo.Estado),
                CornerRadius = new CornerRadius(5),
                Margin = new Thickness(5, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(indicadorEstado, 0);
            gridAmigo.Children.Add(indicadorEstado);

            // Nombre del amigo
            TextBlock textoNombre = new TextBlock
            {
                Tag = amigo.NombreUsuario,
                Text = amigo.NombreUsuario,
                Foreground = AppTheme.Actual.TextoPrincipal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0)
            };
            Grid.SetColumn(textoNombre, 1);
            gridAmigo.Children.Add(textoNombre);

            bordeAmigo.Child = gridAmigo;
            listaAmigos.Children.Add(bordeAmigo);

            // TODO - Si algo falla con los chats, es posible que sea esto, comprobarlo
            // Inicializar historial de chat vacío para el nuevo amigo
            if (!historialChat.ContainsKey(amigo.IdUsuario))
            {
                historialChat[amigo.IdUsuario] = new List<MensajeChat>();
            }

            // Aplicar animación de entrada
            AplicarAnimacionEntrada(bordeAmigo);
        }

        // Métodos existentes sin cambios...
        private void amigo_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = AppTheme.Actual.RatonEncima;
                border.Cursor = Cursors.Hand;
            }
        }

        private void amigo_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = AppTheme.Actual.FondoPanel;
                border.Cursor = Cursors.Arrow;
            }
        }

        private void amigo_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border)
            {
                string idAmigo = "";
                string nombreAmigo = "";

                // Obtener el nombre del amigo
                if (border.Tag != null)
                {
                    idAmigo = border.Tag.ToString();
                    nombreAmigo = ObtenerNombreAmigoDeUnBorder(border);
                }


                // Cambiar el amigo actual y cargar su historial de chat
                amigoActual = idAmigo;
                cabeceraAdvertencia.Visibility = Visibility.Collapsed;
                cabeceraNombreChat.Visibility = Visibility.Visible;
                gridMensajes.Visibility = Visibility.Visible;
                cabeceraNombreChat.Text = $"Chat con {nombreAmigo}";
                Dispatcher.Invoke(() => CargarHistorialChat(idAmigo));
                
            }
        }

        private string ObtenerNombreAmigoDeUnBorder(Border border)
        {
            // Obtener el Grid que es el hijo del Border
            Grid gridAmigo = border.Child as Grid;
            if (gridAmigo == null)
                return null; // O manejar el error según tu lógica

            // Buscar el TextBlock entre los hijos del Grid
            foreach (var child in gridAmigo.Children)
            {
                if (child is TextBlock textoNombre)
                {
                    return textoNombre.Text;
                }
            }

            return null; // O manejar el caso en que no se encuentre el TextBlock
        }

        private async Task CargarHistorialChat(string IdAmigo)
        {
            // Limpiar el área de mensajes
            areaMensajes.Children.Clear();

            await BuscarChatEnComunConAmigo(IdAmigo);
            await UnirseAlChat(idChatActual);
            await CargarHistorialChatDesdeElServidorAsync(IdAmigo);          

            // Hacer scroll hasta el último mensaje
            scrollMensajes.ScrollToEnd();
        }

        private async Task UnirseAlChat(int idChat)
        {
            if (cliente != null && cliente.Connected)
            {
                // Dejar sala anterior si existe
                if (idChatActual != 0)
                {
                    await cliente.EmitAsync("leave_chat", new
                    {
                        id_chat = idChatActual,
                        id_usuario = ventanaPrincipal.Usuario.id_usuario
                    });
                }

                // Unirse a la nueva sala
                await cliente.EmitAsync("join_chat", new
                {
                    id_chat = idChat,
                    id_usuario = ventanaPrincipal.Usuario.id_usuario
                });

                //notificacion.MostrarNotificacion($"Unido al chat del socket: {idChat}", NotificationType.Info);
            }
            else
            {
                notificacion.MostrarNotificacion("No conectado al servidor", NotificationType.Error);
            }
        }

        //TODO
        private async Task BuscarChatEnComunConAmigo(string idAmigo)
        {
            string url = $"http://{Config.IP}:{Config.Puerto}/chats/chat_en_comun?id_usuario_1={ventanaPrincipal.Usuario.id_usuario}&id_usuario_2={idAmigo}";

            try
            {
                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    var jsonObj = JObject.Parse(jsonResponse);
                    this.idChatActual = (int)jsonObj["chat_en_comun"];
                    //notificacion.MostrarNotificacion($"Se ha encontrado el chat con {idAmigo}: id chat {idChatActual}", NotificationType.Info);
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    notificacion.MostrarNotificacion($"Error del servidor: {errorResponse} (Código: {response.StatusCode})", NotificationType.Error);
                    // Si falla se crea el grupo y se busca de manera recursiva
                    await CrearGrupoChatEnServidorAsync(idAmigo);
                    await BuscarChatEnComunConAmigo(idAmigo);
                }
            }
            catch (Exception ex)
            {
                notificacion.MostrarNotificacion($"Hubo un error intentando buscar el chat con el usuario con id {idAmigo}: " + ex, NotificationType.Error);
                return;
            }
        }

        private async Task CargarHistorialChatDesdeElServidorAsync(string idAmigo)
        {
            string url = $"http://{Config.IP}:{Config.Puerto}/chats/mensajes_chat?id_chat={idChatActual}";

            try
            {
                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    //MessageBox.Show("Respuesta JSON: " + jsonResponse);
                    var jsonObject = JObject.Parse(jsonResponse);
                    var jsonMensajes = jsonObject["mensajes"] as JArray;
                    historialChat.Clear();
                    List<MensajeChat> listaDeMensajes = new List<MensajeChat>();
                    foreach (var mensaje in jsonMensajes)
                    {
                        string remitente = (string)mensaje["nombre_usuario"];
                        if (remitente == ventanaPrincipal.user.nombre_usuario)
                            remitente = "Yo";
                        string contenido = (string)mensaje["mensaje"];
                        DateTime timeStamp = (DateTime)mensaje["timestamp"];

                        listaDeMensajes.Add(new MensajeChat(remitente,contenido,timeStamp));
                    }
                    historialChat.Add(idAmigo,listaDeMensajes);

                    foreach (var mensaje in historialChat[idAmigo])
                    {
                        AgregarMensajeAChat(mensaje);
                    }
                    //notificacion.MostrarNotificacion($"Se ha cargado el historial de chat con {idAmigo}: id chat {idChatActual}", NotificationType.Info);
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    notificacion.MostrarNotificacion($"Error del servidor: {errorResponse} (Código: {response.StatusCode})", NotificationType.Error);

                }
            }
            catch (Exception ex)
            {
                notificacion.MostrarNotificacion($"Hubo un error cargando el historial de chat con el usuario con id {idAmigo}: " + ex, NotificationType.Error);
                return;
            }
        }

        private void AgregarMensajeAChat(MensajeChat mensaje)
        {
            // Crear el contenedor del mensaje
            Border bordeMensaje = new Border
            {
                Padding = new Thickness(10),
                Margin = new Thickness(5),
                CornerRadius = new CornerRadius(10),
                MaxWidth = 400
            };

            // Configurar el estilo según el remitente
            if (mensaje.Remitente == "Yo")
            {
                bordeMensaje.Background = AppTheme.Actual.BordePanel;
                bordeMensaje.HorizontalAlignment = HorizontalAlignment.Right;
            }
            else
            {
                bordeMensaje.Background = AppTheme.Actual.RatonEncima;
                bordeMensaje.HorizontalAlignment = HorizontalAlignment.Left;
            }

            // Crear el contenido del mensaje
            StackPanel contenidoMensaje = new StackPanel();

            // Texto del mensaje
            TextBlock textoMensaje = new TextBlock
            {
                Text = mensaje.Contenido,
                TextWrapping = TextWrapping.Wrap,
                Foreground = mensaje.Remitente == "Yo" ? AppTheme.Actual.FondoPanel : AppTheme.Actual.TextoPrincipal
            };
            contenidoMensaje.Children.Add(textoMensaje);

            // Hora del mensaje
            TextBlock textoHora = new TextBlock
            {
                Text = mensaje.Fecha.ToString("HH:mm"),
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Right,
                Foreground = mensaje.Remitente == "Yo" ? AppTheme.Actual.FondoPanel : AppTheme.Actual.TextoSecundario,
                Margin = new Thickness(0, 5, 0, 0)
            };
            contenidoMensaje.Children.Add(textoHora);

            bordeMensaje.Child = contenidoMensaje;
            areaMensajes.Children.Add(bordeMensaje);

            // Aplicar animación de fade in
            AplicarFadeIn(bordeMensaje);
        }

        private void AplicarFadeIn(UIElement elemento)
        {
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
            elemento.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }

        private void btnEnviar_Click(object sender, RoutedEventArgs e)
        {
            EnviarMensaje();
        }

        private void txtMensaje_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                EnviarMensaje();
            }
        }

        //private void EnviarMensaje()
        //{
        //    string contenido = txtMensaje.Text.Trim();

        //    if (!string.IsNullOrEmpty(contenido) && !string.IsNullOrEmpty(amigoActual))
        //    {
        //        // Crear nuevo mensaje
        //        MensajeChat nuevoMensaje = new MensajeChat("Yo",contenido,DateTime.Now);

        //        // Agregar al historial
        //        historialChat[amigoActual].Add(nuevoMensaje);

        //        // Mostrar en la UI
        //        AgregarMensajeAChat(nuevoMensaje);

        //        // Limpiar el campo de texto
        //        txtMensaje.Text = string.Empty;

        //        // Hacer scroll hasta el último mensaje
        //        scrollMensajes.ScrollToEnd();

        //        // Simular respuesta después de un tiempo aleatorio (solo para demostración)
        //        SimularRespuesta();
        //    }
        //}

       

        private void SimularRespuesta()
        {
            // Solo para demostración: simular una respuesta después de un tiempo aleatorio
            if (!string.IsNullOrEmpty(amigoActual))
            {
                Random rnd = new Random();
                int tiempoEspera = rnd.Next(1000, 3000); // Entre 1 y 3 segundos

                DispatcherTimer timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(tiempoEspera);
                timer.Tick += (s, e) =>
                {
                    // Detener el timer
                    timer.Stop();

                    // Crear respuesta simulada
                    string[] respuestas = new string[] {
                        "¡Genial!",
                        "¿Qué tal va todo?",
                        "¿Jugamos una partida?",
                        "Vale, hablamos luego",
                        "Estoy ocupado ahora mismo",
                        "¿Has visto las nuevas ofertas de la tienda?"
                    };

                    string respuesta = respuestas[rnd.Next(respuestas.Length)];
                    MensajeChat mensajeRespuesta = new MensajeChat(amigoActual, respuesta, DateTime.Now);

                    // Agregar al historial
                    historialChat[amigoActual].Add(mensajeRespuesta);

                    // Mostrar en la UI
                    AgregarMensajeAChat(mensajeRespuesta);

                    // Hacer scroll hasta el último mensaje
                    scrollMensajes.ScrollToEnd();
                };
                timer.Start();
            }
        }

        private void btnCopiarIdAmigo_Click(object sender, RoutedEventArgs e)
        {
            String output = txtIdAmigoUsuarioActual.Text;
            System.Windows.Clipboard.SetText(output);
            notificacion.MostrarNotificacion("Código de amigo copiado al portapapeles", NotificationType.Success);
        }

        private void btnEnviarSolicitud_Click(object sender, RoutedEventArgs e)
        {
            String friendCodeABuscar = txtBuscarAmigos.Text;
            if (String.IsNullOrEmpty(friendCodeABuscar))
            {
                notificacion.MostrarNotificacion("Tienes que rellenar el campo con el código de amigo", NotificationType.Warning);
                return;
            }
            SolicitudAmistad user = GetUsuarioPorCodigoDeAmigo(friendCodeABuscar);
            if (user == null)
            {
                notificacion.MostrarNotificacion($"No existe un usuario con código de amigo: {friendCodeABuscar}", NotificationType.Error);
                return;
            }
            MandarSolicitudDeAmistadAsync(user);
        }

        private async void MandarSolicitudDeAmistadAsync(SolicitudAmistad user)
        {
            string url = $"http://{Config.IP}:{Config.Puerto}/friend_list/solicitudes_amistad";
            try
            {
                // Validar inputs
                if (ventanaPrincipal.user == null || ventanaPrincipal.user.id_usuario <= 0)
                {
                    notificacion.MostrarNotificacion("Error: El usuario actual no está definido o tiene un ID inválido.", NotificationType.Error);
                    return;
                }
                if (user == null)
                {
                    notificacion.MostrarNotificacion("Error: El usuario destino no está definido o tiene un ID inválido.", NotificationType.Error);
                    return;
                }

                // Construir la cadena del body
                string jsonBody = $"{{\n  \"de_usuario_id\": {ventanaPrincipal.user.id_usuario},\n  \"para_usuario_id\": {user.IdUsuario}\n}}";
                //MessageBox.Show(jsonBody);
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                // Hacer la solicitud
                HttpResponseMessage response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    notificacion.MostrarNotificacion("Solicitud de amistad enviada correctamente.", NotificationType.Success);
                    txtBuscarAmigos.Text = ""; // Limpiar el campo
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    notificacion.MostrarNotificacion($"Error del servidor: {errorResponse} (Código: {response.StatusCode})", NotificationType.Error);
                }
            }
            catch (HttpRequestException ex)
            {
                notificacion.MostrarNotificacion($"Error de red: {ex.Message}", NotificationType.Error);
            }
            catch (Exception ex)
            {
                notificacion.MostrarNotificacion($"Error inesperado: {ex.Message}", NotificationType.Error);
            }
        }

        private SolicitudAmistad GetUsuarioPorCodigoDeAmigo(String friendCode)
        {
            try
            {
                if (string.IsNullOrEmpty(friendCode))
                {
                    return null;
                }
                var url = $"http://" + Config.IP + $":"+Config.Puerto + $"/users/friend_code/{friendCode}";
                using (var webClient = new WebClient())
                {
                    string jsonString;
                    try
                    {
                        jsonString = webClient.DownloadString(url);
                    }
                    catch (WebException)
                    {
                        // El mensaje de la excepcion ya salta en el siguiente metodo
                        return null;
                    }

                    var jsonObj = JObject.Parse(jsonString);
                    var usuarioJson = jsonObj["usuario"];

                    int id_usuario = (int)usuarioJson["id_usuario"];
                    string nombre_usuario = (string)usuarioJson["nombre_usuario"];
                    //la id de la solicitud no es relevante aqui
                    return new SolicitudAmistad(0,id_usuario, nombre_usuario);
                }
            }
            catch
            {
                return null;
            }
        }

        // Limpiar recursos al cerrar
        public void LimpiarRecursos()
        {
            timerActualizacionSolicitudes?.Stop();
            timerActualizacionSolicitudes = null;
        }

        // Clases auxiliares para manejar los datos (sin cambios)
        public class MensajeChat
        {
            public MensajeChat(string remitente, string contenido, DateTime fecha)
            {
                Remitente = remitente;
                Contenido = contenido;
                Fecha = fecha;
            }

            public string Remitente { get; set; }
            public string Contenido { get; set; }
            public DateTime Fecha { get; set; }
        }

        public class SolicitudAmistad
        {
            public SolicitudAmistad(int id,int id_usuario, string nombre_usuario)
            {
                Id = id.ToString();
                IdUsuario = id_usuario.ToString();
                NombreUsuario = nombre_usuario;
            }
            public string Id { get; set; }
            public string NombreUsuario { get; set; }
            public string IdUsuario { get; set; }
        }

        public class Amigo
        {
            public Amigo(string nombreUsuario, string idUsuario, EstadoAmigo estado, string foto)
            {
                NombreUsuario = nombreUsuario;
                IdUsuario = idUsuario;
                Estado = estado;
                Foto = foto;
            }

            public string NombreUsuario { get; set; }
            public string IdUsuario { get; set; }
            public EstadoAmigo Estado { get; set; }
            public string Foto { get; set; }
        }

        public enum EstadoAmigo
        {
            Conectado,
            Ausente,
            Ocupado,
            Desconectado
        }

        public SolidColorBrush getColorEstado(EstadoAmigo estado)
        {
            switch (estado)
            {
                case EstadoAmigo.Conectado:
                    return new SolidColorBrush(Color.FromRgb(76, 175, 80));
                case EstadoAmigo.Ausente:
                    return new SolidColorBrush(Color.FromRgb(243, 156, 18));
                case EstadoAmigo.Ocupado:
                    return new SolidColorBrush(Color.FromRgb(231, 76, 60));
                case EstadoAmigo.Desconectado:
                default:
                    return new SolidColorBrush(Color.FromRgb(127, 140, 141));
            }

        }
    }
}
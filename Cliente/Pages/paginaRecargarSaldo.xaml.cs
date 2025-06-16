using Cliente_TFG.Classes;
using Cliente_TFG.Windows;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Cliente_TFG.Pages
{
    public partial class paginaRecargaSaldo : Page
    {
        private MainWindow ventanaPrincipal;
        private decimal montoSeleccionado = 0;
        private static readonly HttpClient client = new HttpClient();
        public paginaRecargaSaldo(MainWindow mainWindow)
        {
            InitializeComponent();

            ventanaPrincipal = mainWindow;

            // CONFIGURAR EVENTOS DE RECARGA RAPIDA
            foreach (Button btn in gridRecargaRapida.Children.OfType<Button>())
            {
                btn.Click += RecargaRapida_Click;
                // Agregar efecto de animación
                btn.MouseEnter += (s, e) => AplicarEfectoHover(btn, true);
                btn.MouseLeave += (s, e) => AplicarEfectoHover(btn, false);
            }

            CargarMetodosPago();
            AplicarAnimacionEntrada();


        }

        public void RestaurarOpacidad()
        {
            panelPrincipal.Opacity = 1;
        }

        // METODO PARA CARGAR LOS METODOS DE PAGO
        private void CargarMetodosPago()
        {
            cmbMetodosPago.Items.Clear();

            var metodos = new[]
            {
                "💳 Tarjeta de Crédito/Débito",
                "🏦 Transferencia Bancaria",
                "📱 PayPal",
                "💰 Paysafecard",
            };

            foreach (var metodo in metodos)
            {
                cmbMetodosPago.Items.Add(metodo);
            }

            if (cmbMetodosPago.Items.Count > 0)
                cmbMetodosPago.SelectedIndex = 0;
        }

        // EVENTOS DE RECARGA RAPIDA
        private void RecargaRapida_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string montoStr)
            {
                if (decimal.TryParse(montoStr, out decimal monto))
                {
                    MostrarConfirmacionRecarga(monto);
                }
            }
        }

        // EVENTO DEL INPUT DE MONTO PERSONALIZADO
        private void txtMontoPersonalizado_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            var texto = textBox.Text;

            // Limpiar texto para permitir solo números y punto decimal
            var textoLimpio = new string(texto.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());

            // Reemplazar coma por punto para formato decimal
            textoLimpio = textoLimpio.Replace(',', '.');

            // Verificar que solo haya un punto decimal
            var puntos = textoLimpio.Count(c => c == '.');
            if (puntos > 1)
            {
                var indicePrimerPunto = textoLimpio.IndexOf('.');
                textoLimpio = textoLimpio.Substring(0, indicePrimerPunto + 1) +
                             textoLimpio.Substring(indicePrimerPunto + 1).Replace(".", "");
            }

            // Actualizar el texto si cambió
            if (texto != textoLimpio)
            {
                textBox.Text = textoLimpio;
                textBox.CaretIndex = textoLimpio.Length;
            }

            // Validar y mostrar información
            if (decimal.TryParse(textoLimpio, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal monto))
            {
                if (monto >= 1 && monto <= 10000)
                {
                    txtMontoInfo.Text = $"Cantidad a recargar: {monto:F2}€";
                    txtMontoInfo.Foreground = AppTheme.Actual.TextoPrincipal;
                    panelInfoMonto.Visibility = Visibility.Visible;
                    btnContinuarPersonalizado.IsEnabled = true;
                    montoSeleccionado = monto;

                    AplicarFadeIn(panelInfoMonto);
                }
                else if (monto > 10000)
                {
                    txtMontoInfo.Text = "⚠️ Cantidad máxima: 10,000€";
                    txtMontoInfo.Foreground = Brushes.Orange;
                    panelInfoMonto.Visibility = Visibility.Visible;
                    btnContinuarPersonalizado.IsEnabled = false;
                }
                else
                {
                    panelInfoMonto.Visibility = Visibility.Collapsed;
                    btnContinuarPersonalizado.IsEnabled = false;
                }
            }
            else
            {
                panelInfoMonto.Visibility = Visibility.Collapsed;
                btnContinuarPersonalizado.IsEnabled = false;
            }
        }

        // EVENTO DEL BOTON CONTINUAR PERSONALIZADO
        private void btnContinuarPersonalizado_Click(object sender, RoutedEventArgs e)
        {
            if (montoSeleccionado > 0)
            {
                MostrarConfirmacionRecarga(montoSeleccionado);
            }
        }

        // MOSTRAR PANEL DE CONFIRMACION
        private void MostrarConfirmacionRecarga(decimal monto)
        {
            montoSeleccionado = monto;
            txtMontoSeleccionado.Text = $"💰 {monto:F2}€";

            panelConfirmacionRecarga.Visibility = Visibility.Visible;
            AplicarFadeIn(panelConfirmacionRecarga);
        }

        // CONFIRMAR RECARGA
        private void btnConfirmarRecarga_Click(object sender, RoutedEventArgs e)
        {
            var metodoSeleccionado = cmbMetodosPago.SelectedItem?.ToString() ?? "Método no seleccionado";

            int userId = ventanaPrincipal.Usuario.IdUsuario;
            RecargarSaldoAsync(userId,metodoSeleccionado);

            CerrarConfirmacion();
            LimpiarFormulario();
        }

        private async void RecargarSaldoAsync(int userId,string metodoSeleccionado)
        {
            try
            {
                //Hago una copia del monto por que luego se resetea
                decimal monto= new decimal();
                monto = montoSeleccionado;

                string url = $"http://{Config.IP}:{Config.Puerto}/users/{userId}/recargar?cantidad={montoSeleccionado}";


                //HACER PUT
                HttpResponseMessage response = await client.PutAsync(url, null);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();

                    //PARSING DEL DINERO NUEVO 
                    var resultado = Newtonsoft.Json.JsonConvert.DeserializeObject<RespuestaRecargaSaldo>(jsonResponse);

                    if (resultado != null)
                    {
                        double nuevoDinero = resultado.DineroRestante;
                        ventanaPrincipal.Cabecera_top.Dinero = nuevoDinero;
                        //MessageBox.Show("Mensaje del servidor: "+ resultado.Mensaje);

                    }

                    var ventana = new VentanaRecarga($"{monto:F2}€", 
                                                    metodoSeleccionado, 
                                                    $"{ventanaPrincipal.Cabecera_top.Dinero:F2}€");
                    ventana.ShowDialog();

                    //MessageBox.Show(
                    //    $"✅ Recarga procesada exitosamente!\n\n" +
                    //    $"💰 Monto: {monto:F2}€\n" +
                    //    $"💳 Método: {metodoSeleccionado}\n\n" +
                    //    "El saldo se ha agregado a tu cuenta correctamente.\n"+
                    //    $"Saldo actual: {resultado.DineroRestante}$",
                    //    "Recarga Exitosa",
                    //    MessageBoxButton.OK,
                    //    MessageBoxImage.Information
                    //);
                }

                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Error al comprar el juego: {errorResponse}");
                }


            }
            catch (Exception ex)
            {
                MessageBox.Show($"Excepción al comprar: {ex.Message}");
            }
        }

        public class RespuestaRecargaSaldo
        {
            [JsonProperty("dinero_restante")]
            public double DineroRestante { get; set; }

            [JsonProperty("message")]
            public string Mensaje { get; set; }

            [JsonProperty("success")]
            public bool Success { get; set; }
        }
        

        // CANCELAR RECARGA
        private void btnCancelarRecarga_Click(object sender, RoutedEventArgs e)
        {
            CerrarConfirmacion();
        }

        // CERRAR PANEL DE CONFIRMACION
        private void CerrarConfirmacion()
        {
            AplicarFadeOut(panelConfirmacionRecarga, () =>
            {
                panelConfirmacionRecarga.Visibility = Visibility.Collapsed;
            });
        }

        // LIMPIAR FORMULARIO
        private void LimpiarFormulario()
        {
            txtMontoPersonalizado.Text = "";
            panelInfoMonto.Visibility = Visibility.Collapsed;
            btnContinuarPersonalizado.IsEnabled = false;
            montoSeleccionado = 0;
        }

        // EFECTO HOVER PARA BOTONES
        private void AplicarEfectoHover(Button boton, bool hover)
        {
            var escala = hover ? 1.05 : 1.0;
            var transform = boton.RenderTransform as ScaleTransform ?? new ScaleTransform();

            boton.RenderTransform = transform;
            boton.RenderTransformOrigin = new Point(0.5, 0.5);

            var animacion = new DoubleAnimation
            {
                To = escala,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            transform.BeginAnimation(ScaleTransform.ScaleXProperty, animacion);
            transform.BeginAnimation(ScaleTransform.ScaleYProperty, animacion);
        }

        // ANIMACION DE ENTRADA
        private void AplicarAnimacionEntrada()
        {
            // Buscar los contenedores principales por su tipo
            var secciones = panelPrincipal.Content as StackPanel;
            if (secciones != null)
            {
                var elementos = secciones.Children.OfType<Border>().ToArray();

                for (int i = 0; i < elementos.Length; i++)
                {
                    var elemento = elementos[i];
                    elemento.Opacity = 0;
                    elemento.RenderTransform = new TranslateTransform(0, 50);

                    var fadeIn = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromMilliseconds(800),
                        BeginTime = TimeSpan.FromMilliseconds(i * 300),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };

                    var slideIn = new DoubleAnimation
                    {
                        From = 50,
                        To = 0,
                        Duration = TimeSpan.FromMilliseconds(800),
                        BeginTime = TimeSpan.FromMilliseconds(i * 300),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };

                    elemento.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                    ((TranslateTransform)elemento.RenderTransform).BeginAnimation(TranslateTransform.YProperty, slideIn);
                }
            }
        }

        // ANIMACION FADE IN
        private void AplicarFadeIn(UIElement elemento)
        {
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            elemento.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }

        // ANIMACION FADE OUT
        private void AplicarFadeOut(UIElement elemento, Action callback = null)
        {
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            if (callback != null)
            {
                fadeOut.Completed += (s, e) => callback();
            }

            elemento.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }
    }
}

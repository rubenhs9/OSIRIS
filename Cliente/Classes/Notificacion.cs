using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Cliente_TFG.Pages.paginaAmigos;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows;

namespace Cliente_TFG.Classes
{
    internal class Notificacion
    {
        StackPanel panelNotificaciones;
        public Notificacion(StackPanel panelNotificaciones) { 
            this.panelNotificaciones = panelNotificaciones;
        }

        public void MostrarNotificacion(string mensaje, NotificationType tipo)
        {
            // Crear una notificación temporal en la UI
            var notificacion = new Border
            {
                Background = ObtenerColorNotificacion(tipo),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(10),
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top
            };

            var textoNotificacion = new TextBlock
            {
                Text = mensaje,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold
            };

            notificacion.Child = textoNotificacion;

            // Agregar a un panel de notificaciones
            panelNotificaciones.Children.Add(notificacion);

            // Remover después de 3 segundos
            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                panelNotificaciones.Children.Remove(notificacion);
            };
            timer.Start();
        }


        // Enum para tipos de notificación
        public enum NotificationType
        {
            Success,
            Warning,
            Error,
            Info
        }

        public Brush ObtenerColorNotificacion(NotificationType tipo)
        {
            switch (tipo)
            {
                case NotificationType.Success:
                    return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Verde
                case NotificationType.Warning:
                    return new SolidColorBrush(Color.FromRgb(255, 193, 7)); // Amarillo
                case NotificationType.Error:
                    return new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Rojo
                case NotificationType.Info:
                    return new SolidColorBrush(Color.FromRgb(33, 150, 243)); // Azul
                default:
                    return new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Gris
            }
        }
    }
}

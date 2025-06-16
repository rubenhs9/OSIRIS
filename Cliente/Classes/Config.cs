using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Windows;

namespace Cliente_TFG.Classes
{
    public static class Config
    {
        public static string Puerto { get; set; } = "50000";
        //public static string IP { get; set; } = "127.0.0.1";

        public static string IP { get; set; } = "26.84.183.227"; //JESUS PC
        //public static string IP { get; set; } = "26.254.119.213"; //JESUS LAPTOP

        //public static string IP { get; set; } = "26.75.134.118"; //RUBEN PC
        //public static string IP { get; set; } = "26.75.134.118"; //RUBEN LAPTOP

        //public static string IP { get; set; } = "26.72.223.97"; //NACHO

        static Config()
        {
            // Cargar la configuración al inicializar la clase
            LoadFromFile();
        }

        public static void LoadFromFile(string fileName = "config.json")
        {
            try
            {
                // Obtiene la ruta del directorio donde está el ejecutable
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string filePath = Path.Combine(basePath, fileName);

                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"El archivo {fileName} no se encuentra en {filePath}");
                }

                string jsonString = File.ReadAllText(filePath);
                var jsonConfig = JsonSerializer.Deserialize<JsonConfig>(jsonString);

                // Validar que los valores no sean nulos o vacíos
                if (string.IsNullOrWhiteSpace(jsonConfig.IP) || string.IsNullOrWhiteSpace(jsonConfig.Puerto))
                {
                    throw new InvalidOperationException("La configuración de IP o Puerto no es válida.");
                }

                // Asignar los valores a las propiedades estáticas
                IP = jsonConfig.IP;
                Puerto = jsonConfig.Puerto;
                //MessageBox.Show("config cargada con exito");
            }
            catch (Exception ex)
            {
                // Manejo de errores: usar valores por defecto
                MessageBox.Show($"Error al cargar la configuración: {ex.Message}");
                IP = "127.0.0.1";
                Puerto = "50000";
            }
        }

        // Clase auxiliar para deserializar el JSON
        private class JsonConfig
        {
            public string IP { get; set; }
            public string Puerto { get; set; }
        }
    
    }
}

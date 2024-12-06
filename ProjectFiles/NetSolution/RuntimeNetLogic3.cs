#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.DataLogger;
using FTOptix.NativeUI;
using FTOptix.Store;
using FTOptix.ODBCStore;
using FTOptix.InfluxDBStore;
using FTOptix.SerialPort;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.CommunicationDriver;
using FTOptix.Core;
using InfluxDB.Client.Writes;
using InfluxDB.Client;
using FTOptix.Modbus;
using FTOptix.RAEtherNetIP;
using FTOptix.MicroController;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;

using System.Diagnostics;
using System.Net;
using System.Collections.Generic;

#endregion

public class RuntimeNetLogic3 : BaseNetLogic
{

    private static readonly HttpClient client = new HttpClient();
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }


    [ExportMethod]
    public void AgregarElementosDinamicos()
    {
        // Encuentra el Accordion en la interfaz
        var accordion = Project.Current.Get<Accordion>("UI/Screens/Managment/Accordion1");
        if (accordion == null)
        {
            Log.Error("No se encontró el Accordion.");
            return;
        }

        // Conecta a la base de datos y obtiene las órdenes en estado "Inicio"
        var db = Project.Current.Get<Store>("DataStores/ODBCDatabase1");
        string query = "SELECT ID_OrdenProduccion FROM Ordenes_Produccion WHERE Estado_Orden = 'Inicio'";
        Object[,] resultSet;
        String[] header;

        // Ejecuta la consulta
        db.Query(query, out header, out resultSet);

        // Inicializa el margen superior para los elementos
        int topMargin = 10;

        // Recorre cada orden obtenida y crea los elementos dinámicos
        for (int i = 0; i < resultSet.GetLength(0); i++)
        {
            string orderId = resultSet[i, 0].ToString();

            // Crear Label para mostrar el ID de la orden
            var label = InformationModel.Make<Label>("label_dinamico_" + DateTime.Now.Ticks);
            label.Text = $"Orden: {orderId}";
            label.Width = 200;
            label.Height = 30;
            label.FontSize = 20;
            label.VerticalAlignment = VerticalAlignment.Top;
            label.HorizontalAlignment = HorizontalAlignment.Left;
            label.TopMargin = topMargin;

            // Crear Button para generar el QR
            var button = InformationModel.Make<Button>("boton_dinamico_" + DateTime.Now.Ticks);
            button.Text = "Generar QR";
            button.Width = 100;
            button.Height = 30;
            button.VerticalAlignment = VerticalAlignment.Top;
            button.HorizontalAlignment = HorizontalAlignment.Right;
            button.TopMargin = topMargin;

            // Asignar evento al botón




            // Crear un Panel y añadir el Label y el Button
            var panel = InformationModel.Make<Panel>("panel_dinamico_" + DateTime.Now.Ticks);
            panel.Width = 250;
            panel.HorizontalAlignment = HorizontalAlignment.Stretch;
            panel.Add(label);
            panel.Add(button);

            // Agregar el Panel al Accordion
            accordion.Content.Add(panel);

            // Incrementa el margen superior para el siguiente elemento
            topMargin += 30;
        }

        Log.Info("Elementos dinámicos agregados al Accordion.");
    }



    [ExportMethod]
    public void GenerarYGuardarQRConPNG()
    {
        var testvar = Project.Current.GetVariable("Model/test");
        string filePath = Project.Current.ProjectDirectory;
        Log.Info(filePath);

        try
        {
            // Conecta a la base de datos y obtiene las órdenes en estado "Inicio"
            var db = Project.Current.Get<Store>("DataStores/ODBCDatabase1");
            string queryGetOrders = "SELECT ID_OrdenProduccion FROM Ordenes_Produccion WHERE Estado_Orden = 'Inicio'";
            Object[,] resultSet;
            String[] header;

            // Ejecuta la consulta para obtener órdenes pendientes
            db.Query(queryGetOrders, out header, out resultSet);

            if (resultSet.GetLength(0) == 0)
            {
                Log.Info("No hay órdenes en estado 'Inicio'.");
                Project.Current.Get<Label>("UI/Screens/Managment/Label3").Visible = true;

                Thread.Sleep(10000);
                Project.Current.Get<Label>("UI/Screens/Managment/Label3").Visible = false;
                return;
            }

            // Itera sobre los resultados y genera un QR para cada orden
            for (int i = 0; i < resultSet.GetLength(0); i++)
            {
                string orderID = resultSet[i, 0]?.ToString();
                if (string.IsNullOrEmpty(orderID))
                    continue;

                // Generar un código QR único
                string qrCode = Guid.NewGuid().ToString();

                // Guardar el QR en la base de datos
                string queryUpdateOrden = $"UPDATE Ordenes_Produccion SET QR = '{qrCode}' WHERE ID_OrdenProduccion = {orderID}";
                string queryUpdateSkids = $"UPDATE Skids SET QR = '{qrCode}' WHERE ID_OrdenProduccion = {orderID}";
                string queryUpdateCOMPRA = $"UPDATE Ordenes_Compra SET QR = '{qrCode}' WHERE ID_OrdenProduccion = {orderID}";
                string queryUpdateEstado = $"UPDATE Ordenes_Produccion SET Estado_Orden = 'Etiqueta impresa' WHERE ID_OrdenProduccion = {orderID}";
                Log.Info(queryUpdateOrden);

                Object[,] ResultSet;
                String[] Header;

                db.Query(queryUpdateOrden, out Header, out ResultSet);
                db.Query(queryUpdateSkids, out Header, out ResultSet);
                db.Query(queryUpdateEstado, out Header, out ResultSet);

                // Leer el archivo PRN
                string prnFilePath = Path.Combine(filePath, "QRLABEL.prn");
                string zplContent = File.ReadAllText(prnFilePath);

                // Obtener la fecha actual en formato deseado
                string fechaActual = DateTime.Now.ToString("yyyy-MM-dd");

                // Realizar los reemplazos en el contenido del archivo PRN
                zplContent = zplContent
                    .Replace("EsteEsUnIdentificadorUnico$", qrCode) // Reemplaza el código QR
                    .Replace("QRCODEEXAMPLE$", qrCode)
                    .Replace("DESCRIPCION$", $"Orden numero: {orderID}") // Reemplaza la descripción
                    .Replace("FECHA$", fechaActual); // Reemplaza la fecha

                // Guardar el archivo PRN modificado usando el valor de QR en el nombre
                string modifiedPrnFilePath = Path.Combine(filePath, $"QRLABEL_{qrCode}.prn");
                File.WriteAllText(modifiedPrnFilePath, zplContent);
                Log.Info($"Archivo PRN modificado guardado como {modifiedPrnFilePath}");

                // Llamar a la API de Labelary para generar el PDF
                string pdfFilePath = Path.Combine(filePath, $"label_{qrCode}.pdf");

                using (var client = new HttpClient())
                {
                    var formData = new MultipartFormDataContent
                {
                    { new StringContent(zplContent), "file" }
                };

                    var request = new HttpRequestMessage(HttpMethod.Post, "http://api.labelary.com/v1/printers/8dpmm/labels/4x3/0/")
                    {
                        Content = formData
                    };
                    request.Headers.Add("Accept", "application/pdf");

                    // Enviar la solicitud y recibir la respuesta en formato PDF
                    HttpResponseMessage response = client.Send(request);

                    if (response.IsSuccessStatusCode)
                    {
                        // Guardar el PDF en el disco
                        using (var fileStream = new FileStream(pdfFilePath, FileMode.Create, FileAccess.Write))
                        {
                            response.Content.CopyToAsync(fileStream).Wait();
                        }

                        Log.Info($"PDF generado y guardado correctamente como {pdfFilePath}");

                        // Llamar a la función para imprimir pasando la ruta del archivo PRN generado
                        ImprimirEtiquetaZPL(modifiedPrnFilePath);
                    }
                    else
                    {
                        Log.Error("Error al generar el PDF: " + response.ReasonPhrase);
                    }
                }
            }
            
            
        }
        catch (Exception ex)
        {
            Log.Error("Error al procesar la impresión: " + ex.Message);
        }
    }



    private void ImprimirEtiquetaZPL(string archivoEtiquetaZPL)
    {
        try
        {
            // Nombre de la impresora en Windows (asegúrate de que esta impresora esté correctamente configurada)
            string impresora = "ZD421";  // Reemplázalo con el nombre real de la impresora
            string hostname = Dns.GetHostName(); // Obtiene el nombre del equipo local

            // Construir el comando COPY
            string comando = $"COPY \"{archivoEtiquetaZPL}\" \\\\{hostname}\\{impresora}";

            // Mostrar el comando en el log para verificación
            Log.Info($"Comando a ejecutar: {comando}");

            // Ejecutar el comando usando ProcessStartInfo
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe", // Usar cmd.exe para ejecutar el comando COPY
                Arguments = "/C " + comando, // El parámetro /C ejecuta el comando y luego cierra cmd
                CreateNoWindow = true, // No mostrar la ventana de cmd
                UseShellExecute = false, // No usar la shell
                RedirectStandardOutput = true, // Redirigir salida estándar (si es necesario)
                RedirectStandardError = true // Redirigir errores (si es necesario)
            };

            // Ejecutar el proceso
            Process proceso = Process.Start(startInfo);
            proceso.WaitForExit(); // Esperar a que termine el proceso

            // Verificar el estado de salida del proceso
            if (proceso.ExitCode == 0)
            {
                Log.Info("Etiqueta debe ser impresa correctamente.");
            }
            else
            {
                Log.Error("Error al intentar imprimir la etiqueta. Revisa la configuración de la impresora.");
            }
        }
        catch (Exception ex)
        {
            Log.Error("Error al procesar la impresión: " + ex.Message);
        }



    }

    [ExportMethod]
    public void cheackEtiqueta(string qr)
    {
        try
        {
            // Conectar a la base de datos
            Log.Info("PASO1");
            var db = Project.Current.Get<Store>("DataStores/ODBCDatabase1");
            Log.Info("PASO2");
            // Consulta para verificar si el QR existe
            string querySelect = $"SELECT * FROM Ordenes_Produccion WHERE Estado_Orden = 'Etiqueta impresa'";
            Object[,] resultSet;
            String[] header;

            // Ejecutar la consulta
            db.Query(querySelect, out header, out resultSet);
            Log.Info("PASO3");
            // Verificar si se encontró un resultado
            if (resultSet.GetLength(0) > 0)
            {
                Log.Info("PASO4");
                // Si existe, actualizar el estado de la orden
                string queryUpdate = $"UPDATE Ordenes_Produccion SET Estado_Orden = 'En proceso' WHERE QR = '{qr}'";
                db.Query(queryUpdate, out header, out resultSet);
                Log.Info("PASO5");
                Log.Info($"Orden actualizada a 'En proceso'");

            }
            else
            {
                // Si no se encuentra, registrar un mensaje
                Log.Info("El código QR proporcionado no corresponde a ninguna orden en la base de datos.");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error al verificar o actualizar la orden: {ex.Message}");
        }
    }




}

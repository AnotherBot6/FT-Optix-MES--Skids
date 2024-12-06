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
using System.Threading;
using InfluxDB.Client.Writes;
using InfluxDB.Client;
using FTOptix.RAEtherNetIP;
#endregion

public class BateriaPanelMore : BaseNetLogic
{
    public override void Start()
    {
       
        // Insert code to be executed when the user-defined logic is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }
    [ExportMethod]
    public void modBateria()
    {
        var botonS = Project.Current.GetVariable("Model/ViewMorePanels/Folder1/check1");
        var temp1 = Project.Current.GetVariable("Model/ViewMorePanels/Folder1/temperatura");
        var temp2 = Project.Current.GetVariable("Model/ViewMorePanels/Folder1/capacidad");
        var temp3 = Project.Current.GetVariable("Model/ViewMorePanels/Folder1/densidad");
        var temp4 = Project.Current.GetVariable("Model/ViewMorePanels/Folder1/volts");
        var temp5 = Project.Current.GetVariable("Model/ViewMorePanels/Folder1/x");
        var temp6 = Project.Current.GetVariable("Model/ViewMorePanels/Folder1/y");
        var temp7 = Project.Current.GetVariable("Model/ViewMorePanels/Folder1/z");


        while (botonS.Value == true)
        {

            Log.Info("holaa");
            
            Random random = new Random();

            // Generar datos aleatorios
            float temperatura = random.Next(28, 36);
            float capacidad = random.Next(82, 90);
            float densidadEnergetica = random.Next(90, 100);
            float voltaje = random.Next(350, 400);
            int x = random.Next(1195, 1200);
            int y = random.Next(795, 800);
            int z = random.Next(95, 100);

            temp1.Value = temperatura;
            temp2.Value = capacidad;
            temp3.Value = densidadEnergetica;
            temp4.Value = voltaje;
            temp5.Value = x;
            temp6.Value = y;
            temp7.Value = z;


            Thread.Sleep(4000);
        }


    }
    [ExportMethod]
    public void Task()
    {
        Random random = new Random();
        Log.Info("paso1");
        // Configura el cliente de InfluxDB con la URL y el token de autenticación
        string url = "http://localhost:8086"; // Cambia esto por tu URL de InfluxDB
        string atoken = "01cATLNCIDhtucmCjGXvRzaPUScNDNGMNFgfF6pnPTnbRAT0R2L4XcuUDRG36ZHxI5fR9KJ5ToNjC2-RJam3Kw=="; // Configura tu token aquí
        Log.Info("paso2");
        using var client = new InfluxDBClient(url, token: atoken);
        const string buckett = "Battery";
        const string orgg = "RA";

       
        Log.Info("paso3");
        int i = 0;
        var points = new[]{
            "OK",
            "FAIL"
        };
        var writeApi = client.GetWriteApiAsync();
        while (i < 10)
        {
            // Genera valores aleatorios para el voltaje e ID
            double voltage = random.NextDouble() * (4.2 - 3.0) + 3.0; // Voltaje entre 3.0 y 4.2
            int Id = random.Next(1, 4); // ID entre 1 y 3
            int estado = random.Next(0,1);

            // Crea el punto de datos para InfluxDB
            var pointe = PointData
                .Measurement("Voltaje")
                .Tag("Id bateria", Id.ToString())
                .Field(voltage.ToString(), points[estado]);

            // Escribe el punto en InfluxDB
            writeApi.WritePointAsync(point: pointe, bucket: buckett, org: orgg);
            Log.Info("Paso4");
            // Pausa de 1 segundo antes de enviar el siguiente punt

            i++;
        }
    }

}

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
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.Core;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using FTOptix.SerialPort;
using FTOptix.CommunicationDriver;
using Barcoder;
using Barcoder.Qr;
using Barcoder.Renderer;
using Barcoder.Renderer.Image;
using FTOptix.OPCUAServer;
using System.IO;
using Newtonsoft.Json.Linq;
using FTOptix.Modbus;
using FTOptix.RAEtherNetIP;
using FTOptix.MicroController;

#endregion

public class LogQR : BaseNetLogic
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
    public void ManageStage(string skidId, string stage)
    {
        var panel = Project.Current.Get<Rectangle>("UI/Screens/ControlPanel/Image1/LogQR/Rectangle1/Rectangle1");
        var Ordenes_Produccion = Project.Current.Get<DataGrid>("UI/Screens/ControlPanel/Image1/LogQR/Rectangle1/Rectangle1/Ordenes_Produccion");
        var BatteryStage_1 = Project.Current.Get<DataGrid>("UI/Screens/ControlPanel/Image1/LogQR/Rectangle1/Rectangle1/BatteryStage_1"); // Cambia "DataGridPath" a la ruta real de tu DataGrid
        var variable = Project.Current.GetVariable("Model/query_1");
        var variable2 = Project.Current.GetVariable("Model/query_2");
        if (string.IsNullOrEmpty(skidId) || string.IsNullOrEmpty(stage))
            return;

        try
        {
            string query;
            string query2;
            panel.Visible = true;
            panel.Enabled = true;
            panel.Opacity = 100;
            // Definir el query según la etapa
            var db = Project.Current.Get<Store>("DataStores/ODBCDatabase1");

            string queryGetOrders = $"SELECT ID_Skid FROM Ordenes_Produccion WHERE QR = '{skidId}'";
            Object[,] resultSet;
            String[] header;

            // Ejecuta la consulta para obtener órdenes pendientes
            db.Query(queryGetOrders, out header, out resultSet);
            string X = resultSet[0,0].ToString();
            Log.Info(X);
            switch (stage)
            {
                case"Orden":

                    break;

                case "Chasis":
                    query = $"SELECT * FROM Skids WHERE ID_Skid = '{X}'";
                    break;
                case "Bateria":
                    query = $"SELECT * FROM Componentes WHERE Componentes.ID_Skid = '{X}'";
                    query2 = $"SELECT * FROM Ordenes_Produccion WHERE Ordenes_Produccion.ID_Skid = '{X}'";
                    variable.Value = query;
                    variable2.Value = query2;

                    BatteryStage_1.Enabled = true;
                    BatteryStage_1.Visible = true;
                    BatteryStage_1.Opacity = 100;

                    Ordenes_Produccion.Enabled = true;
                    Ordenes_Produccion.Visible = true;
                    Ordenes_Produccion.Opacity = 100; 

                    break;
                case "Suspension":
                    query = $"SELECT * FROM Pruebas WHERE ID_Skid = '{skidId}'";
                    break;
                case "Llantas":
                    query = $"SELECT * FROM Embalaje WHERE ID_Skid = '{skidId}'";
                    break;
                case "Produccion":
                    query = $"SELECT * FROM Ordenes_Produccion WHERE ID_Skid = '{skidId}'";
                    break;
                default:
                    query = string.Empty;
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Error("Error al procesar la etapa: " + ex.Message);
        }
    }
    [ExportMethod]
    public void updateStage(string stage)
    {
        var stg = Project.Current.GetVariable("Model/Stage");
        stg.Value = stage;
    }

    

}

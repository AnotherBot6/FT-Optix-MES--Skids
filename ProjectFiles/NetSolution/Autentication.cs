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
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.Core;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using FTOptix.InfluxDBStore;
using FTOptix.SerialPort;
using FTOptix.CommunicationDriver;
using FTOptix.Modbus;
using FTOptix.RAEtherNetIP;
using FTOptix.MicroController;
#endregion

public class Autentication : BaseNetLogic
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
    public void activarAut() {
        var aut = Project.Current.GetVariable("Model/Autenticado");
        Log.Info(aut.Value.ToString());
    }
}

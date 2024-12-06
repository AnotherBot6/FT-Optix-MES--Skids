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
using FTOptix.InfluxDBStore;
using FTOptix.SerialPort;
using FTOptix.CommunicationDriver;
using FTOptix.Modbus;
using FTOptix.RAEtherNetIP;
using FTOptix.MicroController;
#endregion

public class panelsView : BaseNetLogic
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

    public void OpenPanel(string panelSiguiente,  string panelActual) {
        var aut = Project.Current.GetVariable("Model/Autenticado");
        var panelmain = Project.Current.Get<Panel>("UI/Screens/ControlPanel");
        if (aut.Value == true)
        {
            var panel1 = Project.Current.Get<Panel>(panelSiguiente);
            panel1.Enabled = true;
            panel1.Visible = true;
            panel1.Opacity = 100;
            Log.Info("paso");


            if (panelActual != "a") {
                var panel2 = Project.Current.Get<Panel>(panelActual);
                panel2.Enabled = false;
                panel2.Visible = false;
                panel2.Opacity = 0;
            }

        }
        else
        {

            Log.Error("no autenticado");
        }
    }
    [ExportMethod]

    public void ClosePanel(string panelActual,string panelAnterior) {

        var aut = Project.Current.GetVariable("Model/Autenticado");
        if (aut.Value == true)
        {
            var panel1 = Project.Current.Get<Panel>(panelActual);
            panel1.Enabled = false;
            panel1.Visible = false;
            panel1.Opacity = 0;

            if (panelAnterior != "a") {
                var panel2 = Project.Current.Get<Panel>(panelAnterior);
                panel2.Enabled = true;
                panel2.Visible = true;
                panel2.Opacity = 100;
            }
            
        }
        else
        {
            Log.Error("no autenticado");
        }

    }

}

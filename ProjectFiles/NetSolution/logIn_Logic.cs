#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.Retentivity;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.CoreBase;
using FTOptix.Core;
using FTOptix.NetLogic;
using FTOptix.Store;
using FTOptix.ODBCStore;
using FTOptix.DataLogger;
using FTOptix.InfluxDBStore;
using FTOptix.SerialPort;
using FTOptix.CommunicationDriver;
using FTOptix.Modbus;
using FTOptix.RAEtherNetIP;
using FTOptix.MicroController;
using System.Threading;
#endregion

public class logIn_Logic : BaseNetLogic
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

    public void BuscarUsuarioPorRFIDpASSWORD(string inputId, string passW)
    {
        var Autenticado = Project.Current.GetVariable("Model/Autenticado");
        var Rol = Project.Current.GetVariable("Model/LogIn/Rol");
        var user = Project.Current.GetVariable("Model/LogIn/Nombre_Usuario");
        var info = Project.Current.Get<Rectangle>("UI/Screens/Login_page/Panel1/Image1/Image2/Cuadro info");
        var error = Project.Current.Get<Rectangle>("UI/Screens/Login_page/Panel1/Image1/Image2/Cuadro error");
        var Logout = Project.Current.Get<Button>("UI/Screens/Login_page/Panel1/Button2");
        try
        {
            // Consulta SQL para buscar el usuario con el RFID ingresado
            string query = $"SELECT * FROM Usuarios WHERE Nombre_Usuario = '{inputId}'";
            // Crear conexión a la base de datos (asegúrate de configurar correctamente tu ODBCStore)
            var db = Project.Current.Get<Store>("DataStores/ODBCDatabase1"); // Cambiar "ODBCStore" al nombre correcto de tu conexión ODBC
            // Definir variables para almacenar los encabezados de la consulta y los resultados
            Object[,] ResultSet;
            String[] Header;

            // Ejecutar la consulta
            db.Query(query, out Header, out ResultSet);

            

            if (ResultSet != null && ResultSet.GetLength(0) > 0)
            {
                // Si el RFID fue encontrado, encender el LED
                int count = Convert.ToInt32(ResultSet[0, 0]);
                string dataBPass = ResultSet[0, 3].ToString();
                string rol = ResultSet[0, 4].ToString();

                if (count > 0 && passW == dataBPass)
                {
                    
                    string nombreUsuario = ResultSet[0, 1].ToString();
                    // Mostrar el nombre del usuario en el cuadro de texto
                    
                    user.Value = nombreUsuario;
                   
                    Autenticado.Value = true;
                    Rol.Value = rol;
                    Log.Info(Autenticado.Value.ToString());
                    Log.Info($"{Rol.Value}");
                    info.Visible = true;
                    error.Visible = false;
                    Logout.Visible = true;
                    Logout.Enabled = true;
                    Logout.Opacity = 100;
                }
                else
                {
                    
                    Logout.Visible = false;
                    Logout.Enabled = false;
                    Logout.Opacity = 0;
                    
                    Autenticado.Value = false;
                    Rol.Value = "";
                    user.Value = "";
                    info.Visible = false;
                    error.Visible = true;
                    Log.Info(Autenticado.Value.ToString());
                    Thread.Sleep(5000);
                    error.Visible = false;
                }
            }
            else
            {
                
                Autenticado.Value = false;
                Rol.Value = "";
                user.Value = "";
                Log.Info(Autenticado.Value.ToString());
                Logout.Visible = false;
                Logout.Enabled = false;
                Logout.Opacity = 0;
                info.Visible = false;
                error.Visible = true;
                Thread.Sleep(5000);
                error.Visible = false;

            }

        }
        catch (Exception ex)
        {
            Log.Error("Error al buscar el RFID: " + ex.Message);
            Autenticado.Value = false;
            Rol.Value = "";
            Log.Info(Autenticado.Value.ToString());
        }
    }
    [ExportMethod]
    public void logOut() 
    {
        var Autenticado = Project.Current.GetVariable("Model/Autenticado");
        var Rol = Project.Current.GetVariable("Model/LogIn/Rol");
        var Logout = Project.Current.Get<Button>("UI/Screens/Login_page/Panel1/Button2");
        var Box = Project.Current.Get<TextBox>("UI/Screens/Login_page/Panel1/Image1/Image2/TextBoxRFID");
        
        
        var tag = Project.Current.Get<SerialPort>("CommDrivers/SerialPort1");
        var user = Project.Current.GetVariable("Model/LogIn/Nombre_Usuario");
        var info = Project.Current.Get<Rectangle>("UI/Screens/Login_page/Panel1/Image1/Image2/Cuadro info");
        var error = Project.Current.Get<Rectangle>("UI/Screens/Login_page/Panel1/Image1/Image2/Cuadro error");
        Autenticado.Value = false;
        Rol.Value = "";
        info.Visible = false;
        error.Visible = false;

        
        user.Value = "";

        

        Logout.Visible = false;
        Logout.Enabled = false;
        Logout.Opacity = 0;

        Box.Text.Remove(0);
        


        var l = tag.Get<IUAVariable>("tag1");
        l.Value = "0x00";
        Log.Info($"{l.Value} , {Autenticado.Value}, {Rol.Value}");



    }
}

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
using FTOptix.SerialPort;
using FTOptix.CommunicationDriver;
using System.Text.RegularExpressions;
using FTOptix.Modbus;
using FTOptix.RAEtherNetIP;
using FTOptix.MicroController;
using System.Threading;
#endregion

public class RuntimeNetLogic1 : BaseNetLogic
{
    private SerialPort serialPort;
    private PeriodicTask periodicTask;
    private IUAVariable tag;
    private IEventObserver observer;
    private IEventRegistration registration;
    private const string readCommand = "rfid:qid.id.hold\r";
    private const string responsePattern = @"{.*?,.*?,.*?;([0x0-9A-Fa-f]+)}";
    private bool rfidScanner;
    
    public override void Start()
    {
        
        if (Owner is SerialPort port)
        {
            serialPort = port;
            serialPort.Baudrate = 9600;
        }
        else
        {
            Log.Error("El objeto Owner no es un SerialPort.");
            return;
        }

        tag = serialPort.Get<IUAVariable>("tag1");

        observer = new CallbackVariableChangeObserver(OnCommunicationStatusVariableValueChanged);
        registration = serialPort.CommunicationStatusVariable.RegisterEventObserver(observer, EventType.VariableValueChanged);

        periodicTask = new PeriodicTask(Read, 500, Owner);
        periodicTask.Start();
    }

    private void Read()
    {
        try
        {
            ReadImpl();
        }
        catch (Exception ex)
        {
           // Log.Error("Error in Read method: " + ex.Message);
        }
    }
    private byte[] Serialize()
    {
        // Aquí puedes definir el comando específico que deseas enviar al lector RFID
        // Por ejemplo, si el comando es "READ" en ASCII
        string command = "rfid:qid.id.hold\r"; // Reemplaza esto con el comando que necesitas
        byte[] commandBytes = System.Text.Encoding.ASCII.GetBytes(command);
        return commandBytes; // Retorna el comando como un array de bytes
    }
    private void ReadImpl()
    {
        // Enviar el comando al dispositivo
        byte[] command = Serialize(); // Método para serializar tu comando
        serialPort.WriteBytes(command);

        // Esperar un momento para permitir que el dispositivo responda
        System.Threading.Thread.Sleep(500); // Ajusta el tiempo según sea necesario

        // Leer los bytes del puerto serie
        var result = serialPort.ReadBytesUntil("}"); // Ajusta el número de bytes según sea necesario
        
        // Verificar la respuesta
        if (result != null && result.Length > 0)
        {
            // Convertir los bytes a una cadena
            string response = System.Text.Encoding.ASCII.GetString(result);

            // Usar expresión regular para encontrar el ID
            // Usar expresión regular para encontrar el ID
            var match = Regex.Match(response, responsePattern);
            if (match.Success)
            {
                // Obtener el ID desde la captura del regex (grupo 1)
                string id = match.Groups[1].Value;

                // Mostrar el ID y guardarlo en tag
                
                //Log.Info("RFID ID: " + id);

                if (!(id.Equals("0x00", StringComparison.OrdinalIgnoreCase)))
                {
                    tag.Value = id;
                    rfidScanner = true;
                    Log.Info("RFID ID: " + id);
                    BuscarUsuarioPorRFID(id.ToString());
                }
                    
            }

            else
            {
                Log.Error("Response: " + response);
                rfidScanner = false;
            }
        }
        else
        {
            Log.Error("No data received from the device.");
            rfidScanner = false;
        }
    }


    private void OnCommunicationStatusVariableValueChanged(IUAVariable variable, UAValue newValue, UAValue oldValue, uint[] indexes, ulong senderId)
    {
        Log.Info("Communication status changed: " + newValue.Value.ToString());
    }


    private void BuscarUsuarioPorRFID(string inputId)
    {
        var Autenticado = Project.Current.GetVariable("Model/Autenticado");
        var Rol = Project.Current.GetVariable("Model/LogIn/Rol");
        var user = Project.Current.GetVariable("Model/LogIn/Nombre_Usuario");
        var Logout = Project.Current.Get<Button>("UI/Screens/Login_page/Panel1/Button2");
        var info = Project.Current.Get<Rectangle>("UI/Screens/Login_page/Panel1/Image1/Image2/Cuadro info");
        var error = Project.Current.Get<Rectangle>("UI/Screens/Login_page/Panel1/Image1/Image2/Cuadro error");
        try
        {
            // Consulta SQL para buscar el usuario con el RFID ingresado
            string query = $"SELECT * FROM Usuarios WHERE Tarjeta_RFID = '{inputId}'";
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
                string i = ResultSet[0, 4].ToString();
                if (count > 0)
                {
                   
                    string nombreUsuario = ResultSet[0, 1].ToString();
                    // Mostrar el nombre del usuario en el cuadro de texto
                    
                    Autenticado.Value = true;
                    Rol.Value = i;
                    Log.Info(Autenticado.Value.ToString());
                    user.Value = nombreUsuario;
                    Logout.Visible = true;
                    Logout.Enabled = true;
                    Logout.Opacity = 100;
                    info.Visible = true;
                    error.Visible = false;

                }
            }
            else
            {
                
                
                Autenticado.Value = false;
                Rol.Value = "";
                Logout.Visible = false;
                Logout.Enabled = false;
                Logout.Opacity = 0;
                user.Value = "";
                info.Visible = false;
                error.Visible = true;
                Log.Info(Autenticado.Value.ToString());
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
}

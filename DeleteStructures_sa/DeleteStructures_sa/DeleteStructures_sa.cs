using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Windows;

using CustomAttributes;
using EsapiDebugSetup;
using Telemetry;
using VMS.TPS.Common.Model.API;

using Application = VMS.TPS.Common.Model.API.Application;
using ScriptContext = Context.ScriptContext;
using User = Context.User;

// TODO: Replace the following version attributes by creating AssemblyInfo.cs. You can do this in the properties of the Visual Studio project.
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0")]

[assembly: AssemblyType(ASSEMBLYTYPES.STANDALONE)]
[assembly: AssemblyTitle("Delete Structures v1.0")]
[assembly: AssemblyDescription("Delete user-selected structures from the currently loaded structure set.")]
[assembly: AssemblyAuthorship("Jeff Kempe", "")]
[assembly: AssemblyQA("")]
[assembly: AssemblyDeveloperLead("Jeff Kempe", "")]
[assembly: AssemblyManager("Stewart Gaede", "")]
[assembly: AssemblyKeywords("Structures;delete")]

[assembly: ESAPIScript(IsWriteable = true)]

namespace DeleteStructures_sa
{
    class Program
    {
        private static string ScriptName = "Delete Structures (stand-alone)";
        private static ScriptTelemetry _telemetry = null;
        private static Application _app = null;

        private static void SetUpTelemetry()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            System.Version version;
            AssemblyName assemblyName;

            assemblyName = assembly.GetName();
            version = assemblyName.Version;

            ScriptName = $"{ScriptName} v{version.Major}.{version.Minor}";

            string location = assembly.Location;
            DateTime now = DateTime.Now;
            IPAddress host = Dns.GetHostAddresses(Dns.GetHostName()).Where(x => x.AddressFamily == AddressFamily.InterNetwork).ToArray()[0];

            _telemetry = new ScriptTelemetry()
            {
                EntryDate = now,
                FileName = location,
                HostName = Environment.GetEnvironmentVariable("COMPUTERNAME"),
                HostIP = host.ToString(),
                ScriptName = ScriptName,
                StartTime = now,
                Status = "TBOX",
                Version = version,
            };
            if (location.ToUpper().Contains(@"\PROD\"))
            {
                _telemetry.Status = "PROD";
            }
            else if (location.ToUpper().Contains(@"\TBOX\"))
            {
                _telemetry.Status = "TBOX";
            }
            else if (location.ToUpper().Contains(@"\ESAPI\"))
            {
                _telemetry.Status = "ESAPI";
            }
            else
            {
                _telemetry.Status = "N/A";
            }
        }

        static Program()
        {
            SetUpTelemetry();
        }
        [STAThread]
        static void Main(/*string[] args*/)
        {
            try
            {
                using (Application app = Application.CreateApplication())
                {
                    #region Set up debugging

                    _app = app;
                    EsapiDebugSetupClass.RegisterContextFactory(ScriptContext.CreateContext);
                    //EsapiDebugSetupClass.PatientID = "ESAPI_Rename_Test";
                    //EsapiDebugSetupClass.PatientID = "Z_ESAPI_convert2HiRes";
                    EsapiDebugSetupClass.PatientID = "ESAPI_Rename_Test";
                    EsapiDebugSetupClass.CourseID = "";
                    EsapiDebugSetupClass.PlanID = "";
                    try
                    {
                        EsapiDebugSetupClass.LoadPatient(app);
                    }
                    catch (Exception e)
                    {
                        _ = MessageBox.Show($"Error loading patient: {e.Message}");
                        return;
                    }
                    ScriptContext _context = (ScriptContext)EsapiDebugSetupClass.CreateContext();
                    _context.CurrentUser = new User()
                    {
                        Name = app.CurrentUser.Name
                    };

                    Window _window = new Window();

                    #endregion

                    //Execute(app);
                    Execute(_context, _window);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
            }
        }
        //static void Execute(Application app)
        static void Execute(ScriptContext context, Window window)
        {
            try
            {
                #region Execute bits for the plugin - checking that everything is loaded

                window.Closing += Window_Closing;
                window.Title = ScriptName;

                _telemetry.CourseID = context.Course != null ? context.Course.Id : "NoCrs";
                _telemetry.PatientID = context.Patient != null ? context.Patient.Id : "NoPt";
                _telemetry.PlanID = context.PlanSetup != null ? context.PlanSetup.Id : "NoPln";
                _telemetry.UserID = context.CurrentUser.Id;

                if (context.Patient == null || context.StructureSet == null)
                {
                    _ = MessageBox.Show(
                            "Please load a patient and structure set before running this script.",
                            ScriptName,
                            MessageBoxButton.OK,
                            MessageBoxImage.Exclamation
                        );
                    //window.Loaded += Window_Loaded;
                    _telemetry.Error = new Exception("No patient/structure-set loaded.");
                    return;
                }
                _telemetry.Notes = $"StructureSet ID: {context.StructureSet.Id}";

                context.Patient.BeginModifications();

                window.Content = new DeleteForm(context);
                window.SizeToContent = SizeToContent.WidthAndHeight;
                window.MaxHeight = 600;
                window.MinHeight = 450;
                window.MaxWidth = 750;
                window.MinWidth = 600;

                #endregion

                _ = window.ShowDialog();    //  Not required outside stand-alone script
            }
            catch (Exception ex)
            {
                if (_telemetry != null)
                {
                    _telemetry.Error = ex;
                    if (_telemetry.StopTime == new DateTime())
                    {
                        _telemetry.StopTime = DateTime.Now;
                    }
                    LogTelemetry(); //  Not required outside stand-alone script
                }
            }
            finally
            {
                if (context.Patient != null)     //  Not required outside stand-alone script
                {
                    //_app.SaveModifications();  //  We don't want to actually apply the modifications to the database
                    _app.ClosePatient();
                }
            }
        }

        private static void LogTelemetry()
        {
            if (_telemetry != null)
            {
                if (_telemetry.StopTime == new DateTime())
                {
                    _telemetry.StopTime = DateTime.Now;
                }
#if DEBUG
                _ = MessageBox.Show($"Logging telemetry:\n{_telemetry}", "Telemetry", MessageBoxButton.OK, MessageBoxImage.Information);
#else
                _telemetry?.LogEntry();
#endif
            }
        }
        private static void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            LogTelemetry();
        }
        //private void Window_Loaded(object sender, RoutedEventArgs e)
        //{
        //    Window window = sender as Window;
        //    window.Close();
        //}
    }
}

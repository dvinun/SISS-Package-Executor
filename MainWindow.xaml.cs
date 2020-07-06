using Microsoft.SqlServer.Dts.Runtime;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace SISSPackageExecutor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IOnScreenLogger
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnSelectSSISPackage_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Data Transformation Services (*.dtsx)|*.dtsx";
            openFileDialog.FilterIndex = 2;
            openFileDialog.RestoreDirectory = true;

            var result = openFileDialog.ShowDialog();
            if (result == true)
            {
                tbSSISPackageFileLocation.Text = openFileDialog.FileName;
                btnExecuteSSISPackage.IsEnabled = true;
            }
        }

        private void AppIconHyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        class SSISPackageExecuteEventListener : DefaultEvents
        {
            IOnScreenLogger onScreenLogger = null;
            public SSISPackageExecuteEventListener(IOnScreenLogger onScreenLogger) => this.onScreenLogger = onScreenLogger;

            public override bool OnError(DtsObject source, int errorCode, string subComponent,
              string description, string helpFile, int helpContext, string idofInterfaceWithError)
            {
                var subComponentText = string.IsNullOrEmpty(subComponent) ? "" : $"/{subComponent}";
                onScreenLogger.Log($"SSIS Event Listener > Error - {source}{subComponentText} : {description}", LogType.Error);
                return false;
            }

            public override void OnInformation(DtsObject source, int informationCode, string subComponent,
                string description, string helpFile, int helpContext, string idofInterfaceWithError, ref bool fireAgain)
            {
                var subComponentText = string.IsNullOrEmpty(subComponent) ? "" : $"/{subComponent}";
                onScreenLogger.Log($"SSIS Event Listener > Information - {source}{subComponentText} : {description}", LogType.Info);
                fireAgain = true;
            }

            public override void OnExecutionStatusChanged(Executable exec, DTSExecStatus newStatus, ref bool fireAgain)
            {
                onScreenLogger.Log($"SSIS Event Listener > Execution Status Changed - New Statis: {newStatus}", LogType.Info);
                fireAgain = true;
            }

            public override void OnProgress(TaskHost taskHost, string progressDescription, int percentComplete, int progressCountLow,
                int progressCountHigh, string subComponent, ref bool fireAgain)
            {
                var subComponentText = string.IsNullOrEmpty(subComponent) ? "" : $" - {subComponent}";
                onScreenLogger.Log($"SSIS Event Listener > Progress Reported{subComponentText}: " +
                    $"Percentage Complete - {percentComplete}, Progress Description - {progressDescription}",
                    LogType.Info);
                fireAgain = true;
            }

            public override void OnTaskFailed(TaskHost taskHost)
            {
                onScreenLogger.Log($"SSIS Event Listener > Task Failed - Task Host Name: {taskHost.Name}", LogType.Failure);
            }

            public override void OnWarning(DtsObject source, int warningCode, string subComponent, string description,
                string helpFile, int helpContext, string idofInterfaceWithError)
            {
                var subComponentText = string.IsNullOrEmpty(subComponent) ? "" : $"/{subComponent}";
                onScreenLogger.Log($"SSIS Event Listener > Warning - {source}{subComponentText}: Description - {description}", LogType.Info);
            }
        }

        private async void btnExecuteSSISPackage_ClickAsync(object sender, RoutedEventArgs e)
        {
            try
            {
                var fileLocation = tbSSISPackageFileLocation.Text;
                var eventListener = new SSISPackageExecuteEventListener(this);
                tbLogger.Text = string.Empty;

                if (string.IsNullOrEmpty(fileLocation))
                {
                    Log("Selected file is invalid/empty. Please select a valid dtsx file and try again");
                    return;
                }

                Log($"Loading package...");

                await System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        var myApplication = new Microsoft.SqlServer.Dts.Runtime.Application();
                        var package = myApplication.LoadPackage(fileLocation, null);
                        Log($"Package {package.Name} loaded successfully.");

                        Log($"Package execution in progress. Please wait...");
                        var executionResults = package.Execute(null, null, eventListener, null, null);
                        Log($"Package execution complete. Result Code: {executionResults}");
                    }
                    catch (Exception ex)
                    {
                        Log($"Encountered exception... {Environment.NewLine}Message: {ex.Message}{Environment.NewLine}Stack Trace: {ex.StackTrace}", LogType.Exception);
                    }
                });
            }
            catch (Exception ex)
            {
                Log($"Encountered exception... {Environment.NewLine}Message: {ex.Message}{Environment.NewLine}Stack Trace: {ex.StackTrace}", LogType.Exception);
            }
        }

        public void Log(string message, LogType logType = LogType.Info)
        {
            Dispatcher.BeginInvoke(new Action(() =>
             {
                 if (tbLogger.Text.Length > 0)
                 {
                     tbLogger.AppendText(Environment.NewLine);
                     tbLogger.AppendText(Environment.NewLine);
                 }

                 tbLogger.AppendText($"{DateTime.Now} | {logType.ToString().ToUpper()} > " + message);
                 tbLogger.ScrollToEnd();
             }));
        }
    }
}

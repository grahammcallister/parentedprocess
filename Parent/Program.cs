using Mono.Options;
using ParentProcess;
using Pipe;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Parent
{
    public class Program
    {
        private BackgroundWorker _clientPipeWorker;
        private BackgroundWorker _serverPipeWorker;
        private BackgroundWorker _processManagerWorker;
        private ProcessManager _managedProcess;
        private PipeServer _pipeServer;
        private PipeClient _pipeClient;

        public Program()
        {
            MakeClientPipeWorker();
            MakeServerPipeWorker();
            MakeProcessManagerWorker();
        }

        private static string ParentProcessHandle = string.Empty;
        private static string ChildProcess = string.Empty;
        private static string Pipename = string.Empty;


        public static void Main(string[] args)
        {
            ParseAndHandleStartupArgs(args);
            Program program = new Program();

            // See https://stackoverflow.com/questions/7402146/cpu-friendly-infinite-loop

            // Create a IPC wait handle with a unique identifier.
            bool createdNew;
            var waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, "CF2D4313-33DE-489D-9721-6AFF69841DEA", out createdNew);
            var signaled = false;

            // If the handle was already there, inform the other process to exit itself.
            // Afterwards we'll also die.
            if (!createdNew)
            {
                waitHandle.Set();
                return;
            }

            // Wait if someone tells us to die or do every five seconds something else.
            do
            {
                signaled = waitHandle.WaitOne(TimeSpan.FromSeconds(5));
                // ToDo: Something else if desired.
            } while (!signaled);
        }

        private static void ParseAndHandleStartupArgs(string[] args)
        {
            var arg = args.Aggregate("", (current, next) => current + ", " + next);
            Debug.WriteLine($"Parent called with args {arg}");
            var options = new OptionSet()
            {
                { "c=|childprocess=", "full path to child process to parent", (string option) => ChildProcess = option },
                { "h=|handle=", "parent process window handle", (string option) => ParentProcessHandle = option },
                { "p=|pipename=", "name to use for named pipe comms", (string option) => Pipename = option }
            };
            options.Parse(args);
        }

        private void MakeServerPipeWorker()
        {
            _serverPipeWorker = new BackgroundWorker();
            _serverPipeWorker.DoWork += ServerPipeWorker_DoWork;
            _serverPipeWorker.RunWorkerAsync();
        }

        private void ServerPipeWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                _pipeServer = new PipeServer(ServerPipeName);
                _pipeServer.Connect += PipeServerOnConnect;
                _pipeServer.MessageReceived += PipeServerMessageReceived;

                Debug.WriteLine($"Parent PipeServer Open | ------------------- {ServerPipeName} ");
                _pipeServer.Open();

                // See https://stackoverflow.com/questions/7402146/cpu-friendly-infinite-loop

                // Create a IPC wait handle with a unique identifier.
                bool createdNew;
                var waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, Guid.NewGuid().ToString(), out createdNew);
                var signaled = false;

                // If the handle was already there, inform the other process to exit itself.
                // Afterwards we'll also die.
                if (!createdNew)
                {
                    waitHandle.Set();
                    return;
                }

                // Wait if someone tells us to die or do every five seconds something else.
                do
                {
                    signaled = waitHandle.WaitOne(TimeSpan.FromSeconds(5));
                    // ToDo: Something else if desired.
                } while (!signaled);
            } catch (Exception ex)
            {
                DebugMessage(ex, $"Parent PipeServer {MethodBase.GetCurrentMethod()}");
            }
            Exit();
        }

        private void DebugMessage(Exception ex, string source = "Parent")
        {
            Debug.WriteLine($"{source} | {ex.Message}");
        }

        private void Exit()
        {
            try
            {
                _managedProcess?.StopProcess();
                _pipeServer?.Close();
            } catch (Exception ex)
            {
                DebugMessage(ex, $"Parent {MethodBase.GetCurrentMethod()}");

            }
        }

        private void PipeServerMessageReceived(object sender, MessageEventArgs args)
        {
            Debug.WriteLine($"Parent PipeServer MessageReceived | " + args?.Message);
        }

        private void PipeServerOnConnect(object sender, EventArgs args)
        {
            Debug.WriteLine($"Parent PipeServer OnConnect | -------------------");
        }

        private void MakeClientPipeWorker()
        {
            _clientPipeWorker = new BackgroundWorker();
            _clientPipeWorker.DoWork += ClientPipeWorker_DoWork;
            _clientPipeWorker.RunWorkerAsync();
        }

        private void ClientPipeWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                Debug.WriteLine($"Parent PipeClient Open | ------------------- {ClientPipeName} ");
                _pipeClient = new PipeClient(ClientPipeName);
                _pipeClient.Connect += PipeClientOnConnect;
                _pipeClient.MessageReceived += PipeClientMessageReceived;

                
                _pipeClient.Open();

                // See https://stackoverflow.com/questions/7402146/cpu-friendly-infinite-loop

                // Create a IPC wait handle with a unique identifier.
                bool createdNew;
                var waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, Guid.NewGuid().ToString(), out createdNew);
                var signaled = false;

                // If the handle was already there, inform the other process to exit itself.
                // Afterwards we'll also die.
                if (!createdNew)
                {
                    waitHandle.Set();
                    return;
                }

                // Wait if someone tells us to die or do every five seconds something else.
                do
                {
                    signaled = waitHandle.WaitOne(TimeSpan.FromSeconds(5));
                    // ToDo: Something else if desired.
                } while (!signaled);
            }
            catch (Exception ex)
            {
                DebugMessage(ex, $"Parent PipeClient {MethodBase.GetCurrentMethod()}");
            }
            Exit();
        }

        private void PipeClientMessageReceived(object sender, MessageEventArgs args)
        {
            Debug.WriteLine($"Parent PipeClient MessageReceived | " + args?.Message);
            try
            {
                dynamic result = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(args?.Message);
                string eventName = result.eventName;
                if(string.Equals(eventName, "resize", StringComparison.InvariantCultureIgnoreCase)) {
                    Debug.WriteLine($"Parent PipeClient Resize Received | " + args?.Message);
                    Rectangle resized = new Rectangle();
                    resized.Width = (int) result["0"].Value;
                    resized.Height = (int) result["1"].Value;
                    _managedProcess.Resize(this, resized);
                }
                
                
            } catch (Exception ex) { Debug.WriteLine(ex.Message);  }
        }

        private void PipeClientOnConnect(object sender, EventArgs args)
        {
            Debug.WriteLine($"Parent PipeClient OnConnect | ------------------- {ClientPipeName} ");
        }

        private void MakeProcessManagerWorker()
        {
            _processManagerWorker = new BackgroundWorker();
            _processManagerWorker.DoWork += ProcessManagerWorker_DoWork;
            _processManagerWorker.RunWorkerAsync();
        }

        private void ProcessManagerWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                Debug.WriteLine($"Parent ProcessManagerWorker_DoWork | {ChildProcess} {Pipename} {ParentProcessHandle}");
                _managedProcess = new ProcessManager(ChildProcess, Pipename, Pipename);
                _managedProcess.ProcessStartedEvent += ManagedProcessStartedEvent;
                _managedProcess.ProcessMainWindowHandleFoundEvent += ManagedProcessMainWindowHandleFoundEvent;
                _managedProcess.ProcessStoppedEvent += ManagedProcessStoppedEvent;
                _managedProcess.ProcessUnhandledExceptionEvent += ManagedProcessUnhandledExceptionEvent;

                int handle = int.Parse(ParentProcessHandle);
                _managedProcess.ParentWindowHandle = new IntPtr(handle);

                Debug.WriteLine($"Parent ProcessManagerWorker_DoWork | Start Process ---------------------------------------");
                _managedProcess.StartProcess();

                // See https://stackoverflow.com/questions/7402146/cpu-friendly-infinite-loop

                // Create a IPC wait handle with a unique identifier.
                bool createdNew;
                var waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, Guid.NewGuid().ToString(), out createdNew);
                var signaled = false;

                // If the handle was already there, inform the other process to exit itself.
                // Afterwards we'll also die.
                if (!createdNew)
                {
                    waitHandle.Set();
                    return;
                }

                // Wait if someone tells us to die or do every five seconds something else.
                do
                {
                    signaled = waitHandle.WaitOne(TimeSpan.FromSeconds(5));
                    // ToDo: Something else if desired.
                } while (!signaled);
            } catch (Exception ex)
            {
                DebugMessage(ex, $"Parent {MethodBase.GetCurrentMethod()}");
            }
        }

        private void ManagedProcessStartedEvent(EventArgs args)
        {
            Debug.WriteLine($"Parent ProcessManagerWorker_DoWork | Process started ---------------------------------------");
            Task.Delay(1000);
            _managedProcess.FindChildMainWindowHandle();
        }

        private void ManagedProcessMainWindowHandleFoundEvent(EventArgs args)
        {
            Debug.WriteLine($"Parent ProcessManagerWorker_DoWork | Place in Parent ---------------------------------------");
            _managedProcess.PlaceInParent(this);
        }

        private void ManagedProcessUnhandledExceptionEvent(object sender, UnhandledExceptionEventArgs args)
        {
            DebugMessage(args.ExceptionObject as Exception, $"Parent {MethodBase.GetCurrentMethod()}");
        }

        private void ManagedProcessStoppedEvent(EventArgs args)
        {
            Debug.WriteLine($"Parent ProcessManagerWorker_DoWork | ManagedProcessStoppedEvent ---------------------------------------");
        }

        private string ServerPipeName { get => Program.Pipename + "_server"; }
        private string ClientPipeName { get => Program.Pipename + "_client"; }

    }
}

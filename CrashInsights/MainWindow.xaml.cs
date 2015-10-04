using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CrashInsights
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            mWatchdogThread.Start();

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var loopStart = DateTime.Now;
            // Loop for 20.000ms (20 seconds):
            // (I try not to use Thread.Sleep() here to a) simulate actual work and b) don't put the thread into suspend mode / make it inactive
            while ((DateTime.Now - loopStart).Milliseconds <= 20000)
            {

            }
        }

        Thread mWatchdogThread = new Thread(WatchdogLoop);

        /**
        WatchdogLoop polls Process.Responding every few milliseconds and tries to get a stack dump if the
        host process is not responding for more than 2 seconds.
        **/
        private static void WatchdogLoop()
        {
            var lastTimeResponding = DateTime.Now;
            var process = Process.GetCurrentProcess();
            while (true) {
                if (process.Responding)
                {
                    lastTimeResponding = DateTime.Now;
                }
                else
                {
                    // The threashold is 2 seconds for now:
                    if ((DateTime.Now - lastTimeResponding).TotalMilliseconds > 2000)
                    {
                        // App seems to be hanging, now get a stack dump using ClrMD (more or less copy & pasted from
                        // http://stackoverflow.com/questions/2057781/is-there-a-way-to-get-the-stacktraces-for-all-threads-in-c-like-java-lang-thre)
                        var result = new Dictionary<int, string[]>();
                        using (var dataTarget = Microsoft.Diagnostics.Runtime.DataTarget.AttachToProcess(process.Id, 500, Microsoft.Diagnostics.Runtime.AttachFlag.Passive))
                        {
                            string dacLocation = dataTarget.ClrVersions[0].TryGetDacLocation();
                            var runtime = dataTarget.CreateRuntime(dacLocation);

                            foreach (var t in runtime.Threads)
                            {
                                result.Add(
                                    t.ManagedThreadId,
                                    t.StackTrace.Select(f =>
                                    {
                                        if (f.Method != null)
                                        {
                                            return f.Method.Type.Name + "." + f.Method.Name;
                                        }

                                        return null;
                                    }).ToArray()
                                );
                            }
                        }
                        var json = JsonConvert.SerializeObject(result);
                        json = json; // Inspect the result in the debugger here.
                    }
                }
                Thread.Sleep(100); // Check every 100ms
            }
        }
    }
}

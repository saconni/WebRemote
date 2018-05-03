using Microsoft.Owin.FileSystems;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.StaticFiles;
using Owin;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Windows.Forms;

namespace WebRemote
{
    class Program
    {
        private static ManualResetEvent _exitingEvent = new ManualResetEvent(false);
        private static ManualResetEvent _exitEvent = new ManualResetEvent(false);

        private const string StaticFilesPath = @"C:\Users\Rodrigo\source\repos\WebRemote\WebRemote\www";
        private const string StaticScriptsPath = @"C:\Users\Rodrigo\source\repos\WebRemote\WebRemote\Scripts";
        private const string ApplicationUrl = @"http://localhost:8080";

        //
        // Main procedure
        //
        static void Main(string[] args)
        {
            Console.CancelKeyPress += (sender, a) =>
            {
                _exitingEvent.Set();
                a.Cancel = true;
            };

            SetConsoleCtrlHandler(ConsoleCtrlHandler, true);

            ScreenRecorder recorder = new ScreenRecorder();
            recorder.Start();

            ScreenCaptureController.Recorder = recorder;


            // this is the web application stack
            WebApp.Start(ApplicationUrl, (builder) =>
            {
                var config = new HttpConfiguration();
                config.Routes.MapHttpRoute(
                    name: "DefaultApi",
                    routeTemplate: "api/{controller}/{lastKnownFrame}",
                    defaults: new { lastKnownFrame = RouteParameter.Optional }
                    );

                builder.UseWebApi(config);

                builder.MapSignalR();

                 builder.UseFileServer(new FileServerOptions()
                {
                    EnableDirectoryBrowsing = false,
                    RequestPath = new Microsoft.Owin.PathString(@"/js"),
                    FileSystem = new PhysicalFileSystem(StaticScriptsPath)
                });

                builder.UseFileServer(new FileServerOptions()
                {
                    EnableDirectoryBrowsing = true,
                    FileSystem = new PhysicalFileSystem(StaticFilesPath)
                });
            });

            // wait for the user to exit the application
            Console.Write("Press CTRL + C to exit");
            _exitingEvent.WaitOne();
            _exitEvent.Set();
        }

        //
        // Handler for different console interruptions
        //
        public static bool ConsoleCtrlHandler(CtrlTypes ctrlType)
        {
            bool isClosing = false;
            switch(ctrlType)
            {
                case CtrlTypes.CTRL_C_EVENT:
                case CtrlTypes.CTRL_BREAK_EVENT:
                case CtrlTypes.CTRL_CLOSE_EVENT:
                case CtrlTypes.CTRL_LOGOFF_EVENT:
                case CtrlTypes.CTRL_SHUTDOWN_EVENT:
                    isClosing = true;
                    break;
            }

            if(isClosing)
            {
                _exitingEvent.Set();
                _exitEvent.WaitOne();
                return true;
            }

            return false;
        }

        //
        // Import some stuff from unmanaged dll's
        //
        #region unmanaged
        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine handler, bool add);
        public delegate bool HandlerRoutine(CtrlTypes ctrlType);
        public enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }
        #endregion
    }
}

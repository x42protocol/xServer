using System;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using X42.Server;

namespace X42.Utilities.Extensions
{
    /// <summary>
    ///     Extension methods for IX42Server interface.
    /// </summary>
    public static class X42ServerExtensions
    {
        /// <summary>
        ///     Installs handlers for graceful shutdown in the console, starts the x42 server and waits until it terminates.
        /// </summary>
        /// <param name="server">X42 Server to run.</param>
        public static async Task RunAsync(this IxServer server)
        {
            ManualResetEventSlim done = new ManualResetEventSlim(false);
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                Action shutdown = () =>
                {
                    if (!cts.IsCancellationRequested)
                    {
                        Console.WriteLine("Application is shutting down.");
                        try
                        {
                            cts.Cancel();
                        }
                        catch (ObjectDisposedException exception)
                        {
                            Console.WriteLine(exception.Message);
                        }
                    }

                    done.Wait();
                };

                AssemblyLoadContext assemblyLoadContext =
                    AssemblyLoadContext.GetLoadContext(typeof(XServer).GetTypeInfo().Assembly);
                assemblyLoadContext.Unloading += context => shutdown();

                Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    shutdown();
                    // Don't terminate the process immediately, wait for the Main thread to exit gracefully.
                    eventArgs.Cancel = true;
                };

                try
                {
                    await server.RunAsync(cts.Token, "Application started. Press Ctrl+C to shut down.",
                        "Application stopped.").ConfigureAwait(false);
                }
                finally
                {
                    done.Set();
                }
            }
        }

        /// <summary>
        ///     Starts a x42 server, sets up cancellation tokens for its shutdown, and waits until it terminates.
        /// </summary>
        /// <param name="server">x42 server to run.</param>
        /// <param name="cancellationToken">Cancellation token that triggers when the server should be shut down.</param>
        /// <param name="shutdownMessage">Message to display on the console to instruct the user on how to invoke the shutdown.</param>
        /// <param name="shutdownCompleteMessage">Message to display on the console when the shutdown is complete.</param>
        public static async Task RunAsync(this IxServer server, CancellationToken cancellationToken,
            string shutdownMessage, string shutdownCompleteMessage)
        {
            server.Start();

            if (!string.IsNullOrEmpty(shutdownMessage))
            {
                Console.WriteLine();
                Console.WriteLine(shutdownMessage);
                Console.WriteLine();
            }

            cancellationToken.Register(state => { ((IxServerLifetime) state).StopApplication(); },
                server.xServerLifetime);

            TaskCompletionSource<object> waitForStop =
                new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            server.xServerLifetime.ApplicationStopping.Register(obj =>
            {
                TaskCompletionSource<object> tcs = (TaskCompletionSource<object>) obj;
                tcs.TrySetResult(null);
            }, waitForStop);

            await waitForStop.Task.ConfigureAwait(false);

            server.Dispose();

            if (!string.IsNullOrEmpty(shutdownCompleteMessage))
            {
                Console.WriteLine();
                Console.WriteLine(shutdownCompleteMessage);
                Console.WriteLine();
            }
        }
    }
}
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace REstomp
{

    using MidFunc = Func<Func<IDictionary<string, object>, Task>, Func<IDictionary<string, object>, Task>>;
    using StompCommand = StompParser.Command;

    public class StompService
        : IDisposable
    {
        protected IStompParser Parser { get; set; }

        private TcpListener Listener { get; }

        private CancellationTokenSource CancellationSource { get; } = new CancellationTokenSource();

        public StompService(IPEndPoint endPoint, IStompParser parser)
        {
            Parser = parser;

            Listener = new TcpListener(endPoint);
        }

        public void Start(Action<Stack<MidFunc>> middlewareStackAction)
        {
            Task.Run(() =>
            {
                Listener.Start();

                while (!CancellationSource.IsCancellationRequested)
                {
                    if (Listener.Pending())
                    {
                        Listener.AcceptTcpClientAsync()
                            .ContinueWith(async (tcpClientTask) =>
                            {
                                if (tcpClientTask.IsCompleted && tcpClientTask.Result != null)
                                {
                                    using (var tcpClient = tcpClientTask.Result)
                                    using (var netStream = tcpClient.GetStream())
                                    {
                                        var middlewareStack = new Stack<MidFunc>();

                                        middlewareStackAction.Invoke(middlewareStack);

                                        var application = new StompPipeline(middlewareStack);

                                        while (!CancellationSource.IsCancellationRequested && tcpClient.Connected)
                                        {
                                            var frame = await Parser
                                                .ReadStompFrame(netStream, CancellationSource.Token)
                                                .UnWrapFrame();

                                            var environment = new Dictionary<string, object>
                                            {
                                                ["stomp.requestMethod"] = frame.Command,
                                                ["stomp.requestHeaders"] = frame.Headers,
                                                ["stomp.requestBody"] = frame.Body,
                                                ["stomp.responseMethod"] = null,
                                                ["stomp.responseHeaders"] = new Dictionary<string, string>().ToImmutableArray(),
                                                ["stomp.responseBody"] = new byte[0].ToImmutableArray(),
                                                ["stomp.terminateConnection"] = false
                                            };

                                            var responseFrame = await application.Process(environment);

                                            if (responseFrame != null)
                                            {
                                                Parser.WriteStompFrame(netStream, responseFrame);

                                                var terminatingCommands = new[]
                                                    {StompCommand.DISCONNECT, StompCommand.ERROR};
                                                if (terminatingCommands.Contains(responseFrame.Command))
                                                    break;
                                            }
                                        }

                                        Thread.Sleep(1000);
                                    }
                                }
                            });
                    }
                    else
                    {
                        Thread.Sleep(20);
                    }
                }
            });
        }

        public void Stop()
        {
            CancellationSource.Cancel();
        }

        public void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {

            }

        }
        public void Dispose()
        {
            Dispose(true);
        }

        ~StompService()
        {
            Dispose(false);
        }
    }
}

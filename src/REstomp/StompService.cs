using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace REstomp
{
    public class StompService
        : IDisposable
    {
        private TcpListener Listener { get; }

        private IList<TcpClient> Connections { get; } = new List<TcpClient>();
        private IList<TcpClient> NegotiatedConnections { get; } = new List<TcpClient>();

        public StompService(IPAddress ipAddress, int port)
        {
            var endpoint = new IPEndPoint(ipAddress, port);
            Listener = new TcpListener(endpoint);
        }

        public void Start()
        {
            StompFrame.Empty
                .With(frame => frame.Command, StompParser.Command.CONNECTED);

            Listener.Start(100);

            var acceptTask = Listener.AcceptTcpClientAsync();

            acceptTask.ContinueWith(async (tcpClientTask) =>
            {
                if (tcpClientTask.IsCompleted && tcpClientTask.Result != null)
                {
                    Connections.Add(tcpClientTask.Result);

                    var netStream = tcpClientTask.Result.GetStream();
                    //Begin negotiate

                    var streamAndFrame = await StompParser.ReadStompCommand(netStream.AsPrependableStream());

                    var headerParseResult =
                        await StompParser.ReadStompHeaders(streamAndFrame.Item1, streamAndFrame.Item2);

                    var bodyParseReault =
                        await StompParser.ReadStompBody(headerParseResult.Item1, headerParseResult.Item2, CancellationToken.None);
                }

                //Not accepted. Protocol error. 

            });
        }

        public void Stop()
        {
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

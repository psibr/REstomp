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
        protected IStompParser Parser { get; set; }

        private TcpListener Listener { get; }

        private IList<TcpClient> Connections { get; } = new List<TcpClient>();
        private IList<TcpClient> NegotiatedConnections { get; } = new List<TcpClient>();

        public StompService(IPEndPoint endPoint, IStompParser parser)
        {
            Parser = parser;
            
            Listener = new TcpListener(endPoint);
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

                    var frame = await Parser.ReadStompFrame(netStream);
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

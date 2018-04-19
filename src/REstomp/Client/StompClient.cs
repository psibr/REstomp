using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace REstomp.Client
{
    using StompCommand = StompParser.Command;

    public class StompClient
    {
        protected IStompParser Parser { get; }

        protected TcpClient Client { get; private set; }

        protected NetworkStream NetStream { get; private set; }

        public StompClient(IStompParser parser = null)
        {
            Parser = parser ?? new StompParser();
        }

        protected StompClientSession CurrentSession { get; set; }

        public async Task Send(IDictionary<string, string> headers, string content, string contentType = "plain/text", Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;

            var byteContent = encoding.GetBytes(content);

            var standardHeaders = new Dictionary<string, string>
            {
                ["session"] = CurrentSession.SessionId,
                ["content-length"] = byteContent.Length.ToString(),
                ["content-type"] = $"{contentType};charset=utf-8"
            };

            Parser.WriteStompFrame(NetStream, new StompFrame(StompCommand.SEND, standardHeaders.Union(headers), byteContent));

            var response = await Parser.ReadStompFrame(NetStream).UnWrapFrame();


        }

        public async Task<StompClientSession> Connect(string host, int port)
        {
            Client = new TcpClient();
            await Client.ConnectAsync(host, port);

            NetStream = Client.GetStream();

            Parser.WriteStompFrame(NetStream, new StompFrame(StompParser.Command.CONNECT, new Dictionary<string, string>
            {
                ["accept-version"] = "1.2"
            }));

            var response = await Parser.ReadStompFrame(NetStream).UnWrapFrame();

            var headers = response.Headers.UniqueKeys();

            CurrentSession = (response.Command == StompParser.Command.CONNECTED)
                ? new StompClientSession
                    {
                        SessionId = headers["session"],
                        NegotiatedVersion = headers["version"]
                    }
                : null;

            return CurrentSession;
        }
    }

    public class StompClientSession
    {
        public string SessionId { get; set; }

        public string NegotiatedVersion { get; set; }
    }


}
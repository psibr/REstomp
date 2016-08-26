using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace REstomp.Client
{
    public class StompClient
    {
        protected IStompParser Parser { get; }

        protected TcpClient Client { get; private set; }

        protected NetworkStream NetStream { get; private set; }

        public StompClient(IStompParser parser = null)
        {
            Parser = parser ?? new StompParser();
        }

        public async Task<bool> TryConnect(string host, int port)
        {
            Client = new TcpClient();
            await Client.ConnectAsync(host, port);

            NetStream = Client.GetStream();

            Parser.WriteStompFrame(NetStream, new StompFrame(StompParser.Command.CONNECT, new Dictionary<string, string>
            {
                ["version"] = "1.2"
            }));

            Thread.Sleep(5000);

            var response = await Parser.ReadStompFrame(NetStream).UnWrapFrame();

            return (response.Command == StompParser.Command.CONNECTED);
        }
    } 


}
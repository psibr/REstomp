using System;
using System.Collections.Generic;
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
            Listener.Start(100);

            var acceptTask = Listener.AcceptTcpClientAsync();

            acceptTask.ContinueWith(async (tcpClientTask) =>
            {
                if (tcpClientTask.IsCompleted && tcpClientTask.Result != null)
                {
                    Connections.Add(tcpClientTask.Result);

                    var netStream = tcpClientTask.Result.GetStream();
                    //Begin negotiate

                    var streamAndFrame = await StompStreamParser.ReadStompCommand(netStream, CancellationToken.None);



                    if (streamAndFrame.Item2.CommandString == null)
                        throw new Exception();

                    var headers = new Dictionary<string, string>();
                    var headerAndBodyBuffer = new byte[20];

                    var bytesFound = 0;

                    //after the command string
                    var eolsEncountered = 0;

                    var headerContents = new List<List<byte>>();
                    var headerLine = new List<byte>();

                    //index of the next character we wish to read
                    var parserIndex = 0;

                    while (true)
                    {
                        //if this message does not contain more headers
                        //(that is, if we encountered two EOLs in a row)
                        if (eolsEncountered > 1) break;

                        //add more bytes if we got the entire way through our last netStream.Read
                        if (parserIndex == bytesFound)
                        {
                            parserIndex = 0;

                            if (netStream.DataAvailable)
                            {
                                bytesFound = netStream.Read(headerAndBodyBuffer, 0, headerAndBodyBuffer.Length);
                            }
                        }

                        for (int i = parserIndex; i < bytesFound; i++)
                        {
                            parserIndex = i + 1;

                            //if this byte is LF
                            if (headerAndBodyBuffer[i] == 0x0a)
                            {

                                //add the line to contents and set eol to encountered
                                if (headerLine.Any()) headerContents.Add(headerLine);
                                headerLine = new List<byte>();

                                eolsEncountered++;
                                break;

                            }
                            //if the byte is not LF

                            //if the character is not CR
                            if (headerAndBodyBuffer[i] != 0x0d)
                            {
                                //add the character and set eolEncountered to false
                                headerLine.Add(headerAndBodyBuffer[i]);
                                eolsEncountered = 0;
                            }
                        }
                    }

                    var headerSegments = headerContents.Select(line =>
                        Encoding.UTF8.GetString(line.ToArray()).Split(':'));

                    foreach (var segments in headerSegments)
                    {
                        //Add header if key not already added (first come, first-only served)
                        if (!headers.ContainsKey(segments[0]))
                            headers.Add(segments[0], segments[1]);
                    }

                    var bodyBuilder = new List<byte>();

                    if (new[] { "SEND", "MESSAGE", "ERROR" }
                        .Contains(streamAndFrame.Item2.CommandString))
                    {
                        var contentLength = -1;
                        var bodyBytesRead = 0;

                        //Attempt to find and use the content-length header.
                        string contentLengthHeaderValue;
                        if (headers.TryGetValue("content-length", out contentLengthHeaderValue))
                            if (!int.TryParse(contentLengthHeaderValue, out contentLength))
                                throw new Exception(); //ERROR frame, content-length not assignable to int

                        //transfer unused header bytes to body
                        for (int i = parserIndex; i < bytesFound; i++)
                        {
                            bodyBuilder.Add(headerAndBodyBuffer[i]);
                        }
                    }
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
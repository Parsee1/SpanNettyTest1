namespace SpanNettyTest1
{
    using System;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using DotNetty.Buffers;
    using DotNetty.Codecs.Http;
    using DotNetty.Codecs.Http2;
    using DotNetty.Common.Utilities;
    using DotNetty.Transport.Bootstrapping;
    using DotNetty.Transport.Channels;
    using DotNetty.Transport.Channels.Sockets;
    using DotNetty.Transport.Libuv;
    using System.Threading;

    /// <summary>
    /// An HTTP2 client that allows you to send HTTP2 frames to a server using HTTP1-style approaches
    /// (via {@link io.netty.handler.codec.http2.InboundHttp2ToHttpAdapter}). Inbound and outbound
    /// frames are logged.
    /// 
    /// When run from the command-line, sends a single HEADERS frame to the server and gets back
    /// a "Hello World" response.
    /// See the ./http2/helloworld/frame/client/ example for a HTTP2 client example which does not use
    /// HTTP1-style objects and patterns.
    /// </summary>
    public class MyClient {

        public Bootstrap clientBootStrap;
        public bool isConnected = false;
        public Task<IChannel> task = null;
        public IChannel clientChannel = null;
        public Http2ClientInitializer initializer = null;

        public int PORT = 8443;
        public string hostname {
            get {
                return "127.0.0.1";
            }
        }

        public static void Main(string[] args) {
            new MyClient().connect();
        }
        
        public void connect() {
            if (!isConnected) {
                
                clientBootStrap = new Bootstrap();
                IEventLoopGroup group;
                bool useLibuv = false;
                if (useLibuv) {
                    group = new EventLoopGroup();
                }
                else {
                    group = new MultithreadEventLoopGroup();
                }
                clientBootStrap
                        .Group(group)
                        .Option(ChannelOption.TcpNodelay, true);    //  ??
                if (useLibuv) {
                    clientBootStrap.Channel<TcpChannel>();
                }
                else {
                    clientBootStrap.Channel<TcpSocketChannel>();
                }

                initializer = new Http2ClientInitializer(
                    hostname,
                    int.MaxValue);

                clientBootStrap.Handler(initializer);
                Console.WriteLine("Connecting...");
                task = clientBootStrap.ConnectAsync(     //new IPEndPoint(ClientSettings.Host));    //  TODO IP?
                    new IPEndPoint(IPAddress.Parse("127.0.0.1"),      //  new DnsEndPoint(inetHost, inetPort)
                    PORT));
                //443);
                //task.Wait(TimeSpan.FromMinutes(0.5));
                clientChannel = task.Result;    //  infinite wait here when running from server
                isConnected = task.IsCompleted && clientChannel != null;
                Console.WriteLine("Connected.");
            }
        }

        
    }
}

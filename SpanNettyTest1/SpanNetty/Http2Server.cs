namespace SpanNettyTest1
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Security;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading.Tasks;
    using DotNetty.Buffers;
    using DotNetty.Codecs;
    using DotNetty.Common.Concurrency;
    using DotNetty.Common.Internal;
    using DotNetty.Common.Utilities;
    using DotNetty.Handlers.Logging;
    using DotNetty.Handlers.Tls;
    using DotNetty.Transport.Bootstrapping;
    using DotNetty.Transport.Channels;
    using DotNetty.Transport.Channels.Sockets;
    using DotNetty.Transport.Libuv;

    /**
     * Demonstrates an Http2 server using Netty to display a bunch of images and
     * simulate latency. It is a Netty version of the <a href="https://http2.golang.org/gophertiles?latency=0">
     * Go lang HTTP2 tiles demo</a>.
     */
    public class Http2Server
    {
        readonly bool UseLibuv = false;

        readonly IEventLoopGroup bossGroup;
        readonly IEventLoopGroup workGroup;

        public Http2Server(IEventLoopGroup bossGroup, IEventLoopGroup workGroup)
        {
            this.bossGroup = bossGroup;
            this.workGroup = workGroup;
        }

        public Task<IChannel> StartAsync()
        {
            var bootstrap = new ServerBootstrap();
            bootstrap.Group(this.bossGroup, this.workGroup);

            if (UseLibuv)
            {
                bootstrap.Channel<TcpServerChannel>();
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                    || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    bootstrap
                        .Option(ChannelOption.SoReuseport, true)
                        .ChildOption(ChannelOption.SoReuseaddr, true);
                }
            }
            else
            {
                bootstrap.Channel<TcpServerSocketChannel>();
            }

            var tlsCertificate = new X509Certificate2(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dotnetty.com.pfx"), "password");

            bootstrap
                .Option(ChannelOption.SoBacklog, 1024)
                    
                .Handler(new LoggingHandler("LSTN2"))

                .ChildHandler(new ActionChannelInitializer<IChannel>(ch =>
                {
                    //ch.Pipeline.AddLast(new LoggingHandler("CONN2"));
                    //ch.Pipeline.AddLast("framing-enc", new LengthFieldPrepender2(2));
                    //ch.Pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder2(ushort.MaxValue, 0, 2, 0, 2));
                    //  ch.Pipeline.AddLast(new HttpConnectDetector());
                    TlsHandler tlsHandler = new TlsHandler(new ServerTlsSettings(tlsCertificate) {
                        ApplicationProtocols = new List<SslApplicationProtocol>(new[]{
                            SslApplicationProtocol.Http2,
                            SslApplicationProtocol.Http11
                        })
                    });
                    ch.Pipeline.AddLast(tlsHandler);
                    ch.Pipeline.AddLast(new Http2OrHttpHandler());
                }));

            return bootstrap.BindAsync(IPAddress.Parse("192.168.1.98"), 443);    //bootstrap.BindAsync(IPAddress.Loopback, 443);
        }
    }
  
}

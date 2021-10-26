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
//using Examples.Common;
using DotNetty.Transport.Libuv;
//using Http2Helloworld.Client;
using System.Runtime.InteropServices;
using System.Runtime;
using DotNetty.Handlers.Logging;
using DotNetty.Common.Internal.Logging;
using Microsoft.Extensions.Logging;
using DotNetty.Handlers.Tls;
using System.Net.Security;
using System.Collections.Generic;
using System.Threading;
using DotNetty.Transport.Channels.Local;
using NLog.Extensions.Logging;

namespace SpanNettyTest1
{
    public class Program
    {
        public static IEventLoopGroup bossGroup;
        public static IEventLoopGroup workGroup;
        public static async Task Main(string[] args) {
            
            SetConsoleLogger();
            IChannel http2Channel = null;

            try {
                bool useLibuv = false;   //ServerSettings.UseLibuv;
                Console.WriteLine("Transport type : " + (useLibuv ? "Libuv" : "Socket"));
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
                }

                Console.WriteLine($"Server garbage collection : {(GCSettings.IsServerGC ? "Enabled" : "Disabled")}");
                Console.WriteLine($"Current latency mode for garbage collection: {GCSettings.LatencyMode}");
                Console.WriteLine("\n");
                if (useLibuv) {
                    var dispatcher = new DispatcherEventLoopGroup();
                    bossGroup = dispatcher;
                    workGroup = new WorkerEventLoopGroup(dispatcher);
                }
                else {
                    bossGroup = new MultithreadEventLoopGroup(1);
                    workGroup = new MultithreadEventLoopGroup();
                }
                
                Http2Server http2 = new Http2Server(bossGroup, workGroup);
                http2Channel = await http2.StartAsync();

                //ch.CloseSafe();
                while (true)
                    Console.ReadLine();
            }
            catch (Exception exc) {
                Console.WriteLine(exc.ToString());
                Console.ReadKey();
            }
            finally {
                if (http2Channel != null) { await http2Channel.CloseAsync(); }
                //  if (httpChannel != null) { await httpChannel.CloseAsync(); }

                await workGroup.ShutdownGracefullyAsync();
                await bossGroup.ShutdownGracefullyAsync();
            }
        }

        private static Task<IChannel> StartAsync(ServerBootstrap serverBootStrap) {
            return serverBootStrap.BindAsync(IPAddress.Parse("192.168.1.98"), 443);
        }

        public class Http2ServerInitializer : ChannelInitializer<IChannel> {
            static readonly ILogger s_logger = InternalLoggerFactory.DefaultFactory.CreateLogger<Http2ServerInitializer>();

            readonly X509Certificate2 tlsCertificate;
            readonly int maxHttpContentLength;

            public Http2ServerInitializer(X509Certificate2 tlsCertificate)
                : this(tlsCertificate, 16 * 1024) {
            }

            public Http2ServerInitializer(X509Certificate2 tlsCertificate, int maxHttpContentLength) {
                if (maxHttpContentLength < 0) {
                    throw new ArgumentException("maxHttpContentLength (expected >= 0): " + maxHttpContentLength);
                }
                this.tlsCertificate = tlsCertificate;
                this.maxHttpContentLength = maxHttpContentLength;
            }

            protected override void InitChannel(IChannel channel) {
                ConfigureSsl(channel);
            }

            /**
             * Configure the pipeline for TLS NPN negotiation to HTTP/2.
             */
            void ConfigureSsl(IChannel ch) {
                ch.Pipeline.AddLast(new TlsHandler(new ServerTlsSettings(this.tlsCertificate) {
                    ApplicationProtocols = new List<SslApplicationProtocol>(new[]{
                                SslApplicationProtocol.Http2,
                                SslApplicationProtocol.Http11
                            })
                }
                ));
                ch.Pipeline.AddLast(new Http2OrHttpHandler());  //  TODO
            }
           
        }

        public static void SetConsoleLogger() {
            var f = new LoggerFactory();
            f.AddNLog();
            InternalLoggerFactory.DefaultFactory = f;
        }
    }
}

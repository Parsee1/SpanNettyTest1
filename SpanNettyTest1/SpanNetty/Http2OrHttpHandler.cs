using System;
using System.Net.Security;
using DotNetty.Codecs.Http;
using DotNetty.Codecs.Http2;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Channels;

namespace SpanNettyTest1
{

    public class Http2OrHttpHandler : ApplicationProtocolNegotiationHandler
    {
        const int MAX_CONTENT_LENGTH = 1024 * 100;

        public Http2OrHttpHandler(bool isQSEED = false)
            : base(SslApplicationProtocol.Http11)
        {
        }

        public MyClient myClient = null;

        public override void ChannelActive(IChannelHandlerContext ctx) {
            base.ChannelActive(ctx);
            myClient = new MyClient();
              myClient.connect();
        }
        protected override void ConfigurePipeline(IChannelHandlerContext ctx, SslApplicationProtocol protocol) {
            if (SslApplicationProtocol.Http2.Equals(protocol)) {
                ConfigureHttp2(ctx);
                Console.WriteLine("SslApplicationProtocol.Http2");
                return;
            }

            if (SslApplicationProtocol.Http11.Equals(protocol)) {
                ConfigureHttp1(ctx);
                Console.WriteLine("SslApplicationProtocol.Http1");
                return;
            }

            throw new InvalidOperationException("unknown protocol: " + protocol);
        }

        private void ConfigureHttp2(IChannelHandlerContext ctx) {

            //ctx.Pipeline.AddLast(Http2FrameCodecBuilder.ForServer().Build());
            //ctx.Pipeline.AddLast(
            //    //  这里面临一个重要选择，是按Frame处理，还是按IFullHttpRequest处理？
            //    //  new Http2MultiplexHandler(new MyHttp2FrameHandler())   //支持多路复用？好像只能按Frame处理
            //    new MyHttp2FrameHandler()
            //    ); //

            var connection = new DefaultHttp2Connection(true);
            InboundHttp2ToHttpAdapter listener = new InboundHttp2ToHttpAdapterBuilder(connection) {
                IsPropagateSettings = true,
                IsValidateHttpHeaders = false,
                MaxContentLength = MAX_CONTENT_LENGTH
            }.Build();

            //  Builder which builds DotNetty.Codecs.Http2.HttpToHttp2ConnectionHandler objects.
            ctx.Pipeline.AddLast(new HttpToHttp2ConnectionHandlerBuilder() {
                FrameListener = listener,
                // FrameLogger = TilesHttp2ToHttpHandler.logger,
                Connection = connection
            }.Build());

            Http2RequestHandler http2RequestHandler = new Http2RequestHandler(myClient);
            //http2RequestHandler.isQSEED = this.isQSEED;
            ctx.Pipeline.AddLast(http2RequestHandler);
        }

        private static void ConfigureHttp1(IChannelHandlerContext ctx) {
            ctx.Pipeline.AddLast(new HttpServerCodec(),
                                 new HttpObjectAggregator(MAX_CONTENT_LENGTH),  //  chunk？
                                 new FallbackRequestHandler());     //  TODO  Fallback实现
        }
    }
}


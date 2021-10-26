using System;
using DotNetty.Buffers;
using DotNetty.Codecs.Http;
using DotNetty.Codecs.Http2;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Libuv;
using DotNetty.Transport.Channels.Sockets;
using System.Threading.Tasks;
using System.Net;
using System.Threading;

namespace SpanNettyTest1
{
    /**
     * Handles all the requests for data. It receives a {@link IFullHttpRequest},
     * which has been converted by a {@link InboundHttp2ToHttpAdapter} before it
     * arrived here. For further details, check {@link Http2OrHttpHandler} where the
     * pipeline is setup.
     */
    public class Http2RequestHandler : SimpleChannelInboundHandler2<IFullHttpRequest>
    {
        public MyClient myClient = null;

        public Http2RequestHandler(MyClient myClient) : base() {
            this.myClient = myClient;
        }
        //  https://github.com/normanmaurer/netty-in-action/blob/2.0-SNAPSHOT/chapter8/src/main/java/nia/chapter8/BootstrapSharingEventLoopGroup.java
        //public override void ChannelActive(IChannelHandlerContext context) {    //  这里不执行？
        //    base.ChannelActive(context);
        //    if (!isConnected)
        //        Connect();
        //}

        protected override void ChannelRead0(IChannelHandlerContext ctx, IFullHttpRequest request)
        {
            QueryStringDecoder queryString = new QueryStringDecoder(request.Uri); 
            if (!myClient.isConnected) {
                myClient.connect();
            }

            while(myClient.task== null || !myClient.task.IsCompleted || myClient.clientChannel == null){
                Thread.Sleep(100);
            }

            string streamId = StreamId(request);

            //  TODO:
            //  bla bla blahh
            //  do something later here...
        }
        private static string StreamId(IFullHttpRequest request)
        {
            return request.Headers.GetAsString(HttpConversionUtil.ExtensionHeaderNames.StreamId);
        }

        private static void StreamId(IFullHttpResponse response, string streamId)
        {
            response.Headers.Set(HttpConversionUtil.ExtensionHeaderNames.StreamId, streamId);
        }

    }
}

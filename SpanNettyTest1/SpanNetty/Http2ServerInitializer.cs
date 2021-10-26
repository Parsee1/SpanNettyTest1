using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using DotNetty.Codecs.Http;
using DotNetty.Codecs.Http2;
using DotNetty.Common.Internal.Logging;
using DotNetty.Common.Utilities;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Logging;

namespace SpanNettyTest1
{
   
    public class Http2ServerInitializer : ChannelInitializer<IChannel>
    {
        static readonly ILogger s_logger = InternalLoggerFactory.DefaultFactory.CreateLogger<Http2ServerInitializer>();

        readonly X509Certificate2 tlsCertificate;
        readonly int maxHttpContentLength;  //？？？干啥用
        readonly bool isQSEED;

        public Http2ServerInitializer(X509Certificate2 tlsCertificate, bool isQSEED = false)
            : this(tlsCertificate, 16 * 1024, isQSEED)
        {
        }

        public Http2ServerInitializer(X509Certificate2 tlsCertificate, int maxHttpContentLength, bool isQSEED = false)
        {
            if (maxHttpContentLength < 0)
            {
                throw new ArgumentException("maxHttpContentLength (expected >= 0): " + maxHttpContentLength);
            }
            this.tlsCertificate = tlsCertificate;
            this.maxHttpContentLength = maxHttpContentLength;
            this.isQSEED = isQSEED;
        }

        protected override void InitChannel(IChannel channel)
        {
            ConfigureSsl(channel);
        }

        /**
         * Configure the pipeline for TLS NPN negotiation to HTTP/2.
         */
        void ConfigureSsl(IChannel ch)
        {
            ch.Pipeline.AddLast(new TlsHandler(new ServerTlsSettings(this.tlsCertificate)
            {
                ApplicationProtocols = new List<SslApplicationProtocol>(new[]
                        {
                            SslApplicationProtocol.Http2,
                            SslApplicationProtocol.Http11
                        })
            }
                ));
            ch.Pipeline.AddLast(new Http2OrHttpHandler(isQSEED));
        }
    }
}

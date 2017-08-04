using System;
using System.Collections.Generic;
using System.Text;

namespace Telegraf
{
    public interface IText
    {
        void Send(string command);
    }

    public static class TextTypeFactory
    {
        public static IText Create(MetricsConfig config)
        {
            switch (config.TextType)
            {
                case TextType.Http:
                    return CreateHttp(config);
                case TextType.Udp:
                    return CreateUdp(config);
                default:
                    return CreateUdp(config);
            }
        }

        public static ITextUDP CreateUdp(MetricsConfig config)
        {
            return new TextUDP(
                config.ServerName,
                config.ServerPort,
                config.MaxUDPPacketSize);
        }

        public static ITextHttp CreateHttp(MetricsConfig config)
        {
            return new TextHttp(
                config.ServerName,
                config.ServerPort);
        }
    }
}

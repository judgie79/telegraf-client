using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Telegraf
{
    public interface ITextHttp : IText
    {

    }

    public class TextHttp : ITextHttp, IDisposable
    {
        private string _name;
        private bool _disposed;
        private int _port;
        private Uri _telegrafUri;

        private HttpClient _webClient;

        public TextHttp(string name, int port = 8186)
        {
            _name = name;
            _port = port;
            UriBuilder b = new UriBuilder("http", _name, _port);
            _telegrafUri = b.Uri;
            _webClient = new HttpClient();
            _webClient.BaseAddress = _telegrafUri;
        }

        

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~TextHttp()
        {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                if (_webClient != null)
                {
                    try
                    {
                        _webClient.Dispose();
                    }
                    catch (Exception)
                    {
                        //Swallow since we are not using a logger, should we add LibLog and start logging??
                    }
                }
            }

            _disposed = true;
        }

        public void Send(string command)
        {
            byte[] data = Encoding.UTF8.GetBytes(command);

            _webClient.PostAsync("/write", new ByteArrayContent(data));
        }
    }
}

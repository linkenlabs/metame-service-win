using log4net;
using MetaMe.Sensors;
using Microsoft.Ccr.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MetaMe.WindowsClient
{
    class IconSaver
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        readonly Port<IconSaverMessage> _messagePort = new Port<IconSaverMessage>();

        public IconSaver(DispatcherQueue queue)
        {
            Action<IconSaverMessage> action = HandleIconSaverMessage;

            Arbiter.Activate(queue,
                Arbiter.Interleave(
                    new TeardownReceiverGroup(),
                    new ExclusiveReceiverGroup(
                        Arbiter.Receive(true, _messagePort, HandleIconSaverMessage)),
                    new ConcurrentReceiverGroup()));
        }

        public void PostAsync(IconSaverMessage message)
        {
            _messagePort.Post(message);
        }

        private void HandleIconSaverMessage(IconSaverMessage message)
        {
            try
            {
                if (string.IsNullOrEmpty(message.Url))
                {
                    string iconSavePath = IconHelpers.GetIconPath(message.AppName);
                    TryExtractWindowsProcessIcon(message.ProcessPath, iconSavePath);
                }
                else
                {
                    ExtractFavIconCached(message.Url);
                }
            }
            catch (WebException)
            {
                //cannot reach webpage. no need to log
                return;
            }
            catch (ArgumentException ex)
            {
                log.Warn(ex);
                log.Warn(JsonConvert.SerializeObject(message));
            }
            catch (ExternalException ex)
            {
                log.Warn(ex);
                log.Warn(JsonConvert.SerializeObject(message));
            }
            catch (Exception ex)
            {
                log.Warn(ex);
                log.Warn(JsonConvert.SerializeObject(message));
            }
        }

        private static void TryExtractWindowsProcessIcon(string processPath, string iconSavePath)
        {
            // check if icon exists
            if (File.Exists(iconSavePath))
            {
                return;
            }

            if (string.IsNullOrEmpty(processPath))
            {
                return;
            }

            if (!File.Exists(processPath))
            {
                return;
            }

            // UWP unsupported for now
            string fileName = Path.GetFileNameWithoutExtension(processPath);
            if (fileName == UWP.ProcessName)
            {
                return;
            }

            IconHelpers.ExtractProcessIcon(processPath, iconSavePath);
        }

        private static readonly HashSet<string> _extractFavIconFailedAttempts = new HashSet<string>();

        private static void ExtractFavIconCached(string url)
        {
            //use protocol and domain as the key
            string urlProtocolAndDomain = UrlUtils.ExtractUrlProtocolAndHost(url);
            if (string.IsNullOrEmpty(urlProtocolAndDomain))
            {
                return;
            }

            if (_extractFavIconFailedAttempts.Contains(urlProtocolAndDomain))
            {
                return;
            }

            try
            {
                ExtractFavIcon(url);
            }
            catch (WebException)
            {
                //dont try url again since it doesn't exist
                _extractFavIconFailedAttempts.Add(urlProtocolAndDomain);
                throw;
            }
            catch (ArgumentException)
            {
                //icon exists but is invalid icon. Don't try again
                _extractFavIconFailedAttempts.Add(urlProtocolAndDomain);
                throw;
            }
            catch (ExternalException)
            {
                _extractFavIconFailedAttempts.Add(urlProtocolAndDomain);
                throw;
            }
        }
        //pre: url is not null or empty and contains the protocol
        private static void ExtractFavIcon(string url)
        {
            string urlHost = UrlUtils.ExtractUrlHost(url);
            if (string.IsNullOrEmpty(urlHost))
            {
                return;
            }
            string savePath = IconHelpers.GetIconPath(urlHost);
            if (File.Exists(savePath))
            {
                return;
            }

            string urlProtocolAndDomain = UrlUtils.ExtractUrlProtocolAndHost(url);
            if (string.IsNullOrEmpty(urlProtocolAndDomain))
            {
                return;
            }

            var requestUrl = string.Format(@"https://icons.duckduckgo.com/ip2/{0}.ico", urlHost);

            WebRequest request = WebRequest.Create(requestUrl);
            WebResponse response = request.GetResponse();
            Stream stream = response.GetResponseStream();
            Image favicon = Image.FromStream(stream);
            favicon.Save(savePath);
        }

    }
}

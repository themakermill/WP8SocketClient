using System;
using Windows.Networking.Sockets;
using Windows.Storage.Streams; 
using Windows.UI.Core; 
using Windows.UI.Xaml; 
using Windows.UI.Xaml.Controls; 
using Windows.Web; 

namespace WP8SocketClient
{
    class SocketClient
    {
        private MessageWebSocket messageWebSocket;
        private DataWriter messageWriter;
        private bool connected = false;


        public async void SendMessage(string messageText, string hostName, string portNumber)
        {
            string websocketUri = "ws://" + hostName + ":" + portNumber;
            bool connecting = true;

            try
            {
                // Have we connected yet?
                if (messageWebSocket == null)
                {
                    // Validating the URI is required since it was received from an untrusted source (user input). 
                    // The URI is validated by calling TryGetUri() that will return 'false' for strings that are not
                    // valid WebSocket URIs.
                    // Note that when enabling the text box users may provide URIs to machines on the intrAnet 
                    // or intErnet. In these cases the app requires the "Home or Work Networking" or 
                    // "Internet (Client)" capability respectively.
                    Uri server;


                    if (!TryGetUri(websocketUri, out server))
                    {
                        return;
                    }

                    messageWebSocket = new MessageWebSocket();
                    messageWebSocket.Control.MessageType = SocketMessageType.Utf8;
                    messageWebSocket.MessageReceived += MessageReceived;
                    messageWebSocket.Closed += SocketClosed;

                    await messageWebSocket.ConnectAsync(server);
                    messageWriter = new DataWriter(messageWebSocket.OutputStream);

                    connected = true;
                }
                connecting = false;

                // Buffer any data we want to send.
                messageWriter.WriteString(messageText);

                // Send the data as one complete message.
                await messageWriter.StoreAsync();
            }
            catch (Exception ex) // For debugging
            {
                // Error happened during connect operation.
                if (connecting && messageWebSocket != null)
                {
                    messageWebSocket.Dispose();
                    messageWebSocket = null;
                }

                WebErrorStatus status = WebSocketError.GetStatus(ex.GetBaseException().HResult);

                switch (status)
                {
                    case WebErrorStatus.CannotConnect:
                    case WebErrorStatus.NotFound:
                    case WebErrorStatus.RequestTimeout:
                    case WebErrorStatus.Unknown:
                        throw;
                    default:
                        break;
                }
            }
        }
        private bool TryGetUri(string uriString, out Uri uri) 
        { 
            uri = null; 
 
            Uri webSocketUri; 

            if (!Uri.TryCreate(uriString.Trim(), UriKind.Absolute, out webSocketUri)) 
            { 
                return false; 
            } 
             
            // Fragments are not allowed in WebSocket URIs. 
            if (!String.IsNullOrEmpty(webSocketUri.Fragment)) 
            { 
                return false; 
            } 
 
            // Uri.SchemeName returns the canonicalized scheme name so we can use case-sensitive, ordinal string 
            // comparison. 
            if ((webSocketUri.Scheme != "ws") && (webSocketUri.Scheme != "wss")) 
            { 
                return false; 
            } 

            uri = webSocketUri; 
 
            return true; 
        } 


        private void MessageReceived(MessageWebSocket sender, MessageWebSocketMessageReceivedEventArgs args)
        {
            try
            {
                using (DataReader reader = args.GetDataReader())
                {
                    reader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;

                    string read = reader.ReadString(reader.UnconsumedBufferLength);
                }
            }
            catch (Exception ex) // For debugging
            {
                WebErrorStatus status = WebSocketError.GetStatus(ex.GetBaseException().HResult);

                if (status == WebErrorStatus.Unknown)
                {
                    throw;
                }
            }
        }

        public void Close()
        {
            try
            {
                if (messageWebSocket != null)
                {
                    messageWebSocket.Close(1000, "Closed due to user request.");
                    messageWebSocket = null;
                }
            }
            catch (Exception ex)
            {
                WebErrorStatus status = WebSocketError.GetStatus(ex.GetBaseException().HResult);

                if (status == WebErrorStatus.Unknown)
                {
                    throw;
                }
            }
        }

        // This may be triggered remotely by the server or locally by Close/Dispose()
        private void SocketClosed(IWebSocket sender, WebSocketClosedEventArgs args)
        {
            if (messageWebSocket != null)
            {
                messageWebSocket.Dispose();
                messageWebSocket = null;
            }
        }

    }
}

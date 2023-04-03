using UnityEngine;
using System;
using System.IO;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Paeezan.BSClient;

public partial class NetCtrl : MonoBehaviour, IGameService
{
    public class BSTcpClient
    {
        public TcpClient socket;

        private Stream stream;
        private Packet receivedData;
        private byte[] receiveBuffer;

        /// <summary>Attempts to connect to the server via TCP.</summary>
        public void Connect()
        {
            NetCtrl.EventPublisherNetworkState.Subscribe(NetworkMessage.Disconnected, (sender, args)=>
            {
                Disconnect();
            });

            socket = new TcpClient(AddressFamily.InterNetworkV6)
            {
                ReceiveTimeout = 100,
                SendTimeout = 100,
                ReceiveBufferSize = dataBufferSize,
                SendBufferSize = dataBufferSize,
            };

            receiveBuffer = new byte[dataBufferSize];
            //instance.ip = "127.0.0.1";
            //instance.ip = "65.108.62.133";
            Debug.Log($"Trying to connect: {GameController.Instance.ClientConfig.ServerIP}");
            Debug.Log($"Trying to connect: {GameController.Instance.ClientConfig.ServerPort}");
            socket.BeginConnect(GameController.Instance.ClientConfig.ServerIP, GameController.Instance.ClientConfig.ServerPort, ConnectCallback, socket);
        }

        /// <summary>Initializes the newly connected client's TCP-related info.</summary>
        private void ConnectCallback(IAsyncResult _result)
        {
            try
            {
                socket.EndConnect(_result);
            }
            catch(SocketException ex)
            {
                Debug.Log($"We faced socket exception: {ex.SocketErrorCode}");

                ThreadManager.ExecuteOnMainThread(() =>
                {
                    //NetCtrl.EventPublisherNetworkState.Notify(this.GetType(), NetworkMessage.RetryLogin, new EventArgs());
                    //NetCtrl.EventPublisherNetworkState.Notify(this.GetType(), NetworkMessage.Disconnected, new EventArgs());
                });
            }

            if (!socket.Connected)
            {
                return;
            }

            stream = (GameController.Instance.ClientConfig.UseSSL)?
                (Stream)new SslStream(socket.GetStream(), false, new RemoteCertificateValidationCallback(Stream_ValidateServerCertificate)):
                socket.GetStream();

            stream.WriteTimeout = 2000;
            stream.ReadTimeout = 2000;
            
            if(stream is SslStream ssls)
            {
                ssls.BeginAuthenticateAsClient(GameController.Instance.ClientConfig.ServerName, Stream_OnAuthenticated, ssls);
            }
            else 
            {
                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            }

            receivedData = new Packet();
        }

        /// <summary>Sends data to the client via TCP.</summary>
        /// <param name="_packet">The packet to send.</param>
        public void SendData(Packet _packet)
        {
            try
            {
                if (socket != null)
                {
                    stream.BeginWrite(_packet.ToArray(), 0, _packet.Length(), null, null); // Send data to server
                }
            }
            catch (Exception _ex)
            {
                Debug.Log(_ex.Message);
                //Debug.LogException(_ex);
                //NetCtrl.EventPublisherNetworkState.Notify(this, NetworkMessage.RetryLogin, new EventArgs());
                //NetCtrl.EventPublisherNetworkState.Notify(this, NetworkMessage.Disconnected, new EventArgs());
                Debug.Log($"Error sending data to server via TCP: {_ex}");
            }
        }

        private bool Stream_ValidateServerCertificate(
              object sender,
              X509Certificate certificate,
              X509Chain chain,
              SslPolicyErrors sslPolicyErrors)
        {
           if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            Console.WriteLine("Certificate error: {0}", sslPolicyErrors);

            // Do not allow this client to communicate with unauthenticated servers.
            return false;
        }

        private void Stream_OnAuthenticated(IAsyncResult res)
        {
            var ssls = (SslStream)res.AsyncState;
            if(ssls.IsAuthenticated)
            {
                ssls.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            }
            else
            {
                ThreadManager.ExecuteOnMainThread(() =>
                {
                    Debug.LogError("We have a problem in client SSL authentication process");
                    NetCtrl.EventPublisherNetworkState.Notify(this.GetType(), NetworkMessage.RetryLogin, new EventArgs());
                    NetCtrl.EventPublisherNetworkState.Notify(this.GetType(), NetworkMessage.Disconnected, new EventArgs());
                });
            }
        }

        /// <summary>Reads incoming data from the stream.</summary>
        private void ReceiveCallback(IAsyncResult _result)
        {
            try
            {
                if(stream == null)
                {
                    //handling exception when our implemented timeout system in NetworkHandler is fired
                    return;
                }
                int _byteLength = stream.EndRead(_result);
                if (_byteLength <= 0)
                {
                    ThreadManager.ExecuteOnMainThread(()=>{
                        //NetCtrl.EventPublisherNetworkState.Notify(this, NetworkMessage.RetryLogin, new EventArgs());
                        //NetCtrl.EventPublisherNetworkState.Notify(this, NetworkMessage.Disconnected, new EventArgs());
                    });
                    return;
                }

                byte[] _data = new byte[_byteLength];
                Array.Copy(receiveBuffer, _data, _byteLength);

                bool handleRes = HandleData(_data);
                receivedData.Reset(handleRes); // Reset receivedData if all data was handled
                if (handleRes)
                {
                    _packetLength = -1;
                    _packetReceivedLength = 0;
                }
                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            }
            catch(Exception ex)
            {
                Debug.Log(ex);
                _packetLength = -1;
                _packetReceivedLength = 0;
                ThreadManager.ExecuteOnMainThread(()=>{
                    //NetCtrl.EventPublisherNetworkState.Notify(this, NetworkMessage.RetryLogin, new EventArgs());
                    //NetCtrl.EventPublisherNetworkState.Notify(this, NetworkMessage.Disconnected, new EventArgs());
                });
            }
        }
        int _packetLength = - 1;
        int _packetReceivedLength = 0;
        /// <summary>Prepares received data to be used by the appropriate packet handler methods.</summary>
        /// <param name="_data">The recieved data.</param>
        private bool HandleData(byte[] _data)
        {
            receivedData.SetBytes(_data);

            if (receivedData.UnreadLength() >= 4 && _packetLength == -1)
            {
                // If client's received data contains a packet
                _packetLength = receivedData.ReadInt();
                if (_packetLength <= 0)
                {
                    // If packet contains no data
                    return true; // Reset receivedData instance to allow it to be reused
                }
            }

            //Debug.Log("UnreadLength: " + receivedData.UnreadLength());
            //Debug.Log("_packetLength: " + _packetLength);

            while (_packetLength > 0 && _packetLength <= receivedData.UnreadLength())
            {
                // While packet contains data AND packet data length doesn't exceed the length of the packet we're reading
                byte[] _packetBytes = receivedData.ReadBytes(_packetLength);
                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (Packet _packet = new Packet(_packetBytes))
                    {
                        int _packetId = _packet.ReadInt();
                        var resulted = _packet.GetResultedPacketRes();
                        //NetCtrl.StopTimeoutTimer();
                        if(packetHandlers.ContainsKey(_packetId))
                        {
                            packetHandlers[_packetId](resulted); // Call appropriate method to handle the packet
                        }
                        else
                        {
                            Debug.LogWarning("Packet Handler Not Found!");
                            Debug.Log("Packet type: " + _packetId);
                        }
#if DEBUG
                        //Debug.Log($"The packet type {resulted.PacketType} with this content received =>\n{resulted}");
#endif
                        NetCtrl.EventPublisherServerResponse.Notify(this, resulted.PacketType, resulted);
                    }
                });

                _packetLength = 0; // Reset packet length
                if (receivedData.UnreadLength() >= 4)
                {
                    // If client's received data contains another packet
                    _packetLength = receivedData.ReadInt();
                    if (_packetLength <= 0)
                    {
                        // If packet contains no data
                        return true; // Reset receivedData instance to allow it to be reused
                    }
                }
            }

            if (_packetLength <= 1)
            {
                return true; // Reset receivedData instance to allow it to be reused
            }

            return false;
        }

        /// <summary>Disconnects from the server and cleans up the TCP connection.</summary>
        public void Disconnect()
        {
            if(stream != null)
                stream.Dispose();
            stream = null;
            receivedData = null;
            receiveBuffer = null;
            if(socket != null)
                socket.Dispose();
            socket = null;
        }
    }
}
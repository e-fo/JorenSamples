using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

using PacketTypes = Paeezan.BSShared.PacketTypes;
using Paeezan.BSServer.TextConfig;

using Newtonsoft.Json;

namespace GameServer
{
    class Client
    {
        public static int dataBufferSize = 4096;

        public int id;
        public Player player;
        public TCP tcp;
        //public UDP udp;

        public List<Packet> packetLog;

        public Client(int _clientId)
        {
            id = _clientId;
            tcp = new TCP(id);
            packetLog = new List<Packet>();
            
            //udp = new UDP(id);
        }

        public class TCP
        {
            public TcpClient socket;

            private readonly int id;
            private NetworkStream stream;
            private Packet receivedData;
            private byte[] receiveBuffer;

            public TCP(int _id)
            {
                id = _id;
                
            }

            /// <summary>Initializes the newly connected client's TCP-related info.</summary>
            /// <param name="_socket">The TcpClient instance of the newly connected client.</param>
            public void Connect(TcpClient _socket)
            {
                socket = _socket;
                socket.ReceiveBufferSize = dataBufferSize;
                socket.SendBufferSize = dataBufferSize;
                socket.ReceiveTimeout = 30000;
                socket.SendTimeout = 30000;

                stream = socket.GetStream();

                receivedData = new Packet();
                receiveBuffer = new byte[dataBufferSize];

                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
                var res = new PacketTypes.WelcomeResponse()
                {   
                    versionForce = ServerConf.CLIENT_FORCE_UPDATE_VERSION,
                    versionOptional = ServerConf.CLIENT_OPTIONAL_UPDATE_VERSION,
                    downlaodUrlMap = new Dictionary<ClientDistribution, string>(ServerConf.APP_DOWNLOAD_URL_MAP)
                };
                ServerSend.Welcome(id, JsonConvert.SerializeObject(res), ServerRoutes.Welcome);
            }

            /// <summary>Sends data to the client via TCP.</summary>
            /// <param name="_packet">The packet to send.</param>
            public void SendData(Packet _packet)
            {
                try
                {
                    if (socket != null)
                    {
                        stream.BeginWrite(_packet.ToArray(), 0, _packet.Length(), WriteCallback, null); // Send data to appropriate client
                    }
                }
                catch (Exception _ex)
                {
                    Console.WriteLine($"Error sending data to player {id} via TCP: {_ex}");
                }
            }

            private void WriteCallback(IAsyncResult result)
            {
                //Console.WriteLine("e.foadian: write callback fired");
                //Console.WriteLine(result);
            }

            /// <summary>Reads incoming data from the stream.</summary>
            private void ReceiveCallback(IAsyncResult _result)
            {
                try
                {
                    int _byteLength = stream.EndRead(_result);
                    if (_byteLength <= 0)
                    {
                        Server.clients[id].Disconnect();
                        return;
                    }

                    byte[] _data = new byte[_byteLength];
                    Array.Copy(receiveBuffer, _data, _byteLength);

                    receivedData.Reset(HandleData(_data)); // Reset receivedData if all data was handled
                    stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
                }
                catch (Exception _ex)
                {
                    Console.WriteLine($"Error receiving TCP data: {_ex}");
                    Server.clients[id].Disconnect();
                }
            }

            /// <summary>Prepares received data to be used by the appropriate packet handler methods.</summary>
            /// <param name="_data">The recieved data.</param>
            private bool HandleData(byte[] _data)
            {
                //Console.WriteLine("Packet Recieved Length: " + _data.Length);

                int _packetLength = 0;

                receivedData.SetBytes(_data);

                if (receivedData.UnreadLength() >= 4)
                {
                    // If client's received data contains a packet
                    _packetLength = receivedData.ReadInt();
                    if (_packetLength <= 0)
                    {
                        // If packet contains no data
                        return true; // Reset receivedData instance to allow it to be reused
                    }
                }

                while (_packetLength > 0 && _packetLength <= receivedData.UnreadLength())
                {
                    // While packet contains data AND packet data length doesn't exceed the length of the packet we're reading
                    byte[] _packetBytes = receivedData.ReadBytes(_packetLength);
                    //ThreadManager.ExecuteOnMainThread(() =>
                    //{
                        using (Packet _packet = new Packet(_packetBytes))
                        {
                            int _packetId = _packet.ReadInt();
                            //Console.WriteLine("Packet Recieved: " + _packetId.ToString());
                            Server.packetHandlers[_packetId](id, _packet.GetResultedPacketReq()); // Call appropriate method to handle the packet
                        }
                    //});

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
                
                receivedData = receivedData.Clone(resetReadPos:true);
                return false;
            }

            /// <summary>Closes and cleans up the TCP connection.</summary>
            public void Disconnect()
            {
                if(socket != null)
                    socket.Close();
                stream = null;
                receivedData = null;
                receiveBuffer = null;
                socket = null;
            }
        }

        public class UDP
        {
            public IPEndPoint endPoint;

            private int id;

            public UDP(int _id)
            {
                id = _id;
            }

            /// <summary>Initializes the newly connected client's UDP-related info.</summary>
            /// <param name="_endPoint">The IPEndPoint instance of the newly connected client.</param>
            public void Connect(IPEndPoint _endPoint)
            {
                endPoint = _endPoint;
            }

            /// <summary>Sends data to the client via UDP.</summary>
            /// <param name="_packet">The packet to send.</param>
            public void SendData(Packet _packet)
            {
                Server.SendUDPData(endPoint, _packet);
            }

            /// <summary>Prepares received data to be used by the appropriate packet handler methods.</summary>
            /// <param name="_packetData">The packet containing the recieved data.</param>
            public void HandleData(Packet _packetData)
            {
                int _packetLength = _packetData.ReadInt();
                byte[] _packetBytes = _packetData.ReadBytes(_packetLength);

                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (Packet _packet = new Packet(_packetBytes))
                    {
                        int _packetId = _packet.ReadInt();
                        //Console.WriteLine("Packet Recieved UDP: " + _packetId.ToString());
                        Server.packetHandlers[_packetId](id, _packet.GetResultedPacketReq()); // Call appropriate method to handle the packet
                    }
                });
            }


            /// <summary>Cleans up the UDP connection.</summary>
            public void Disconnect()
            {
                endPoint = null;
            }
        }

        /// <summary>Sends the client into the game and informs other clients of the new player.</summary>
        /// <param name="_playerName">The username of the new player.</param>
        

        /// <summary>Disconnects the client and stops all network traffic.</summary>
        public void Disconnect()
        {
            try
            {
                tcp.Disconnect();
                if(tcp.socket != null)
                    Console.WriteLine($"{tcp.socket.Client.RemoteEndPoint} has disconnected.");
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }


            player = null;

            //PacketTypes.BattleEndRequest req = new PacketTypes.BattleEndRequest();

            //ServerHandle.BattleEnd(id, req);
            if (General.users.ContainsKey(id))
            {
                string userId = General.users[id].id.ToString();
                //MatchmakeHandler.UserDisconnected(userId);
                General.users.Remove(id);
                General.playerID.Remove(userId);
            }
            
            //udp.Disconnect();
        }
    }
}

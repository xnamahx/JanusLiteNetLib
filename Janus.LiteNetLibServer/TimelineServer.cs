using LiteNetLib;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using LiteNetLib.Utils;

namespace Janus
{
    /// <summary>
    /// Triggered when timeline server is starting
    /// </summary>
    /// <param name="port">Port number on which timeline server listens</param>
    public delegate void ServerStartingHandler(int port);

	/// <summary>
	/// Networks to the timeline clients
	/// Establishes connections and processes incoming and outgoing messages
	/// </summary>
	public static class TimelineServer
	{
        private static ServerListener _serverListener;

        static Dictionary<DeliveryMode, SendOptions> _deliveryModes = new Dictionary<DeliveryMode, SendOptions>()
		{
			{ DeliveryMode.ReliableOrdered, SendOptions.ReliableOrdered},
			{ DeliveryMode.ReliableUnordered, SendOptions.ReliableUnordered},
			{ DeliveryMode.Unreliable, SendOptions.Unreliable}
		};

		/// <summary>
		/// Triggered when timeline server is starting
		/// </summary>
		public static event ServerStartingHandler ServerStarting;

		/// <summary>
		/// True if timeline server is running
		/// </summary>
		public static bool IsStarted
		{
			get { return _netServer != null && _netServer.IsRunning; }
		}

		/// <summary>
		/// Provides a message interface to synchronizes multiple, remote timeline managers.
		/// </summary>
		public static TimelineSynchronizer TimelineSynchronizer
		{
			get { return _timelineSyncer; }
		}

		static NetManager _netServer;
		static TimelineSynchronizer _timelineSyncer;
		static bool _autoStep;
		static float _stepInterval;
		static Thread _workThread;
        public static Dictionary<ushort, NetPeer> _peerConnections;

        static TimelineServer ()
		{
            _peerConnections = new Dictionary<ushort, NetPeer>();
            _timelineSyncer = new TimelineSynchronizer();
		}

        /// <summary>
        /// Start timeline server - starts separate thread that processes incoming messages
        /// </summary>
        /// <param name="autoStep">If true, automatically increment time.  If false, time must be manually incremented by calling Step</param>
        public static bool Start(bool autoStep)
        {
            Config config = LoadSettings();

            _autoStep = autoStep;
            _stepInterval = 1f / config.StepRate;

            _serverListener = new ServerListener();
            _netServer = new NetManager(_serverListener, config.MaxConnections, "janus");

            _netServer.Start(config.Port);
            _serverListener.Server = _netServer;


            if (ServerStarting != null)
                ServerStarting(_netServer.LocalEndPoint.Port);

            _workThread = new Thread(Work);
            _workThread.Start();

            return true;
        }

        class Config
        {
            public ushort Port = 14242;
            public ushort MaxConnections = 32;
            public ushort StepRate = 100;
        }


        static Config LoadSettings()
        {
            Config config = new Config();

            if (JsonSerialization.CheckJsonFile(@"server.settings"))
            {
                config = JsonSerialization.ReadFromJsonFile<Config>(@"server.settings");
            }

            /*config.AddChild("Port", 14242);
            config.AddChild("MaxConnections", 32);
            config.AddChild("StepRate", 100);*/
            JsonSerialization.WriteToJsonFile<Config>(@"server.settings", config);

            return config;
        }


        /// <summary>
        /// Processes incoming and outgoing messages
        /// </summary>
        public static void Update()
		{
            /*NetDataWriter dataWriter = new NetDataWriter();
            dataWriter.Put("Server DISCOVERY REQUEST");
            _netServer.SendToAll(dataWriter, SendOptions.ReliableUnordered);*/

            _netServer.PollEvents();

            foreach (var connPair in _peerConnections)
            {
                ushort peerIndex = connPair.Key;
                NetPeer peer = connPair.Value;

                // Don't send messages until we're connected.
                if (peer.ConnectionState != ConnectionState.Connected)
                    continue;

                foreach (var message in _timelineSyncer.GetOutgoingMessages(peerIndex))
                {
                    if (message == null)
                        continue;

                    NetDataWriter dataWriter = new NetDataWriter(true, sizeof(byte) + message.Data.Length);
                    dataWriter.Put((byte)message.MessageType);
                    dataWriter.Put(message.Data);
                    peer.Send(dataWriter, (SendOptions) message.DeliveryMode);

                    //netConnection.SendMessage(om, _deliveryModes[message.DeliveryMode], 0);
                }
            }

        }

		/// <summary>
		/// Increments the current time
		/// </summary>
		/// <param name="deltaTime">Time step size in seconds</param>
		public static void Step(float deltaTime)
		{
			_timelineSyncer.Step(deltaTime);
            TimelineManager.Default.Step(deltaTime);

        }

        static ushort GetNextFreePeerIndex ()
		{
			ushort peerIndex = 0;

			do
			{
				if (peerIndex == ushort.MaxValue)
					peerIndex = ushort.MinValue;
				else peerIndex++;
			}
			while (_peerConnections.ContainsKey(peerIndex));

			return peerIndex;
		}

		static void Work()
		{
			DateTime lastStepTime = DateTime.Now;

			while (_netServer.IsRunning)
			{
				Update();

				if (_autoStep)
				{
					DateTime currentTime = DateTime.Now;
					float deltaTime = (float)currentTime.Subtract(lastStepTime).TotalSeconds;

					if (deltaTime >= _stepInterval)
					{
						Step(deltaTime);
						lastStepTime = currentTime;
					}
					Thread.Sleep(1);
				}
			}
		}

		/// <summary>
		/// Stops net server
		/// </summary>
		public static void Stop()
		{
			_netServer.Stop();

			while (_netServer.IsRunning)
			{
				Thread.Sleep(1);
			}

			_netServer = null;
		}
	}

    class ServerListener : INetEventListener
    {
        public NetManager Server;

        public void OnPeerConnected(NetPeer peer)
        {
            Console.WriteLine("[Server] Peer connected: " + peer.EndPoint);
            var peers = Server.GetPeers();
            ushort peerIndex = 0;

            foreach (var netPeer in peers)
            {
                Console.WriteLine("ConnectedPeersList: id={0}, ep={1}", peer.ConnectId, netPeer.EndPoint);
                if (netPeer.ConnectId == peer.ConnectId)
                {
                    TimelineServer.TimelineSynchronizer.ConnectPeer(peerIndex);
                }
                TimelineServer._peerConnections.Add(peerIndex, netPeer);
                peerIndex++;
            }


        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Console.WriteLine("[Server] Peer disconnected: " + peer.EndPoint + ", reason: " + disconnectInfo.Reason);
            ushort peerIndex = 0;

            foreach (var netPeer in TimelineServer._peerConnections.Values)
            {
                Console.WriteLine("ConnectedPeersList: id={0}, ep={1}", peer.ConnectId, netPeer.EndPoint);
                if (netPeer.ConnectId == peer.ConnectId)
                {
                    break;
                }
                peerIndex++;
            }

            TimelineServer.TimelineSynchronizer.DisconnectPeer(peerIndex);
            TimelineServer._peerConnections.Remove(peerIndex);
        }

        public void OnNetworkError(NetEndPoint endPoint, int socketErrorCode)
        {
            Console.WriteLine("[Server] error: " + socketErrorCode);
        }

        public void OnNetworkReceive(NetPeer peer, NetDataReader reader)
        {
            //echo
            //peer.Send(reader.Data, SendOptions.ReliableUnordered);

            Console.WriteLine("[Server] NetworkReceive: ");

            try
            {

                ushort peerIndx = 0;
                foreach (var peerConection in TimelineServer._peerConnections)
                {
                    if (peerConection.Value.ConnectId==peer.ConnectId)
                    {
                        peerIndx = peerConection.Key;
                    }
                }

                var type = reader.GetByte();
                var data = reader.GetBytes();

                TimelineMessage timelineMessage = new TimelineMessage((TimelineMessageType)type, data);
                TimelineServer.TimelineSynchronizer.ProcessIncomingMessage(peerIndx, timelineMessage);
                TimelineManager.Default.ProcessIncomingMessage(timelineMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Server] Exception! " + ex.Message);
            }
            
            //fragment log
            if (reader.AvailableBytes == 13218)
            {
                Console.WriteLine("[Server] TestFrag: {0}, {1}", reader.Data[0], reader.Data[13217]);
            }
        }

        public void OnNetworkReceiveUnconnected(NetEndPoint remoteEndPoint, NetDataReader reader, UnconnectedMessageType messageType)
        {
            Console.WriteLine("[Server] ReceiveUnconnected: {0}", reader.GetString(100));
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {

        }

    }
}
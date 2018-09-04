using System;
using System.Threading;
using System.Collections.Generic;
using LiteNetLib.Utils;
using LiteNetLib;

namespace Janus
{
    /// <summary>
    /// Triggered when the client successfully connects to the timeline synchronizer
    /// </summary>
    /// <param name="index">Client index number as assigned by the timeline synchronizer</param>
    public delegate void ClientConnectedHandler(ushort index);
	/// <summary>
	/// Triggered when the client disconnects from the timeline synchronizer
	/// </summary>
	/// <param name="reason">Reason for disconnection</param>
	public delegate void ClientDisconnectedHandler(string reason);

	/// <summary>
	/// Timeline client handles networking between client and the timeline server
	/// Handles connection, disconnection, timing and processing of incoming and outgoing messages
	/// </summary>
	public static class TimelineClient
	{
        private static ClientListener _clientListener;

        static Dictionary<DeliveryMode, SendOptions> _deliveryModes = new Dictionary<DeliveryMode, SendOptions>()
        {
            { DeliveryMode.ReliableOrdered, SendOptions.ReliableOrdered},
            { DeliveryMode.ReliableUnordered, SendOptions.ReliableUnordered},
            { DeliveryMode.Unreliable, SendOptions.Unreliable}
        };

        /// <summary>
        /// Triggered when the client successfully connects to the timeline synchronizer
        /// </summary>
        public static event ClientConnectedHandler Connected;
		/// <summary>
		/// Triggered when the client disconnects from the timeline synchronizer
		/// </summary>
		public static event ClientDisconnectedHandler Disconnected;

		/// <summary>
		/// Status of the network client
		/// </summary>
		/// <returns>True if the net client is running</returns>
		public static bool IsStarted
		{
			get { return _netClient != null && _netClient.IsRunning; }
		}

		/// <summary>
		/// Status of the network connection
		/// </summary>
		/// <returns>True if connected</returns>
		public static bool IsConnected
		{
			get
			{
				if (_netClient != null)
					return _netClient.IsRunning;
				return false;
			}
		}

		/// <summary>
		/// Current time measured in seconds since the timeline manager started
		/// </summary>
		public static double Now
		{
			get { return TimelineManager.Default.Now; }
		}

		/// <summary>
		/// Unique index for the timeline client - assigned by the timeline synchronizer when connecting
		/// </summary>
		public static ushort Index
		{
			get { return _index; }
		}

		/// <summary>
		/// Round trip time in seconds between the client and the timeline synchronizer
		/// </summary>
		public static float RTT
		{
			get
			{
				if (_netClient.IsRunning)
					return _netClient.PingInterval;
				return float.PositiveInfinity;
			}
		}

		static NetManager _netClient;
		static Thread _workThread;
		static bool _autoStep;
		static float _stepInterval;
		static ushort _index;
		static bool _connectSuccess;
		static bool _disconnectRequested;
		static string _disconnectReason;

        /// <summary>
        /// Start timeline client - starts separate thread that processes incoming messages
        /// </summary>
        /// <param name="autoAdd">If true, automatically connect to the timeline synchronizer using information found in TimelineClient.ini.  If false, must connect manually using Connect</param>
        /// <param name="autoStep">If true, automatically increment time.  If false, time must be manually incremented by calling Step</param>
        public static bool Start(bool autoAdd = true, bool autoStep = true)
        {
            Config config = LoadSettings();

            _autoStep = autoStep;
            _stepInterval = 1f / config.StepRate;

            _clientListener = new ClientListener();

            _netClient = new NetManager(_clientListener, "janus");
            _netClient.Start();

            if (autoAdd)
            {
                _netClient.Connect(config.ServerAddress, config.ServerPort);
            }

            _workThread = new Thread(Work);
            _workThread.Start();

            return true;
        }

        class Config
        {
            public string ServerAddress = "127.0.0.1";
            public ushort ServerPort = 14242;
            public ushort StepRate = 60;
        }

        static Config LoadSettings()
        {
            Config config = new Config();

            if (JsonSerialization.CheckJsonFile(@"client.settings"))
            {
                config = JsonSerialization.ReadFromJsonFile<Config>(@"client.settings");
            }

            /*config.AddChild("Port", 14242);
            config.AddChild("MaxConnections", 32);
            config.AddChild("StepRate", 100);*/
            JsonSerialization.WriteToJsonFile<Config>(@"client.settings", config);

            return config;
        }
        /// <summary>
        /// Connect to the timeline server
        /// </summary>
        /// <param name="serverAddress">Ip address of the server</param>
        /// <param name="serverPort">Port number on server</param>
        public static void Connect(string serverAddress, int serverPort)
		{
			_netClient.Connect(serverAddress, serverPort);
		}
		/// Processes incoming and outgoing messages
		/// </summary>
		static void Update()
		{

            if (!_netClient.IsRunning)
                return;


            /*foreach (var message in TimelineManager.Default.GetOutgoingMessages())
            {
                NetOutgoingMessage om = _netClient.CreateMessage(sizeof(byte) + message.Data.Length);
                om.Write((byte)message.MessageType);
                om.Write(message.Data);

                _netClient.SendMessage(om, _deliveryModes[message.DeliveryMode]);

            }*/

            /*
            ;
            _netClient.SendToAll(dataWriter, SendOptions.ReliableUnordered);*/

            _netClient.PollEvents();

            if (!_netClient.IsRunning)
                return;

            foreach (var message in TimelineManager.Default.GetOutgoingMessages())
            {
                NetDataWriter dataWriter = new NetDataWriter();
                dataWriter.Put((byte)message.MessageType);
                dataWriter.Put(message.Data);
                _netClient.GetFirstPeer().Send(dataWriter, SendOptions.ReliableUnordered);
            }


        }

        /// <summary>
        /// Increments the current time
        /// Triggers events for connection and disconnection
        /// </summary>
        /// <param name="deltaTime">Time step size in seconds</param>
        public static void Step (double deltaTime)
		{
			TimelineManager.Default.Step(deltaTime);
			
			if (_connectSuccess)
			{
				_connectSuccess = false;
				
				if (Connected != null)
					Connected(_index);
			}

			if (_disconnectRequested)
			{
				_connectSuccess = false;
				_disconnectRequested = false;
				_index = 0;
				
				if (Disconnected != null)
					Disconnected(_disconnectReason);

				_disconnectReason = null;
			}
		}

		static void Work()
		{
			DateTime lastStepTime = DateTime.Now;

			while (_netClient.IsRunning)
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
		/// Stops net client
		/// </summary>
		public static void Stop()
		{
			_index = 0;
			_netClient.Stop();

			while (_netClient.IsRunning)
			{
				Thread.Sleep(1);
			}

			_netClient = null;
		}
	}

    class ClientListener : INetEventListener
    {

        public void OnPeerConnected(NetPeer peer)
        {
            Console.WriteLine("[Client] connected to: {0}:{1}", peer.EndPoint.Host, peer.EndPoint.Port);

            /*NetDataWriter dataWriter = new NetDataWriter();
            for (int i = 0; i < 5; i++)
            {
                dataWriter.Reset();
                dataWriter.Put(0);
                dataWriter.Put(i);
                peer.Send(dataWriter, SendOptions.ReliableUnordered);

                dataWriter.Reset();
                dataWriter.Put(1);
                dataWriter.Put(i);
                peer.Send(dataWriter, SendOptions.ReliableOrdered);

                dataWriter.Reset();
                dataWriter.Put(2);
                dataWriter.Put(i);
                peer.Send(dataWriter, SendOptions.Sequenced);

                dataWriter.Reset();
                dataWriter.Put(3);
                dataWriter.Put(i);
                peer.Send(dataWriter, SendOptions.Unreliable);
            }

            //And test fragment
            byte[] testData = new byte[13218];
            testData[0] = 192;
            testData[13217] = 31;
            peer.Send(testData, SendOptions.ReliableOrdered);*/
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Console.WriteLine("[Client] disconnected: " + disconnectInfo.Reason);
        }

        public void OnNetworkError(NetEndPoint endPoint, int socketErrorCode)
        {
            Console.WriteLine("[Client] error! " + socketErrorCode);
        }

        public void OnNetworkReceive(NetPeer peer, NetDataReader reader)
        {

            try
            {
                var type = reader.GetByte();
                var data = reader.GetBytes();
                TimelineMessage timelineMessage = new TimelineMessage((TimelineMessageType)type, data);

				TimelineManager.Default.ProcessIncomingMessage(timelineMessage);
            }
            catch(Exception ex)
            {
                Console.WriteLine("[Client] Exception! " + ex.Message);
            }


        }

        public void OnNetworkReceiveUnconnected(NetEndPoint remoteEndPoint, NetDataReader reader, UnconnectedMessageType messageType)
        {

        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {

        }
    }
}
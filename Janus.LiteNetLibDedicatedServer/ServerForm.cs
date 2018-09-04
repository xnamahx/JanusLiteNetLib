using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace Janus
{
	/// <summary>
	/// Form application that can be used as a standalone application to run the timeline server
	/// </summary>
	public partial class ServerForm : Form
	{
		/// <summary>
		/// Form for standalone application to run the timeline server
		/// </summary>
		public ServerForm()
		{
			InitializeComponent();

			TimelineServer.ServerStarting += OnServerStarting;
			TimelineServer.TimelineSynchronizer.PeerConnected += OnPeerConnected;
			TimelineServer.TimelineSynchronizer.PeerDisconnected += OnPeerDisconnected;
			TimelineServer.TimelineSynchronizer.PeerUpdated += OnPeerUpdated;
			TimelineServer.TimelineSynchronizer.TimelineCreated += OnTimelineCreated;
			TimelineServer.TimelineSynchronizer.TimelineUpdated += OnTimelineUpdated;
			TimelineServer.TimelineSynchronizer.TimelineDestroyed += OnTimelineDestroyed;
			TimelineServer.TimelineSynchronizer.TimelineSet += OnTimelineSet;

			TimelineServer.Start(true);

            var c = TimelineManager.Default.Get<float>("c");

        }

        void OnServerStarting (int port)
		{
			WriteLine("Queen's University EQUIS Lab");
			WriteLine("Janus Timeline Server started on port {0}", port);
		}

		private void OnPeerConnected (ushort index)
		{
			// InvokeRequired required compares the thread ID of the 
			// calling thread to the thread ID of the creating thread. 
			// If these threads are different, it returns true.
			if (this.messageText.InvokeRequired)
			{
				Invoke(new PeerConnectedHandler(OnPeerConnected), new object[] { index });
			}
			else
			{

				clientGrid.Rows.Add(new object[] { index.ToString() });
				clientGroup.Text = "Clients (" + clientGrid.Rows.Count + ")";

			}
		}

		private void OnPeerUpdated(ushort index, float rtt, int toTLS, int fromTLS)
		{
			// InvokeRequired required compares the thread ID of the 
			// calling thread to the thread ID of the creating thread. 
			// If these threads are different, it returns true.
			if (this.messageText.InvokeRequired)
			{
				Invoke(new PeerUpdatedHandler(OnPeerUpdated), new object[] { index, rtt, toTLS, fromTLS });
			}
			else
			{

				foreach (DataGridViewRow row in clientGrid.Rows)
				{
					if (row.Cells[0].Value.ToString() == index.ToString())
					{
						row.Cells[1].Value = rtt.ToString("F3") + " s";
						row.Cells[2].Value = toTLS.ToString() + " kbps";
						row.Cells[3].Value = fromTLS.ToString() + " kbps";

						break;
					}
				}

			}
		}



		private void OnPeerDisconnected (ushort index)
		{
			if (this.messageText.InvokeRequired)
			{
				Invoke(new PeerDisconnectedHandler(OnPeerDisconnected), new object[] { index });
			}
			else
			{
				foreach (DataGridViewRow row in clientGrid.Rows)
				{
					if (row.Cells[0].Value.ToString() == index.ToString())
					{
						clientGrid.Rows.Remove(row);

						if (clientGrid.Rows.Count > 0) clientGroup.Text = "Clients (" + clientGrid.Rows.Count + ")";
						else clientGroup.Text = "Clients";

						break;
					}
				}
			}
		}

		private void OnTimelineSet(ushort index, byte[] timelineId)
		{
			if (this.messageText.InvokeRequired)
			{
				Invoke(new TimelineSetHandler(OnTimelineSet), new object[] { index, timelineId });
			}
			else
			{
                /*foreach (DataGridViewRow row in timeLineGrid.Rows)
                {
                    if (row.Cells[0].Value.ToString() == Encoding.UTF8.GetString(timelineId))
                    {
                        row.Cells[2].Value = TimelineManager.Default.Get<float>(timelineId)[index];
                        break;
                    }
                }*/
            }
		}


		private void OnTimelineCreated (byte[] timelineId)
		{
			// InvokeRequired required compares the thread ID of the 
			// calling thread to the thread ID of the creating thread. 
			// If these threads are different, it returns true.
			if (this.messageText.InvokeRequired)
			{
				Invoke(new TimelineCreatedHandler(OnTimelineCreated), new object[] { timelineId });
			}
			else
			{
				timeLineGrid.Rows.Add(new object[] { Encoding.UTF8.GetString(timelineId), "1", "null" });
				timeLineGroup.Text = "TimeLines (" + timeLineGrid.Rows.Count + ")";
			}
		}

		private void OnTimelineUpdated(int numConnections, byte[] timelineId)
		{
			// InvokeRequired required compares the thread ID of the 
			// calling thread to the thread ID of the creating thread. 
			// If these threads are different, it returns true.
			if (this.messageText.InvokeRequired)
			{
				Invoke(new TimelineUpdatedHandler(OnTimelineUpdated), new object[] {numConnections,  timelineId });
			}
			else
			{
				foreach (DataGridViewRow row in timeLineGrid.Rows)
				{
					if (row.Cells[0].Value.ToString() == Encoding.UTF8.GetString(timelineId))
					{
						row.Cells[1].Value = numConnections.ToString();
                        break;
					}
				}

			}
		}


		private void OnTimelineDestroyed (byte[] timelineId)
		{
			if (this.messageText.InvokeRequired)
			{
				Invoke(new TimelineDestroyedHandler(OnTimelineDestroyed), new object[] { timelineId });
			}
			else
			{
				foreach (DataGridViewRow row in timeLineGrid.Rows)
				{
					if (row.Cells[0].Value.ToString() == Encoding.UTF8.GetString(timelineId))
					{
						timeLineGrid.Rows.Remove(row);

						if (timeLineGrid.Rows.Count > 0) timeLineGroup.Text = "TimeLines (" + timeLineGrid.Rows.Count + ")";
						else timeLineGroup.Text = "TimeLines";

						break;
					}
				}
			}
		}

		private void AddClient(int id)
		{
			clientGrid.Rows.Add(new object[] { id.ToString() });
			clientGroup.Text = "Clients (" + clientGrid.Rows.Count + ")";
		}

		private void RemoveClient(int id)
		{
			foreach (DataGridViewRow row in clientGrid.Rows)
			{
				if (row.Cells[0].Value.ToString() == id.ToString())
				{
					clientGrid.Rows.Remove(row);

					if (clientGrid.Rows.Count > 0) clientGroup.Text = "Clients (" + clientGrid.Rows.Count + ")";
					else clientGroup.Text = "Clients";

					break;
				}
			}
		}

		private void SetClientRTT(int id, int rtt, long receivedBytes, long sentBytes)
		{
			foreach (DataGridViewRow row in clientGrid.Rows)
			{
				if (row.Cells[0].Value.ToString() == id.ToString())
				{
					row.Cells[1].Value = rtt.ToString() + " ms";
					row.Cells[2].Value = receivedBytes.ToString() + " kbps";
					row.Cells[3].Value = sentBytes.ToString() + " kbps";

					break;
				}
			}
		}

		private void AddTimeline(string name)
		{
			timeLineGrid.Rows.Add(new object[] { name, "1", "9" });
			timeLineGroup.Text = "Timelines (" + timeLineGrid.Rows.Count + ")";
		}

		private void UpdateTimeline(string name, int numConnections)
		{
			foreach (DataGridViewRow row in timeLineGrid.Rows)
			{
				if (row.Cells[0].Value.ToString() == name)
				{
					row.Cells[1].Value = numConnections.ToString();
					break;
				}
			}
		}

		private void RemoveTimeline(string name)
		{
			foreach (DataGridViewRow row in timeLineGrid.Rows)
			{
				if (row.Cells[0].Value.ToString() == name)
				{
					timeLineGrid.Rows.Remove(row);

					if (timeLineGrid.Rows.Count > 0) timeLineGroup.Text = "Timelines (" + timeLineGrid.Rows.Count + ")";
					else timeLineGroup.Text = "Timelines";

					break;
				}
			}
		}

		private void WriteLine()
		{
			WriteLine("");
		}

		private void WriteLine(string line, params object[] values)
		{
			messageText.AppendText(string.Format(line, values) + "\r\n");
		}

		private void OnTimelineCellClick(object sender, DataGridViewCellEventArgs e)
		{

		}

		private void OnFormClosed (object sender, FormClosedEventArgs e)
		{
			TimelineServer.Stop();
		}

        private void ServerForm_Load(object sender, EventArgs e)
        {

        }

    }


}

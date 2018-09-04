using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using Janus;

// Simple telepointer example

namespace Equis.Telepointer
{
	public partial class Form1 : Form
	{
		private const int MAX_CLIENTS = 5;          // maximum number of clients

		Timeline<Position>[] pointerPosition;       // array of timeline objects
		int myClientNumber;                         // client number for local client

		public Form1()
		{
			InitializeComponent();
		}

		// store a new position in the timeline everytime the mouse moves
		private void Form1_MouseMove(object sender, MouseEventArgs mouseEv)
		{
			// set a new value in the position timeline based on the current mouse position
			pointerPosition[myClientNumber][0] = new Position(mouseEv.X, mouseEv.Y);
		}

		// create a timeline for each of the possible clients
		private void Form1_Load(object sender, EventArgs e)
		{

			// start the timeline client using settings in TimelineClient.ini
			TimelineClient.Start(true, true);

			// Wait for connection to be established before continuing
			while (!TimelineClient.IsConnected)
			{
				System.Threading.Thread.Sleep(300);
			}	
			
			// Sets the encoding, decoding and interpolation functions to be used for Position Timelines
			// Note: TypeEncode, TypeDecode, TypeInterpolate and TypeExtrapolate must be set before any timelines are created
			//       Otherwise, default serialization and stepwise interpolations will be used unless
			//       Encode, Decode, Interpolate and Extrapolate are set for each individual timeline object
			Position.SetDefautTimelineFunctions(); 

			pointerPosition = new Timeline<Position>[MAX_CLIENTS];
			for (int i = 0; i < MAX_CLIENTS; i++)
			{
				pointerPosition[i] = TimelineManager.Default.Get<Position>("pointer-" + i);
				pointerPosition[i].MaxEntries = 30;
			}

			// Index numbers are unique and start at 1
			myClientNumber = TimelineClient.Index;  
		}

		// Get the positions of all telepointers and draw them
		private void timer1_Tick(object sender, EventArgs e)
		{
			Graphics gfx = CreateGraphics();
			gfx.Clear(Color.SeaShell);

			Position cursorPosition;
			for (int i = 0; i < MAX_CLIENTS; i++)
			{
				if (pointerPosition[i] != null)
				{
				   if (!pointerPosition[i].IsEmpty) // only draw if there are values in the timeline
				   {
					   // create a tail behind the cursor by getting previous positions 100ms apart
					   for (int j = 1; j <= 10; j++)
					   {
						   cursorPosition = pointerPosition[i][(double)(j *(-0.1))];
						   gfx.DrawRectangle(new Pen(Color.Gray), cursorPosition.x, cursorPosition.y, 5, 5);
					   }
					   // get the current position
					   cursorPosition = pointerPosition[i][0];
					   // make me Red, all others Blue
					   Color tpc = i == myClientNumber ? Color.Red : Color.Blue;
					   gfx.DrawRectangle(new Pen(tpc), cursorPosition.x, cursorPosition.y, 5, 5);
				   }
				}
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Janus;

namespace JanusTest
{
	class Program
	{

		static Timeline<float> testTL;
		static void Main (string[] args)
		{
			TimelineClient.Start(true, true);

			Timeline<float> a = new Timeline<float>("x");
            Timeline<float> a1 = new Timeline<float>("y");
            Timeline<float> a2 = new Timeline<float>("z");
            //a.AddSendFilter(TimelineUtils.BuildDeltaFilter<float>((x, y) => (float)Math.Abs(x-y), 2.0f));


            var b = new Timeline<float>("b", false);
            var c = TimelineManager.Default.Get<float>("c");
            //var c1 = TimelineManager.Default.Get<float>("c1");
            //var c2 = TimelineManager.Default.Get<float>("c2");
            //var c3 = TimelineManager.Default.Get<float>("c3");
            //var c4 = TimelineManager.Default.Get<float>("c4");
            //var c5 = TimelineManager.Default.Get<float>("c5");
            //var c6 = TimelineManager.Default.Get<float>("c6");
            //var c7 = TimelineManager.Default.Get<float>("c7");
            //var c8 = TimelineManager.Default.Get<float>("c8");
            //var c9 = TimelineManager.Default.Get<float>("c8");
            //var ca= TimelineManager.Default.Get<float>("ca");
            //var cb = TimelineManager.Default.Get<float>("cb");


			TimelineManager.Default.Add(b);

			a.EntryInserted += OnEntryInserted;
		    b.EntryInserted += OnEntryInserted;
		    c.EntryInserted += OnEntryInserted;
			Random r = new Random();

			//while (true)
			//{
			//    c[0] = (float) r.NextDouble();
			//    Thread.Sleep(1);
			//}
			while (true)
			{
				string[] tokens = Console.ReadLine().Split();

				if (tokens[0] == "set")
				{
					string id = tokens[1];
					float relTime = float.Parse(tokens[2]);
					float value = float.Parse(tokens[3]);

                    var tManager = TimelineManager.Default.Get<float>(id);
                    var rel = tManager[relTime];


                    TimelineManager.Default.Get<float>(id)[relTime] = value;
				}
				else if (tokens[0] == "get")
				{
					string id = tokens[1];
					float relTime = 0;
					if (tokens.Length > 2)
						relTime = float.Parse(tokens[2]);

					Console.WriteLine(TimelineManager.Default.Get<float>(id)[relTime]);
				}
				else if (tokens[0] == "remove")
				{
					string id = tokens[1];
					Timeline<float> currentTimeline = TimelineManager.Default.Get<float>(id);
					TimelineManager.Default.Remove(currentTimeline);
					Console.WriteLine(currentTimeline.StringID + " removed");
				}
				else if (tokens[0] == "now" || tokens[0] == "time")
				{
					Console.WriteLine(TimelineManager.Default.Now);
				}
				else if (tokens[0] == "quit" || tokens[0] == "exit")
				{
					break;
				}
				else Console.WriteLine("Unknown command.");
			}

			TimelineClient.Stop();
		}

		static void OnEntryInserted (Timeline<float> timeline, TimelineEntry<float> entry)
		{
			Console.WriteLine("Inserted " + timeline.StringID + " " + entry.Time + " " + entry.Value);
			if (timeline.StringID == "a")
			{
				testTL = TimelineManager.Default.Get<float>("Anew");
			}
			else
			{
				TimelineManager.Default.Remove(testTL);
			}
			Console.WriteLine("pass " + timeline.StringID + " " + entry.Time + " " + entry.Value);
		}
	}
}

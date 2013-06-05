using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Xml.Serialization;
using System.Diagnostics;
using System.Net.Sockets;

namespace clientd
{
	class Program
	{
		const int DAEMON_PORT = 37723;
		static Thread watcher;
		static Thread receiver;

		static void TerminateClient()
		{
			foreach (Process p in Process.GetProcessesByName("ST_Client_x32"))
			{
				p.CloseMainWindow();
				while (!p.HasExited)
				{
					Console.WriteLine("wait for exiting...");
					Thread.Sleep(1);
				}
			}
		}

		static void BootClient()
		{
			var si = new ProcessStartInfo();
			si.FileName = @"C:\ST\Bin\ST_Client_x32.exe";
			si.WorkingDirectory = @"C:\ST\Bin\";
			var proc = Process.Start(si);
		}

		static void RebootClient()
		{
			TerminateClient();
			BootClient();
		}

		static void WatcherProc()
		{
			Console.WriteLine("clientd");
			for (;;)
			{
				var si = new ProcessStartInfo();
				si.FileName = @"C:\ST\Bin\client_update.cmd";
				si.WindowStyle = ProcessWindowStyle.Hidden;
				var proc = Process.Start(si);
				Thread.Sleep(2000);
			}
		}

		static void ReceiverProc()
		{
			var udp = new UdpClient(DAEMON_PORT);
			System.Text.Encoding enc = System.Text.Encoding.UTF8;

			for (;;)
			{
				Thread.Sleep(1);
				
				System.Net.IPEndPoint remoteEP = null;
				byte[] bytes = udp.Receive(ref remoteEP);
				string msg = enc.GetString(bytes);
				foreach (string s in msg.Split(new char[]{'\n'}))
				{
					if (s.Length>0 && s[0]=='#')
					{
						Console.WriteLine("msg {0}", s);
						if (s=="#REBOOT")
						{
							RebootClient();
						}
						if (s=="#BOOT")
						{
							BootClient();
						}
						else if (s=="#TERMINATE")
						{
							TerminateClient();
						}
					}
				}				
			}
		}

		static void Main(string[] args)
		{
			watcher = new Thread(WatcherProc);
			watcher.Start();
			receiver = new Thread(ReceiverProc);
			receiver.Start();
		}
	}
}

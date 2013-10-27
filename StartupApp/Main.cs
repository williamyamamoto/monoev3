using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Diagnostics;
using MonoBrickFirmware.Graphics;
using MonoBrickFirmware.IO;
using System.Reflection;
using System.Collections.Generic;
using MonoBrickFirmware.Native;
using System.Linq;

namespace StartupApp
{
		
	
	class MainClass
	{
		static Bitmap monoLogo = Bitmap.FromResouce(Assembly.GetExecutingAssembly(), "monologo.bitmap");
		static Font font = Font.FromResource(Assembly.GetExecutingAssembly(), "info56_12.font");
		static string AppToStart = null;
		
		public static string GetIpAddress()
		{
			NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
			foreach (var ni in interfaces)
			{
				foreach (var addr in ni.GetIPProperties().UnicastAddresses)
				{
					if (addr.Address.ToString() != "127.0.0.1")
						return addr.Address.ToString();					
				}
			}
			return "Unknown";
		}
		
		static bool Information(Lcd lcd, Buttons btns)
		{
			string monoVersion = "Unknown";
			Type type = Type.GetType("Mono.Runtime");
			if (type != null)
			{                                          
	    		MethodInfo displayName = type.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static); 
	    		if (displayName != null)                   
	        		monoVersion = (string)displayName.Invoke(null, null); 
			}	
			string monoCLR = System.Reflection.Assembly.GetExecutingAssembly().ImageRuntimeVersion;
			
			Point offset = new Point(0, (int)font.maxHeight);
			Point startPos = new Point(0,0);
			lcd.Clear();
			lcd.WriteText(font, startPos+offset*0, "MonoBrickFirmware:", true);
			lcd.WriteText(font, startPos+offset*1, "0.1.0.0", true);
			lcd.WriteText(font, startPos+offset*2, "Mono version:", true);
			lcd.WriteText(font, startPos+offset*3, monoVersion, true);
			lcd.WriteText(font, startPos+offset*4, "Mono CLR:" + monoCLR, true);			
			lcd.Update();
			btns.GetKeypress();
			return false;
		}
		
		static bool StartApp(string filename)
		{
			AppToStart = filename;
			return true;
		}
		
		static string GetFileNameWithoutExt(string fullname)
		{
			string filename = new FileInfo(fullname).Name;
			return filename.Substring(0, filename.Length-4);
		}
		
		static bool RunPrograms(Lcd lcd, Buttons btns)
		{
			IEnumerable<MenuItem> items = Directory.EnumerateFiles("/home/root/apps/", "*.exe")
				.Select( (filename) => new MenuItem() { text = GetFileNameWithoutExt(filename), action = () => StartApp(filename) } );
			Menu m = new Menu(font, lcd, "Run program:", items);
			m.ShowMenu(btns);
			return true;
		}
		
		static void RunAndWaitForProgram(string filename, string arguments = "")
		{
			System.Diagnostics.Process proc = new System.Diagnostics.Process();
			proc.EnableRaisingEvents=false; 
			proc.StartInfo.FileName = filename;
			proc.StartInfo.Arguments = arguments;
			proc.Start();
			proc.WaitForExit();
		}
		
		static bool Shutdown(Lcd lcd, Buttons btns)
		{
			lcd.Clear();
			lcd.WriteText(font, new Point(0,0), "Shutting down...", true);
			lcd.Update();
			
			UnixDevice dev = new UnixDevice("/dev/lms_power");
			dev.IoCtl(0, new byte[0]);
			btns.LedPattern(2);
			RunAndWaitForProgram("poweroff", "-f");
			for (;;); // The system should now shutdown.			
		}
		
		static void ShowMainMenu(Lcd lcd, Buttons btns)
		{
			
			List<MenuItem> items = new List<MenuItem>();
			items.Add (new MenuItem() { text = "Information", action = () => Information(lcd, btns) });
			items.Add (new MenuItem() { text = "Run programs", action = () => RunPrograms(lcd, btns) });
			items.Add (new MenuItem() { text = "Shutdown", action = () => Shutdown(lcd,btns) });			
			
			Menu m = new Menu(font, lcd, "Main menu", items);
			m.ShowMenu(btns);
		}
		
		public static void Main (string[] args)
		{
			Console.WriteLine ("Hello World!");
			using (Lcd lcd = new Lcd())
				using (Buttons btns = new Buttons())
				{					
					lcd.DrawBitmap(monoLogo, new Point((int)(Lcd.Width-monoLogo.Width)/2,0));					
					string iptext = "IP: " + GetIpAddress();
					Point textPos = new Point((Lcd.Width-font.TextSize(iptext).x)/2, Lcd.Height-23);
					lcd.WriteText(font, textPos, iptext , true);
					lcd.Update();						
					btns.GetKeypress();
				}
			for (;;)
			{
				using (Lcd lcd = new Lcd())
					using (Buttons btns = new Buttons())
					{
						ShowMainMenu(lcd, btns);					
					}			
				if (AppToStart != null)
				{
					Console.WriteLine("Starting application: " + AppToStart);
					RunAndWaitForProgram("mono", AppToStart);					
					Console.WriteLine ("Done running application");
					AppToStart = null;
				}
			}
		}
	}		
}

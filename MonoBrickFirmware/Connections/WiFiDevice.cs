namespace MonoBrickFirmware.Connections
{
	
	public static class WiFiDevice
	{
		static WiFiDevice()
		{
			try
			{
				Instance = new EV3WiFiDevice();	
			}	
			catch
			{
				Instance = null;
			}
		}
		internal static IWiFiDevice Instance { get; set;}
		public static bool TurnOn (string ssid, string password, bool useEncryption, int timeout = 0){return Instance.TurnOn (ssid, password, useEncryption, timeout);}
		public static void TurnOff (){Instance.TurnOff ();}
		public static bool IsLinkUp (){return Instance.IsLinkUp ();}
		public static string Gateway (){return Instance.Gateway ();}
		public static string GetIpAddress(){return Instance.GetIpAddress ();}
	}
}


using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceMonitor
{
    

	public class Connection_log
	{

		public long id { set; get; }
		public string? name { set; get; }
		public string? stop_reason { set; get; }	
		//public string? last_disconnect_time { set; get; }	
		public DateTime? start_time { set; get; }
		public DateTime? stop_time { set; get; }

		public int? current_status { set; get; }
		

	}
}

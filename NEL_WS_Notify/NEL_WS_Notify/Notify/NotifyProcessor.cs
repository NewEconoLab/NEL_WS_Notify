using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace NEL_WS_Notify.Notify
{
    public class NotifyProcessor
    {
        private static DataDetectHandler ddHdl = new DataDetectHandler();

        /// <summary>
        ///  
        /// 初始化WS连接
        ///  
        /// </summary>
        /// <param name="context"></param>
        /// <param name="ws"></param>
        /// <param name="network"></param>
        /// <returns></returns>
        public static async Task initWsProcessor(HttpContext context, WebSocket ws, string network = "testnet")
        {
            var hdl = new WebSocketHandler(context, ws, network);
            ddHdl.HasChanged(network, true, out string message);
            await hdl.onConnected(message);
        }

        /// <summary>
        /// 
        /// 监控线程
        /// 
        /// </summary>
        public static void loop()
        {
            while(true)
            {
                var now = DateTime.Now;
                Thread.Sleep(1000 * WsConst.loop_interval_seconds);
                
                foreach (var network in WsConst.networks)
                {
                    try
                    {
                        var sessions = WebSocketHandler.socketDict.Values.Where(p => p.network == network).ToList();
                        if (sessions != null && sessions.Count > 0 && ddHdl.HasChanged(network, false, out string message))
                        {
                            WebSocketHandler.publish(message, now, sessions);
                            Console.WriteLine("{0} wsService: network={1}, count={2}", now, network, sessions.Count);
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("{0} wsService: Error={1}, Stack={2}", now, ex.Message, ex.StackTrace);
                    }
                        
                }
               
            }
        }

       
        public static void ping()
        {
            while(true)
            {
                Thread.Sleep(1000);
                var sessions = WebSocketHandler.socketDict.Values.ToList();
                if (sessions.Count > 0)
                {
                    sessions.ForEach(async (p) => await p.ping());
                }
            }
            
        }
    }

    public class WsConst
    {
        public static string block_mongodbConnStr_testnet = "";
        public static string block_mongodbDatabase_testnet = "";
        public static string block_mongodbConnStr_mainnet = "";
        public static string block_mongodbDatabase_mainnet = "";

        public static string[] networks = new string[] { "testnet", "mainnet" };

        public static int ping_interval_minutes = 2;   // 单位: 分钟
        public static int loop_interval_seconds = 2;   // 单位: 秒

    }
}

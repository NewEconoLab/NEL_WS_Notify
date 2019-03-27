using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NEL_WS_Notify.Notify
{
    /// <summary>
    /// 
    /// 连接处理器
    /// 
    /// </summary>
    public class WebSocketHandler
    {
        public static Dictionary<UInt32, WebSocketHandler> socketDict = new Dictionary<uint, WebSocketHandler>();
        public static UInt32 sessionId = 0;

        private HttpContext context;
        private WebSocket ws;
        public UInt32 id { get; }
        private long lastSendMunite = 0;
        public string network { get; }

        public WebSocketHandler(HttpContext context, WebSocket ws, string network)
        {
            this.context = context;
            this.ws = ws;
            id = ++sessionId;
            this.network = network;
        }
        
        public string LogInfo => new JObject() { { "id", id } }.ToString();
        public string PingInfo => new JObject() { { "time", DateTime.Now.ToString("u") } }.ToString();
        private long getNowTimeMunite => DateTime.Now.Minute;

        /// <summary>
        /// 
        /// 已建立连接
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task onConnected(string message = "")
        {
            LogHelper.log(" connection opened, id:{0}", id);
            socketDict.Add(id, this);
            try
            {
                await sendMessageAsync(Message.MakeMessage(Message.Type.LogIn, LogInfo));
                if (message != "") await sendMessageAsync(Message.MakeMessage(Message.Type.Notify, message));
            } catch
            {
                await onDisConnect();
            }
            while(true)
            {
                Thread.Sleep(2000);
            }
        }

        /// <summary>
        /// 
        /// 已断开连接
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task onDisConnect()
        {
            LogHelper.log(" connection closed, id:{0}", id);
            socketDict.Remove(id);
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "close", CancellationToken.None);
        }

        /// <summary>
        /// 
        /// 心跳消息
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task ping()
        {
            if (lastSendMunite + WsConst.ping_interval_minutes > getNowTimeMunite) return;
            try
            {
                await sendMessageAsync(Message.MakeMessage(Message.Type.Ping, PingInfo));
            }
            catch
            {
                await onDisConnect();
            }
        }


        /// <summary>
        /// 
        /// 接收消息
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task<string> recvMessageAsync()
        {
            using (var ms = new MemoryStream())
            {
                WebSocketReceiveResult result;
                do
                {
                    var buf = new ArraySegment<byte>(new byte[1024]);
                    result = await ws.ReceiveAsync(buf, CancellationToken.None);
                    ms.Write(buf.Array, buf.Offset, result.Count);

                } while (!result.EndOfMessage);
                //
                using (var rd = new StreamReader(ms, Encoding.UTF8))
                {
                    return await rd.ReadToEndAsync();
                }
            }
        }


        /// <summary>
        /// 
        /// 发送消息
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task sendMessageAsync(string message)
        {
            lastSendMunite = getNowTimeMunite;
            await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)), WebSocketMessageType.Text, true, CancellationToken.None);

        }


        /// <summary>
        /// 
        /// 发布消息
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="sessions"></param>
        public static async void publish(string message, DateTime now, List<WebSocketHandler> list)
        {
            //
            LogHelper.log("{0} publish message:{1}", now, message);
            foreach (var session in list)
            {
                try
                {
                    await session.sendMessageAsync(Message.MakeMessage(Message.Type.Notify, message));
                }
                catch
                {
                    await session.onDisConnect();
                }
            }
        }
    }
    
    class LogHelper
    {
        public static void log(string format, object o1=null, object o2=null, object o3=null)
        {
            Console.WriteLine(format, o1, o2, o3);
        }
    }
    
    class Message
    {
        public enum Type { LogIn, Ping, Notify }

        public static string MakeMessage(Type type, string data)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(new JObject() {
                    {"type", type.ToString() },
                    {"data", JObject.Parse(data)}
                });
        }
    }
}

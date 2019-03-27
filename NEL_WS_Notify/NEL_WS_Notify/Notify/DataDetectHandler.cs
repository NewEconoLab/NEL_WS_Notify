using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace NEL_WS_Notify.Notify
{
    /// <summary>
    /// 
    /// 数据监测处理器
    /// 
    /// </summary>
    public class DataDetectHandler
    {
        /// <summary>
        /// 
        /// 数据变动监测
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool HasChanged(string network, bool first, out string message)
        {
            var a = dictInfo.GetValueOrDefault(network);
            if (a == null)
            {
                a = new A
                {
                    network = network,
                    blockHeight = 0,
                    blockTime = 0,
                    blockHash = ""
                };
            }
            bool flag = false;
            var res = new JObject() { { "network", network } };
            var newdata = getBlockAndNotifyCount(network);
            if(a.blockHeight < newdata.blockHeight || first)
            {
                res.Add("blockHeight", newdata.blockHeight);
                res.Add("blockTime", newdata.blockTime);
                res.Add("blockHash", newdata.blockHash);
                flag = true;
            } else
            {
                flag = false;
            }
            dictInfo.Remove(network);
            dictInfo.Add(newdata.network, newdata);

            message = res.ToString();
            return flag;
        }
        private class A
        {
            public string network { get; set; }
            public long blockHeight { get; set; }
            public long blockTime { get; set; }
            public string blockHash { get; set; }
        }
        private Dictionary<string, A> dictInfo = new Dictionary<string, A>();
        private A getBlockAndNotifyCount(string network)
        {
            bool flag = network == "mainnet";
            string mongodbConnStr =  flag ? WsConst.block_mongodbConnStr_mainnet : WsConst.block_mongodbConnStr_testnet;
            string mongodbDatabase = flag ? WsConst.block_mongodbDatabase_mainnet : WsConst.block_mongodbDatabase_testnet;

            //
            var client = new MongoClient(mongodbConnStr);
            var database = client.GetDatabase(mongodbDatabase);
            var collection = database.GetCollection<BsonDocument>("system_counter");
            string findStr = new JObject() { { "counter", "block" } }.ToString();
            var res = collection.Find(findStr).ToList();

            var a = new A
            {
                network = network,
                blockHeight = 0,
                blockTime = 0,
                blockHash = ""
            };
            if (res == null || res.Count == 0)
            {
                return a;
            }
            var index = (int)res[0]["lastBlockindex"];
            findStr = new JObject() { {"index", index } }.ToString();
            string fieldStr = new JObject() { {"index",1 }, { "time", 1 }, { "hash", 1 } }.ToString();
            collection = database.GetCollection<BsonDocument>("block");
            res = collection.Find(findStr).Project(fieldStr).ToList();
            if(res != null && res.Count > 0)
            {
                a.blockHeight = index;
                a.blockTime = long.Parse(res[0]["time"].ToString());
                a.blockHash = res[0]["hash"].ToString();
            }
            return a;
        }
    }
    
}

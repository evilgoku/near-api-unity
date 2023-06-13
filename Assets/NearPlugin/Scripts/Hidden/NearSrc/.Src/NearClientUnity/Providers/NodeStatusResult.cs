using Newtonsoft.Json.Linq;

namespace NearClientUnity.Providers
{
    public class NodeStatusResult
    {
        public string ChainId { get; set; }
        public string RpcAddr { get; set; }
        public SyncInfo SyncInfo { get; set; }
        public JArray Validators { get; set; }

        public static NodeStatusResult FromDynamicJsonObject(object jsonObject)
        {
            var jsonObj = (Newtonsoft.Json.Linq.JObject)jsonObject;

            var result = new NodeStatusResult()
            {
                ChainId = jsonObj["chain_id"].ToObject<string>(),
                RpcAddr = jsonObj["rpc_addr"].ToObject<string>(),
                SyncInfo = new SyncInfo()
                {
                    LatestBlockHash = jsonObj["sync_info"]["latest_block_hash"].ToObject<string>(),
                    LatestBlockHeight = jsonObj["sync_info"]["latest_block_height"].ToObject<int>(),
                    LatestBlockTime = jsonObj["sync_info"]["latest_block_time"].ToObject<string>(),
                    LatestStateRoot = jsonObj["sync_info"]["latest_state_root"].ToObject<string>(),
                    Syncing = jsonObj["sync_info"]["syncing"].ToObject<bool>()
                },
                Validators = jsonObj["validators"] as JArray
            };
            return result;
        }
    }
}
using NearClientUnity.Utilities;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace NearClientUnity.Providers
{
    public class JsonRpcProvider : Provider
    {
        private readonly ConnectionInfo _connection;

        public JsonRpcProvider(string url)
        {
            var connectionInfo = new ConnectionInfo
            {
                Url = url
            };
            _connection = connectionInfo;
        }

        private int _id { get; set; } = 123;

        public override async Task<BlockResult> GetBlockAsync(int blockId)
        {
            var parameters = new object[] { blockId };
            var result = await SendJsonRpc<BlockResult>("block", parameters);
            return result;
        }

        public override async Task<ChunkResult> GetChunkAsync(string chunkId)
        {
            var parameters = new object[] { chunkId };
            var result = await SendJsonRpc<ChunkResult>("chunk", parameters);
            return result;
        }

        public override Task<ChunkResult> GetChunkAsync(int[,] chunkId)
        {
            throw new NotImplementedException();
        }

        public override INetwork GetNetwork()
        {
            INetwork result = null;
            result.Name = "test";
            result.ChainId = "test";
            return result;
        }

        public override async Task<NodeStatusResult> GetStatusAsync()
        {
            var rawStatusResult = await SendJsonRpc<object>("status", new object[0]);
            var result = NodeStatusResult.FromDynamicJsonObject(rawStatusResult);
            return result;
        }

        public override async Task<FinalExecutionOutcome> GetTxStatusAsync(byte[] txHash, string accountId)
        {
            var parameters = new object[] { Base58.Encode(txHash), accountId };
            var result = await SendJsonRpc<FinalExecutionOutcome>("tx", parameters);
            return result;
        }

        public override async Task<T> QueryAsync<T>(string path, string data)
        {
            var parameters = new object[] { path, data };

            try
            {
                var result = await SendJsonRpc<T>("query", parameters);
                return result;
            }
            catch (Exception e)
            {
                throw new Exception($"Quering {path} failed: {e.Message}.");
            }
        }

        public override async Task<T> SendTransactionAsync<T>(SignedTransaction signedTransaction)
        {
            var bytes = signedTransaction.ToByteArray();
            var parameters = new object[] { Convert.ToBase64String(bytes, 0, bytes.Length) };
            var rawOutcomeResult = await SendJsonRpc<T>("broadcast_tx_commit", parameters);
            var result = rawOutcomeResult;
            return result;
        }

        private async Task<T> SendJsonRpc<T>(string method, object[] parameters)
        {
            var request = new
            {
                method = method,
                @params = parameters,
                id = _id++,
                jsonrpc = "2.0"
            };
            
            var requestString = JsonConvert.SerializeObject(request);
            
            try
            {
                var result = await Web.FetchJsonAsync<T>(_connection, requestString);
                return result;
            }
            catch (Exception e)
            {
                throw new Exception(e.ToString());
            }
        }
    }
}

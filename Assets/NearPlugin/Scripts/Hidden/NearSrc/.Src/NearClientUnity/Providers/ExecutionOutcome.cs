using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace NearClientUnity.Providers
{
    public class ExecutionOutcome
    {
        public int GasBurnt { get; set; }
        public string[] Logs { get; set; }
        public string[] ReceiptIds { get; set; }
        public ExecutionStatus Status { get; set; }
        public ExecutionStatusBasic StatusBasic { get; set; }

        public static ExecutionOutcome FromDynamicJsonObject(object jsonObject)
        {
            var jsonObj = (JObject)jsonObject;

            var logs = jsonObj["logs"].ToObject<string[]>();
            var receiptIds = jsonObj["receipt_ids"].ToObject<string[]>();

            var result = new ExecutionOutcome()
            {
                GasBurnt = jsonObj["gas_burnt"].ToObject<int>(),
                Logs = logs,
                ReceiptIds = receiptIds,
                Status = ExecutionStatus.FromDynamicJsonObject(jsonObj["status"]),
            };

            return result;
        }
    }
}
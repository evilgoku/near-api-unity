using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace NearClientUnity.Providers
{
    public class FinalExecutionOutcome
    {
        public ExecutionOutcomeWithId[] Receipts { get; set; }
        public FinalExecutionStatus Status { get; set; }
        public FinalExecutionStatusBasic StatusBasic { get; set; }
        public ExecutionOutcomeWithId Transaction { get; set; }

        public static FinalExecutionOutcome FromDynamicJsonObject(object jsonObject)
        {
            var jObject = (JObject)jsonObject;
            var receipts = new List<ExecutionOutcomeWithId>();
            foreach (var receipt in jObject["receipts"])
            {
                receipts.Add(ExecutionOutcomeWithId.FromDynamicJsonObject(receipt));
            }
            var result = new FinalExecutionOutcome()
            {
                Receipts = receipts.ToArray(),
                Status = FinalExecutionStatus.FromDynamicJsonObject(jObject["status"]),
                Transaction = ExecutionOutcomeWithId.FromDynamicJsonObject(jObject["transaction"])
            };
            return result;
        }
    }
}
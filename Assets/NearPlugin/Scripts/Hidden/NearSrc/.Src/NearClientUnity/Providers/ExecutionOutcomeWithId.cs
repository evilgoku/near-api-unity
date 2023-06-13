using Newtonsoft.Json.Linq;

namespace NearClientUnity.Providers
{
    public class ExecutionOutcomeWithId
    {
        public string Id { get; set; }
        public ExecutionOutcome Outcome { get; set; }

        public static ExecutionOutcomeWithId FromDynamicJsonObject(object jsonObject)
        {
            var jsonObj = (JObject)jsonObject;
            var result = new ExecutionOutcomeWithId()
            {
                Id = jsonObj["id"].ToString(),
                Outcome = ExecutionOutcome.FromDynamicJsonObject(jsonObj["outcome"]),
            };
            return result;
        }
    }
}
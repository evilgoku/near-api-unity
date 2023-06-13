using Newtonsoft.Json.Linq;

namespace NearClientUnity.Providers
{
    public class FinalExecutionStatus
    {
        public ExecutionError Failure { get; set; }
        public string SuccessValue { get; set; }

        public static FinalExecutionStatus FromDynamicJsonObject(object jsonObject)
        {
            var jsonObj = (JObject)jsonObject;
            var isFailure = jsonObj["Failure"] != null;

            if (isFailure)
            {
                return new FinalExecutionStatus()
                {
                    Failure = ExecutionError.FromDynamicJsonObject(jsonObj["Failure"]),
                };
            }
            return new FinalExecutionStatus()
            {
                SuccessValue = jsonObj["SuccessValue"].ToString()
            };
        }
    }
}
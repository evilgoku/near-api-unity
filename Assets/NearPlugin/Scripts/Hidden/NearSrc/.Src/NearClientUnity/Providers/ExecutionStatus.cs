namespace NearClientUnity.Providers
{
    public class ExecutionStatus
    {
        public ExecutionError Failure { get; set; }
        public string SuccessReceiptId { get; set; }
        public string SuccessValue { get; set; }

        public static ExecutionStatus FromDynamicJsonObject(object jsonObject)
        {
            var jsonObj = (Newtonsoft.Json.Linq.JObject)jsonObject;

            if (jsonObj.ToString() == "Unknown")
            {
                return new ExecutionStatus();
            }

            var isFailure = jsonObj["Failure"] != null;

            if (isFailure)
            {
                return new ExecutionStatus()
                {
                    Failure = ExecutionError.FromDynamicJsonObject(jsonObj["Failure"]),
                };
            }

            return new ExecutionStatus()
            {
                SuccessReceiptId = jsonObj["SuccessReceiptId"].ToObject<string>(),
                SuccessValue = jsonObj["SuccessValue"].ToObject<string>(),
            };
        }
    }
}
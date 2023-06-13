using Newtonsoft.Json.Linq;

namespace NearClientUnity.Providers
{
    public class ExecutionError
    {
        public string ErrorMessage { get; set; }
        public string ErrorType { get; set; }

        public static ExecutionError FromDynamicJsonObject(object jsonObject)
        {
            var jsonObj = (JObject)jsonObject;
            var result = new ExecutionError()
            {
                ErrorMessage = jsonObj["error_message"].ToString(),
                ErrorType = jsonObj["error_type"].ToString()
            };
            return result;
        }
    }
}
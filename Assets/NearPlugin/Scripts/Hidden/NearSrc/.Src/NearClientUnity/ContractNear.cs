using NearClientUnity.Providers;
using NearClientUnity.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace NearClientUnity
{
    public class ContractNear
    {
        private readonly Account _account;
        private readonly string _contractId;
        private readonly string[] _availableChangeMethods;
        private readonly string[] _availableViewMethods;

        public ContractNear(Account account, string contractId, ContractOptions options)
        {
            _account = account;
            _contractId = contractId;
            _availableViewMethods = options.viewMethods;
            _availableChangeMethods = options.changeMethods;
        }

        public async Task<object> Change(string methodName, object args, ulong? gas = null, Nullable<UInt128> amount = null)
        {
            var rawResult = await _account.FunctionCallAsync(_contractId, methodName, args, gas, amount);
            return rawResult;
        }

        public bool TryInvokeMember(string methodName, object[] args, out object result)
        {
            if (Array.Exists(_availableChangeMethods, changeMethod => changeMethod == methodName))
            {
                if (args.Length == 0)
                {
                    result = Change(methodName, null);
                    return true;
                }
                else if (args.Length == 1 && args[0].GetType() == typeof(object))
                {
                    result = Change(methodName, args[0]);
                    return true;
                }
                else if (args.Length == 2 && args[0].GetType() == typeof(object) && args[1].GetType() == typeof(ulong))
                {
                    result = Change(methodName, args[0], Convert.ToUInt64(args[1]));
                    return true;
                }
                else if (args.Length == 3 && args[0].GetType() == typeof(object) && args[1].GetType() == typeof(ulong) && args[2].GetType() == typeof(UInt128))
                {
                    result = Change(methodName, args[0], Convert.ToUInt64(args[1]), (UInt128)args[2]);
                    return true;
                }
            }
            else if (Array.Exists(_availableViewMethods, viewMethod => viewMethod == methodName))
            {
                if (args.Length == 0)
                {
                    result = View(methodName, null);
                    return true;
                }
                else if (args.Length == 1 && args[0].GetType() == typeof(object))
                {
                    result = View(methodName, args[0]);
                    return true;
                }
            }

            result = null;
            return false;
        }

        public async Task<object> View(string methodName, object args)
        {
            var rawResult = await _account.ViewFunctionAsync(_contractId, methodName, args);
            var rawResultJson = JObject.FromObject(rawResult);
    
            var logs = rawResultJson["logs"].ToObject<string[]>();
            var resultBytes = rawResultJson["result"].ToObject<byte[]>();
            var resultString = Encoding.UTF8.GetString(resultBytes).Trim('"');
    
            var data = new
            {
                logs = logs,
                result = resultString
            };
    
            return data;
        }


    }
}

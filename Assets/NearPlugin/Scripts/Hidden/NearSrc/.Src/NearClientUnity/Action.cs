using NearClientUnity.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace NearClientUnity
{
    public class Action
    {
        private readonly object _args;
        private readonly ActionType _type;

        public Action(ActionType type, object args)
        {
            _type = type;
            _args = args;
        }

        public object Args => _args;

        public ActionType Type => _type;

        public static Action AddKey(PublicKey publicKey, AccessKey accessKey)
        {
            var args = new
            {
                PublicKey = publicKey,
                AccessKey = accessKey
            };
            return new Action(ActionType.AddKey, args);
        }

        public static Action CreateAccount()
        {
            return new Action(ActionType.CreateAccount, null);
        }

        public static Action DeleteAccount(string beneficiaryId)
        {
            var args = new
            {
                BeneficiaryId = beneficiaryId
            };
            return new Action(ActionType.DeleteAccount, args);
        }

        public static Action DeleteKey(PublicKey publicKey)
        {
            var args = new
            {
                PublicKey = publicKey
            };
            return new Action(ActionType.DeleteKey, args);
        }

        public static Action DeployContract(byte[] code)
        {
            var args = new
            {
                Code = code
            };
            return new Action(ActionType.DeployContract, args);
        }

        public static Action FromByteArray(byte[] rawBytes)
        {
            using (var ms = new MemoryStream(rawBytes))
            {
                return FromStream(ms);
            }
        }

        public static Action FromStream(MemoryStream stream)
        {
            return FromRawDataStream(stream);
        }

        public static Action FromStream(ref MemoryStream stream)
        {
            return FromRawDataStream(stream);
        }

        public static Action FunctionCall(string methodName, byte[] methodArgs, ulong? gas, UInt128 deposit)
        {
            var args = new
            {
                MethodName = methodName,
                MethodArgs = methodArgs,
                Gas = gas,
                Deposit = deposit
            };
            return new Action(ActionType.FunctionCall, args);
        }


        public static Action Stake(UInt128 stake, PublicKey publicKey)
        {
            var args = new
            {
                Stake = stake,
                PublicKey = publicKey
            };
            return new Action(ActionType.Stake, args);
        }


        public static Action Transfer(UInt128 deposit)
        {
            var args = new
            {
                Deposit = deposit
            };
            return new Action(ActionType.Transfer, args);
        }


        public byte[] ToByteArray()
        {
            using (var ms = new MemoryStream())
            {
                using (var writer = new NearBinaryWriter(ms))
                {
                    writer.Write((byte)_type);

                    var argsDict = JObject.FromObject(_args);

                    switch (_type)
                    {
                        case ActionType.AddKey:
                            {
                                writer.Write((argsDict["PublicKey"].ToObject<PublicKey>()).ToByteArray());
                                writer.Write((argsDict["AccessKey"].ToObject<AccessKey>()).ToByteArray());
                                break;
                            }
                        case ActionType.DeleteKey:
                            {
                                writer.Write((argsDict["PublicKey"].ToObject<PublicKey>()).ToByteArray());
                                break;
                            }
                        case ActionType.CreateAccount:
                            {
                                break;
                            }
                        case ActionType.DeleteAccount:
                            {
                                writer.Write((string)argsDict["BeneficiaryId"]);
                                break;
                            }
                        case ActionType.DeployContract:
                            {
                                var codeBytes = (byte[])argsDict["Code"];
                                writer.Write((uint)codeBytes.Length);
                                writer.Write(codeBytes);
                                break;
                            }
                        case ActionType.FunctionCall:
                            {
                                var methodName = (string)argsDict["MethodName"];
                                var methodArgs = (byte[])argsDict["MethodArgs"];
                                var gas = (ulong?)argsDict["Gas"] ?? 0;
                                UInt128 deposit;
                                try
                                {
                                    deposit = _args.GetType().GetProperty("Deposit")?.GetValue(_args) is UInt128
                                        ? (UInt128)_args.GetType().GetProperty("Deposit")?.GetValue(_args)
                                        : default;
                                }
                                catch
                                {
                                    deposit = UInt128.Zero;
                                }


                                writer.Write(methodName);
                                writer.Write((uint)methodArgs.Length);
                                writer.Write(methodArgs);
                                writer.Write(gas);
                                writer.Write(deposit);
                                break;
                            }
                        case ActionType.Stake:
                            {
                                writer.Write(argsDict["Stake"].ToObject<UInt128>());
                                writer.Write((argsDict["PublicKey"].ToObject<PublicKey>()).ToByteArray());
                                break;
                            }
                        case ActionType.Transfer:
                            {
                                writer.Write(argsDict["Deposit"].ToObject<UInt128>());
                                break;
                            }
                        default:
                            throw new NotSupportedException("Unsupported action type");
                    }

                    return ms.ToArray();
                }
            }
        }



        private static Action FromRawDataStream(MemoryStream stream)
        {
            using (var reader = new NearBinaryReader(stream, true))
            {
                var actionType = (ActionType)reader.ReadByte();

                switch (actionType)
                {
                    case ActionType.AddKey:
                        {
                            var args = new
                            {
                                PublicKey = PublicKey.FromStream(ref stream),
                                AccessKey = AccessKey.FromStream(ref stream)
                            };
                            return new Action(ActionType.AddKey, args);
                        }
                    case ActionType.DeleteKey:
                        {
                            var args = new
                            {
                                PublicKey = PublicKey.FromStream(ref stream)
                            };
                            return new Action(ActionType.DeleteKey, args);
                        }
                    case ActionType.CreateAccount:
                        {
                            return new Action(ActionType.CreateAccount, null);
                        }
                    case ActionType.DeleteAccount:
                        {
                            var args = new
                            {
                                BeneficiaryId = reader.ReadString()
                            };
                            return new Action(ActionType.DeleteAccount, args);
                        }
                    case ActionType.DeployContract:
                        {
                            var byteCount = reader.ReadUInt();

                            var code = new List<byte>();

                            for (var i = 0; i < byteCount; i++)
                            {
                                code.Add(reader.ReadByte());
                            }

                            var args = new
                            {
                                Code = code.ToArray()
                            };
                            return new Action(ActionType.DeployContract, args);
                        }
                    case ActionType.FunctionCall:
                        {
                            var methodName = reader.ReadString();

                            var methodArgsCount = reader.ReadUInt();

                            var methodArgs = new List<byte>();

                            for (var i = 0; i < methodArgsCount; i++)
                            {
                                methodArgs.Add(reader.ReadByte());
                            }

                            var gas = reader.ReadULong();

                            var deposit = reader.ReadUInt128();

                            var args = new
                            {
                                MethodName = methodName,
                                MethodArgs = methodArgs.ToArray(),
                                Gas = gas,
                                Deposit = deposit
                            };
                            return new Action(ActionType.FunctionCall, args);
                        }
                    case ActionType.Stake:
                        {
                            var stake = reader.ReadUInt128();

                            var publicKey = PublicKey.FromStream(ref stream);

                            var args = new
                            {
                                Stake = stake,
                                PublicKey = publicKey
                            };
                            return new Action(ActionType.Stake, args);
                        }
                    case ActionType.Transfer:
                        {
                            var deposit = reader.ReadUInt128();

                            var args = new
                            {
                                Deposit = deposit
                            };
                            return new Action(ActionType.Transfer, args);
                        }
                    default:
                        throw new NotSupportedException("Unsupported action type");
                }
            }
        }
        
        private static UInt128 ParseUInt128(string value)
        {
            // Assuming the format of the UInt128 value is correct
            // Extract the individual parts of the value
            var parts = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                throw new FormatException("Invalid UInt128 format");
            }

            var s0 = ulong.Parse(parts[0].Trim());
            var s1 = ulong.Parse(parts[1].Trim());

            return new UInt128(s0, s1);
        }
    
    }
}
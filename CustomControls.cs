using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using Ingame = Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;

namespace Jimmacle.Antennas
{
    public static class CustomControls
    {
        public static IMyTerminalAction SendAction<T>() where T : Ingame.IMyTerminalBlock
        {
            var sendAction = MyAPIGateway.TerminalControls.CreateAction<T>("Send");
            sendAction.Name = new StringBuilder("Send Message");
            sendAction.Icon = @"Textures\GUI\Icons\Actions\Reverse.dds";
            sendAction.Action = b => Core.RequestBroadcast(BitConverter.GetBytes(b.EntityId));
            return sendAction;
        }

        public static IMyTerminalAction ClearQueue<T>() where T : Ingame.IMyTerminalBlock
        {
            var clearQueue = MyAPIGateway.TerminalControls.CreateAction<T>("ClearIncoming");
            clearQueue.Name = new StringBuilder("Clear Incoming Messages");
            clearQueue.Icon = @"Textures\GUI\Icons\Actions\Reset.dds";
            clearQueue.Action = b => Config.GetProperties(b.EntityId).ClearQueue();
            return clearQueue;
        }

        public static IMyTerminalControlProperty<int> ChannelProp<T>() where T : Ingame.IMyTerminalBlock
        {
            var channelProp = MyAPIGateway.TerminalControls.CreateProperty<int, T>("Channel");
            channelProp.Getter = b => Config.GetProperties(b.EntityId).Channel;
            channelProp.Setter = (b, v) =>
            {
                v = Math.Min(65535, v);
                v = Math.Max(0, v);
                var properties = Config.GetProperties(b.EntityId);
                properties.Channel = v;
                Config.SendSynced(properties);
            };
            return channelProp;
        }

        public static IMyTerminalControlProperty<string> MessageProp<T>() where T : Ingame.IMyTerminalBlock
        {
            var messageProp = MyAPIGateway.TerminalControls.CreateProperty<string, T>("Message");
            messageProp.Getter = b => Config.GetProperties(b.EntityId).Message;
            messageProp.Setter = (b, v) =>
            {
                var properties = Config.GetProperties(b.EntityId);
                if (v.Length > 1000)
                {
                    properties.Message = v.Substring(0, 1000);
                }
                else
                {
                    properties.Message = v;
                }
                Config.SendSynced(properties);
            };
            return messageProp;
        }

        public static IMyTerminalControlProperty<int> IncomingCount<T>() where T : Ingame.IMyTerminalBlock
        {
            var incomingCount = MyAPIGateway.TerminalControls.CreateProperty<int, T>("IncomingCount");
            incomingCount.Getter = b => Config.GetProperties(b.EntityId).QueueCount();
            incomingCount.Setter = (b, v) => { };
            return incomingCount;
        }

        public static IMyTerminalControlProperty<string> ReadNextIncoming<T>() where T : Ingame.IMyTerminalBlock
        {
            var readNextIncoming = MyAPIGateway.TerminalControls.CreateProperty<string, T>("ReadNextIncoming");
            readNextIncoming.Getter = b => Config.GetProperties(b.EntityId).Dequeue();
            readNextIncoming.Setter = (b, v) => { };
            return readNextIncoming;
        }

        public static IMyTerminalControlProperty<Ingame.IMyTerminalBlock> Callback<T>() where T : Ingame.IMyTerminalBlock
        {
            var callback = MyAPIGateway.TerminalControls.CreateProperty<Ingame.IMyTerminalBlock, T>("Callback");
            callback.Getter = b =>
            {
                var properties = Config.GetProperties(b.EntityId);
                IMyEntity entity;
                if (MyAPIGateway.Entities.TryGetEntityById(properties.CallbackId, out entity))
                {
                    if (entity is Ingame.IMyProgrammableBlock || entity is IMyTimerBlock)
                    {
                        var target = entity as Ingame.IMyTerminalBlock;
                        if (Core.HasGridConnection(b, target))
                        {
                            return target;
                        }
                    }
                    else
                    {
                        throw new Exception("Callback is not set to a PB or timer.");
                    }
                }
                else
                {
                    properties.CallbackId = 0;
                    Config.SendSynced(properties);
                }

                return null;
            };
            callback.Setter = (b, v) =>
            {
                var properties = Config.GetProperties(b.EntityId);
                if (v == null)
                {
                    properties.CallbackId = 0;
                }
                else if (Core.HasGridConnection(b, v))
                {
                    if (v is Ingame.IMyProgrammableBlock || v is IMyTimerBlock)
                    {
                        properties.CallbackId = v.EntityId;
                    }
                    else
                    {
                        throw new Exception("Callback is not set to a PB or timer.");
                    }
                }
                Config.SendSynced(properties);
            };
            return callback;
        }

        public static IMyTerminalControl Separator<T>() where T : Ingame.IMyTerminalBlock
        {
            var separator = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, T>(string.Empty);
            return separator;
        }

        public static IMyTerminalControl ChannelSlider<T>() where T : Ingame.IMyTerminalBlock
        {
            var channelSlider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>(string.Empty);
            channelSlider.Title = MyStringId.GetOrCompute("Channel");
            channelSlider.Tooltip = MyStringId.GetOrCompute("Ctrl+Click to set exact value.");
            channelSlider.SetLimits(0, 65535);
            channelSlider.Getter = b => Config.GetProperties(b.EntityId).Channel;
            channelSlider.Setter = (b, v) =>
            {
                var properties = Config.GetProperties(b.EntityId);
                properties.Channel = (int)v;
                Config.SendSynced(properties);
            };
            channelSlider.Writer = (b, v) => v = v.Append("CH " + Config.GetProperties(b.EntityId).Channel);
            return channelSlider;
        }

        public static IMyTerminalControl MessageTextbox<T>() where T : Ingame.IMyTerminalBlock
        {
            var messageTexbox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlTextbox, T>(string.Empty);
            messageTexbox.Title = MyStringId.GetOrCompute("Message");
            messageTexbox.Tooltip = MyStringId.GetOrCompute("1000 characters max.");
            messageTexbox.Getter = b => new StringBuilder(Config.GetProperties(b.EntityId).Message);
            messageTexbox.Setter = (b, v) =>
            {
                var properties = Config.GetProperties(b.EntityId);
                if (v.Length > 1000)
                {
                    properties.Message = v.ToString().Substring(0, 1000);
                }
                else
                {
                    properties.Message = v.ToString();
                }
                Config.SendSynced(properties);
            };
            return messageTexbox;
        }

        public static IMyTerminalControl SendBtn<T>() where T : Ingame.IMyTerminalBlock
        {
            var sendBtn = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, T>(string.Empty);
            sendBtn.Title = MyStringId.GetOrCompute("Send Message");
            sendBtn.Tooltip = MyStringId.GetOrCompute("Transmit the message to\nall suitable antennas.");
            sendBtn.Action = b => Core.RequestBroadcast(BitConverter.GetBytes(b.EntityId));
            return sendBtn;
        }

        public static IMyTerminalControl CallbackList<T>() where T : Ingame.IMyTerminalBlock
        {
            var callbackList = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, T>(string.Empty);
            callbackList.Title = MyStringId.GetOrCompute("Callback");
            callbackList.Tooltip = MyStringId.GetOrCompute("PB: Run with message passed as argument\nTimer: Trigger the timer\nNone: Pass message to queue");
            callbackList.VisibleRowsCount = 5;
            callbackList.Multiselect = false;
            callbackList.ListContent = (b, v, v2) =>
            {
                if (b.CubeGrid != null)
                {
                    var properties = Config.GetProperties(b.EntityId);
                    var blocks = new List<Ingame.IMyTerminalBlock>();
                    var timers = new List<Ingame.IMyTerminalBlock>();
                    var gts = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid((IMyCubeGrid)b.CubeGrid);
                    gts.GetBlocksOfType<Ingame.IMyProgrammableBlock>(blocks);
                    gts.GetBlocksOfType<IMyTimerBlock>(timers);

                    blocks.AddRange(timers);

                    foreach (var block in blocks)
                    {
                        if (block.HasPlayerAccess(b.OwnerId))
                        {
                            var item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(block.CustomName), default(MyStringId), block);
                            if (block.EntityId == properties.CallbackId)
                            {
                                v.Insert(0, item);
                                v2.Add(item);
                            }
                            else
                            {
                                v.Add(item);
                            }
                        }
                    }
                }
            };
            callbackList.ItemSelected = (b, v) =>
            {
                if (v.Count > 0 && v[0].UserData != null)
                {
                    var properties = Config.GetProperties(b.EntityId);
                    var pb = (Ingame.IMyTerminalBlock)v[0].UserData;
                    if (pb.HasPlayerAccess(b.OwnerId))
                    {
                        properties.CallbackId = pb.EntityId;
                        Config.SendSynced(properties);
                    }
                }
            };
            return callbackList;
        }

        public static IMyTerminalControl ClearCallbackBtn<T>() where T : Ingame.IMyTerminalBlock
        {
            var clearCallbackList = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, T>(string.Empty);
            clearCallbackList.Title = MyStringId.GetOrCompute("Clear Selection");
            clearCallbackList.Action = b =>
            {
                var properties = Config.GetProperties(b.EntityId);
                properties.CallbackId = 0;
                Config.SendSynced(properties);
            };
            return clearCallbackList;
        }
    }
}

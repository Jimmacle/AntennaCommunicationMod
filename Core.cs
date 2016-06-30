using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Jimmacle.Antennas
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class Core : MySessionComponentBase
    {
        private bool init = false;
        private static bool gotEntitiesThisTick = false;
        private static HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
        private const int MAX_LENGTH = 1000;

        public override void UpdateBeforeSimulation()
        {
            if (MyAPIGateway.Session != null)
            {
                if (!init)
                {
                    init = true;

                    Config.Load();

                    MyAPIGateway.Multiplayer.RegisterMessageHandler(1628, Config.GetSynced);
                    MyAPIGateway.Multiplayer.RegisterMessageHandler(1629, HandleBroadcastRequest);
                    MyAPIGateway.Utilities.MessageEntered += PlayerBroadcast;
                }

                gotEntitiesThisTick = false;
            }
        }

        public static void RequestBroadcast(byte[] msg)
        {
            MyAPIGateway.Multiplayer.SendMessageToServer(1629, msg);
        }

        private static void HandleBroadcastRequest(byte[] antennaId)
        {
            var id = BitConverter.ToInt64(antennaId, 0);
            var entity = MyAPIGateway.Entities.GetEntityById(id);

            if (entity is IMyTerminalBlock)
            {
                var properties = Config.GetProperties(id);
                MyAPIGateway.Parallel.Start(() => Broadcast((IMyTerminalBlock)entity, properties.Message));
                return;
            }

            if (entity is IMyCharacter)
            {
                var message = Encoding.Unicode.GetString(antennaId, 12, antennaId.Length - 12);
                var channel = BitConverter.ToInt32(antennaId, 8);
                var player = MyAPIGateway.Players.GetPlayerControllingEntity(entity);
                PlayerTransmit(player, channel, message);
            }
        }

        private static void PlayerBroadcast(string messageText, ref bool sendToOthers)
        {
            if (messageText.StartsWith("/b"))
            {
                sendToOthers = false;
                var split = messageText.Split('|');

                if (split.Length != 3)
                    return;

                int x; int.TryParse(split[1], out x);

                var id = BitConverter.GetBytes(MyAPIGateway.Session.LocalHumanPlayer.Controller.ControlledEntity.Entity.EntityId);
                var chan = BitConverter.GetBytes(x);
                var msg = Encoding.Unicode.GetBytes(split[2]);

                var pack = new byte[id.Length + chan.Length + msg.Length];

                Array.Copy(id, 0, pack, 0, id.Length);
                Array.Copy(chan, 0, pack, 8, chan.Length);
                Array.Copy(msg, 0, pack, 12, msg.Length);

                RequestBroadcast(pack);
            }
        }

        public static void PlayerTransmit(IMyPlayer player, int channel, string message)
        {
            if (string.IsNullOrWhiteSpace(message) || message.Length > MAX_LENGTH)
                return;

            if (!(player.Controller.ControlledEntity.Entity is IMyCharacter))
                return;

            if (!((player.Controller.ControlledEntity as Sandbox.Game.Entities.IMyControllableEntity).EnabledBroadcasting))
                return;

            MyAPIGateway.Parallel.Start(() =>
            {
                var antennas = GetAntennasInRange(player.Controller.ControlledEntity.Entity.GetPosition(), 200);

                foreach (var target in antennas)
                {
                    if (CanTransmit(player.IdentityId, target))
                    {
                        var properties = Config.GetProperties(target.EntityId);
                        if (properties.Channel == channel)
                        {
                            MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                            {
                                IMyEntity entity;
                                if (MyAPIGateway.Entities.TryGetEntityById(properties.CallbackId, out entity))
                                {
                                    var block = entity as IMyTerminalBlock;
                                    if (HasGridConnection(target, block))
                                    {
                                        if (block is IMyProgrammableBlock)
                                            if (((IMyProgrammableBlock)block).TryRun(message)) return;
                                        if (block is IMyTimerBlock)
                                            block.ApplyAction("TriggerNow");
                                    }
                                }
                                properties.Enqueue(message);
                            });
                            Broadcast((IMyTerminalBlock)target, message);
                        }
                    }
                }
            });
        }

        public static void Broadcast(IMyTerminalBlock sender, string message, List<IMyTerminalBlock> exclude = null)
        {
            if (!sender.IsFunctional || string.IsNullOrWhiteSpace(message) || message.Length > MAX_LENGTH)
            {
                return;
            }

            var obj = (sender as IMyCubeBlock).GetObjectBuilderCubeBlock() as MyObjectBuilder_LaserAntenna;

            if (obj != null)
            {
                var target = obj.targetEntityId;
                if (target != null)
                {
                    var properties = Config.GetProperties(target.Value);
                    IMyEntity entity;
                    if (MyAPIGateway.Entities.TryGetEntityById(properties.CallbackId, out entity))
                    {
                        var block = entity as IMyTerminalBlock;
                        if (HasGridConnection(MyAPIGateway.Entities.GetEntityById(target.Value) as IMyTerminalBlock, block))
                        {
                            if (block is IMyProgrammableBlock)
                                if (((IMyProgrammableBlock)block).TryRun(message)) return;
                            if (block is IMyTimerBlock)
                                block.ApplyAction("TriggerNow");
                        }
                    }
                    properties.Enqueue(message);
                }
            }
            else
            {
                if (exclude == null)
                {
                    exclude = new List<IMyTerminalBlock>();
                }

                exclude.Add(sender);

                var senderProperties = Config.GetProperties(sender.EntityId);
                var antennas = GetAntennasInRange(sender.GetPosition(), TerminalPropertyExtensions.GetValue<float>(sender, "Radius"));

                foreach (var target in antennas)
                {
                    if (!exclude.Contains(target))
                    {
                        if (target.IsFunctional && CanTransmit(sender.OwnerId, target))
                        {
                            exclude.Add(target);
                            var properties = Config.GetProperties(target.EntityId);
                            if (properties.AntennaId != senderProperties.AntennaId)
                            {
                                if (properties.Channel == senderProperties.Channel)
                                {
                                    MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                                    {
                                        IMyEntity entity;
                                        if (MyAPIGateway.Entities.TryGetEntityById(properties.CallbackId, out entity))
                                        {
                                            var success = false;
                                            var block = entity as IMyTerminalBlock;
                                            if (HasGridConnection(target, block))
                                            {
                                                if (block is IMyProgrammableBlock)
                                                {
                                                    var b = block as IMyProgrammableBlock;
                                                    success = b.TryRun(message);
                                                }
                                                else if (block is IMyTimerBlock)
                                                {
                                                    block.ApplyAction("TriggerNow");
                                                    success = true;
                                                }
                                            }

                                            if (!success)
                                            {
                                                properties.Enqueue(message);
                                            }
                                        }
                                    });
                                }
                                Broadcast((IMyTerminalBlock)target, message, exclude);
                            }
                        }
                    }
                }
            }
        }

        public static bool HasGridConnection(IMyTerminalBlock source, IMyTerminalBlock target)
        {
            if (source == null || target == null) return false;

            var blocks = new List<IMyTerminalBlock>();
            MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(source.CubeGrid).GetBlocks(blocks);
            return blocks.Contains(target);
        }

        private static bool CanTransmit(long sourceIdentity, IMyRadioAntenna target)
        {
            return target.HasPlayerAccess(sourceIdentity);
        }

        public static List<IMyRadioAntenna> GetAntennasInRange(Vector3D position, double radius)
        {
            if (!gotEntitiesThisTick)
            {
                entities.Clear();
                MyAPIGateway.Entities.GetEntities(entities);
                gotEntitiesThisTick = true;
            }

            var result = new List<IMyRadioAntenna>();

            foreach (var entity in entities)
            {
                if (entity is MyCubeGrid)
                {
                    double gridDistSquared = (entity.GetPosition() - position).LengthSquared();
                    double gridRadius = entity.WorldAABB.Extents.Max();

                    //Check if grid may have antennae in range.
                    if (gridDistSquared < Math.Pow(radius + gridRadius, 2))
                    {
                        //Find antennae in range and transmittable.
                        foreach (var block in (entity as MyCubeGrid).GetFatBlocks())
                        {
                            var b = block as IMyRadioAntenna;
                            if (b != null && b.IsFunctional)
                            {
                                var distSquared = (b.GetPosition() - position).LengthSquared();

                                if (distSquared < Math.Pow(radius, 2))
                                {
                                    result.Add(b);
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        public override void SaveData()
        {
            Config.Save();
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(1628, Config.GetSynced);
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(1629, HandleBroadcastRequest);
            MyAPIGateway.Utilities.MessageEntered -= PlayerBroadcast;
        }
    }
}

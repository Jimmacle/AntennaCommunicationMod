using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Jimmacle.Antennas
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class Core : MySessionComponentBase
    {
        private bool _init;
        private const int MAX_LENGTH = 1000;

        public override void UpdateAfterSimulation()
        {
            if (MyAPIGateway.Session != null)
                if (!_init)
                {
                    _init = true;

                    Config.Load();

                    MyAPIGateway.Multiplayer.RegisterMessageHandler(1628, Config.GetSynced);
                    MyAPIGateway.Multiplayer.RegisterMessageHandler(1629, HandleBroadcastRequest);
                    MyAPIGateway.Utilities.MessageEntered += PlayerBroadcast;

                    Debug.Write("init");
                }
        }

        public static void RequestBroadcast(byte[] msg)
        {
            Debug.Write("broadcastreq");
            MyAPIGateway.Multiplayer.SendMessageToServer(1629, msg);
        }

        private void HandleBroadcastRequest(byte[] antennaId)
        {
            Debug.Write("handlereq");
            var id = BitConverter.ToInt64(antennaId, 0);
            var entity = MyAPIGateway.Entities.GetEntityById(id);

            var block = entity as IMyTerminalBlock;
            if (block != null)
            {
                var properties = Config.GetProperties(id);
                StartBroadcast(block, properties.Message, int.MaxValue);
                return;
            }

            if (entity is IMyCharacter)
            {
                var message = Encoding.Unicode.GetString(antennaId, 12, antennaId.Length - 12);
                var channel = BitConverter.ToInt32(antennaId, 8);
                var player = MyAPIGateway.Players.GetPlayerControllingEntity(entity);
                BroadcastFromPlayer(entity as IMyCharacter, player, channel, message);
            }
        }

        private void PlayerBroadcast(string messageText, ref bool sendToOthers)
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

        public void BroadcastFromPlayer(IMyCharacter character, IMyPlayer player, int channel, string message)
        {
            var antennas = GetValidReceivers(character.GetPosition(), 200);

            var excludeList = new HashSet<IMyTerminalBlock>();
            foreach (var ant in antennas)
            {
                if ((Config.GetProperties(ant.EntityId).Channel != channel) || !ant.HasPlayerAccess(player.PlayerID))
                    continue;

                BroadcastRecursive(ant, message, int.MaxValue, excludeList);
            }
        }

        public void StartBroadcast(IMyTerminalBlock source, string message, int maxHops)
        {
            Debug.Write("startbroadcast");
            if (message.Length > MAX_LENGTH)
                return;

            var laser = source.GetObjectBuilderCubeBlock() as MyObjectBuilder_LaserAntenna;
            if (laser?.targetEntityId != null)
            {
                IMyEntity entity;
                MyAPIGateway.Entities.TryGetEntityById(laser.targetEntityId, out entity);
                var endpoint = entity as IMyLaserAntenna;

                if (endpoint != null)
                    ProcessMessage(endpoint, message);

                return;
            }

            var excludeList = new HashSet<IMyTerminalBlock>();
            BroadcastRecursive(source, message, maxHops, excludeList, false);
        }

        public void BroadcastRecursive(IMyTerminalBlock source, string message, int remainingHops, HashSet<IMyTerminalBlock> exclude, bool processLocal = true)
        {
            Debug.Write("broadcastrecursive");
            if (exclude.Contains(source))
                return;

            exclude.Add(source);

            if (processLocal)
                ProcessMessage(source, message);

            if (remainingHops <= 0)
                return;

            remainingHops--;

            var radius = source.GetProperty("Radius").AsFloat().GetValue(source);
            var receivers = GetValidReceivers(source.Position, radius);
            foreach (var rec in receivers)
            {
                if ((Config.GetProperties(source.EntityId).Channel != Config.GetProperties(rec.EntityId).Channel) || !rec.HasPlayerAccess(source.OwnerId))
                {
                    exclude.Add(rec);
                    continue;
                }

                BroadcastRecursive(rec, message, remainingHops, exclude);
            }
        }

        public void ProcessMessage(IMyTerminalBlock endpoint, string message)
        {
            Debug.Write("procmsg");
            var properties = Config.GetProperties(endpoint.EntityId);
            IMyEntity callback;
            MyAPIGateway.Entities.TryGetEntityById(properties.CallbackId, out callback);

            if ((callback as IMyTerminalBlock)?.CubeGrid != endpoint.CubeGrid)
                return;

            var pb = callback as IMyProgrammableBlock;
            if (pb != null)
            {
                pb.TryRun(message);
                return;
            }

            var timer = callback as IMyTimerBlock;
            if (timer != null)
            {
                timer.ApplyAction("TriggerNow");
                return;
            }

            properties.Enqueue(message);
        }

        public static bool HasGridConnection(IMyTerminalBlock source, IMyTerminalBlock target)
        {
            if ((source == null) || (target == null)) return false;

            var blocks = new List<IMyTerminalBlock>();
            MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(source.CubeGrid).GetBlocks(blocks);
            return blocks.Contains(target);
        }

        public static List<IMyTerminalBlock> GetValidReceivers(Vector3D position, double radius)
        {
            var output = new List<IMyTerminalBlock>();
            var max = radius * radius;

            foreach (var radio in RadioCommComponent.RadioAntennae)
            {
                if (!radio.IsWorking)
                    continue;

                var dst = (radio.GetPosition() - position).LengthSquared();
                if (dst <= max)
                    output.Add(radio);
            }
            Debug.Write($"getants: {output.Count}");

            return output;
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

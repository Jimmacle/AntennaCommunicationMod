using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jimmacle.Antennas
{
    public static class Config
    {
        private static Dictionary<long, AntennaProperties> antennaConfigs = new Dictionary<long, AntennaProperties>();
        private static Dictionary<long, Queue<string>> antennaQueues = new Dictionary<long, Queue<string>>();
        private const string VAR_KEY = "AntennaCommunicationData"; //If this is changed all existing saves will lose their antenna data!

        public static AntennaProperties GetProperties(long antennaId)
        {
            if (antennaConfigs.ContainsKey(antennaId))
            {
                return antennaConfigs[antennaId];
            }
            else
            {
                antennaConfigs.Add(antennaId, new AntennaProperties(antennaId));
                return GetProperties(antennaId);
            }
        }

        public static void Save()
        {
            try
            {
                //Serialize dictionary to XML.
                var storage = new List<byte[]>();
                foreach (var item in antennaConfigs)
                    storage.Add(item.Value.Serialized());

                var str = MyAPIGateway.Utilities.SerializeToXML(storage);

                MyAPIGateway.Utilities.SetVariable(VAR_KEY, str);
            }
            catch
            {
                Debug.Write("Save failed!");
            }
        }

        public static void Load()
        {
            //Load saved data.
            try
            {
                string obj;
                if (MyAPIGateway.Utilities.GetVariable(VAR_KEY, out obj))
                {
                    var storage = MyAPIGateway.Utilities.SerializeFromXML<List<byte[]>>(obj);
                    foreach (var item in storage)
                    {
                        var i = AntennaProperties.Deserialize(item);
                        antennaConfigs.Add(i.AntennaId, i);
                    }
                }
            }
            catch
            {
                MyAPIUtilities.Static.Variables.Remove(VAR_KEY);
            }
        }

        public static void SendSynced(AntennaProperties properties)
        {
            var data = properties.Serialized();
            var packets = MessageHelper.Segment(data);

            foreach (var packet in packets)
                MyAPIGateway.Multiplayer.SendMessageToOthers(1628, packet);
        }

        public static void GetSynced(byte[] message)
        {
            var data = MessageHelper.Desegment(message);
            if (data != null)
            {
                var properties = AntennaProperties.Deserialize(data);
                if (antennaConfigs.ContainsKey(properties.AntennaId))
                    antennaConfigs[properties.AntennaId] = properties;
                else
                    antennaConfigs.Add(properties.AntennaId, properties);
            }
        }

        public class AntennaProperties
        {
            public long AntennaId;
            public long CallbackId;
            public int Channel;
            public string Message;

            public AntennaProperties(long id) : this()
            {
                AntennaId = id;
            }

            public AntennaProperties()
            {
                Channel = 0;
                Message = "";
            }

            public void Enqueue(string message)
            {
                if (!antennaQueues.ContainsKey(AntennaId))
                    antennaQueues.Add(AntennaId, new Queue<string>());

                if (antennaQueues[AntennaId].Count < 50)
                    antennaQueues[AntennaId].Enqueue(message);
            }

            public string Dequeue()
            {
                if (antennaQueues.ContainsKey(AntennaId) && (antennaQueues[AntennaId].Count > 0))
                    return antennaQueues[AntennaId].Dequeue();

                return null;
            }

            public int QueueCount()
            {
                if (antennaQueues.ContainsKey(AntennaId))
                    return antennaQueues[AntennaId].Count;

                return 0;
            }

            public void ClearQueue()
            {
                if (antennaQueues.ContainsKey(AntennaId))
                    antennaQueues[AntennaId].Clear();
            }

            private const int META_LENGTH = sizeof(long) * 2 + sizeof(int);

            public byte[] Serialized()
            {
                var antenna = BitConverter.GetBytes(AntennaId);
                var callback = BitConverter.GetBytes(CallbackId);
                var channel = BitConverter.GetBytes(Channel);
                var msg = Encoding.Unicode.GetBytes(Message);

                var outputSize = META_LENGTH + msg.Length;
                var output = new byte[outputSize];

                Array.Copy(antenna, 0, output, 0, antenna.Length);
                Array.Copy(callback, 0, output, 8, callback.Length);
                Array.Copy(channel, 0, output, 16, channel.Length);
                Array.Copy(msg, 0, output, 20, msg.Length);

                return output;
            }

            public static AntennaProperties Deserialize(byte[] obj)
            {
                var props = new AntennaProperties
                {
                    AntennaId = BitConverter.ToInt64(obj, 0),
                    CallbackId = BitConverter.ToInt64(obj, 8),
                    Channel = BitConverter.ToInt32(obj, 16),
                    Message = Encoding.Unicode.GetString(obj, 20, obj.Length - META_LENGTH)
                };
                return props;
            }
        }
    }


}

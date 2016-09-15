using System;
using System.Collections.Generic;

namespace Jimmacle.Antennas
{
    public static class MessageHelper
    {
        private static readonly Dictionary<int, PartialMessage> Messages = new Dictionary<int, PartialMessage>();
        private const int PACKET_SIZE = 4096;
        private const int META_SIZE = sizeof(int) * 2;
        private const int DATA_LENGTH = PACKET_SIZE - META_SIZE;

        /// <summary>
        /// Segments a byte array.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static List<byte[]> Segment(byte[] message)
        {
            var hash = BitConverter.GetBytes(message.GetHashCode());
            var packets = new List<byte[]>();
            var msgIndex = 0;

            var packetId = message.Length / DATA_LENGTH;

            while (packetId >= 0)
            {
                var id = BitConverter.GetBytes(packetId);
                byte[] segment;

                if (message.Length - msgIndex > DATA_LENGTH)
                    segment = new byte[PACKET_SIZE];
                else
                    segment = new byte[META_SIZE + message.Length - msgIndex];

                //Copy packet header data.
                Array.Copy(hash, segment, hash.Length);
                Array.Copy(id, 0, segment, hash.Length, id.Length);

                //Copy segment of original message.
                Array.Copy(message, msgIndex, segment, META_SIZE, segment.Length - META_SIZE);

                packets.Add(segment);
                msgIndex += DATA_LENGTH;
                packetId--;
            }

            return packets;
        }

        /// <summary>
        /// Reassembles a segmented byte array.
        /// </summary>
        /// <param name="packet">Array segment.</param>
        /// <returns>Message fully desegmented, "message" is assigned.</returns>
        public static byte[] Desegment(byte[] packet)
        {
            var hash = BitConverter.ToInt32(packet, 0);
            var packetId = BitConverter.ToInt32(packet, sizeof(int));
            var dataBytes = new byte[packet.Length - META_SIZE];
            Array.Copy(packet, META_SIZE, dataBytes, 0, packet.Length - META_SIZE);

            if (!Messages.ContainsKey(hash))
                if (packetId == 0)
                    return dataBytes;
                else
                    Messages.Add(hash, new PartialMessage(packetId));

            var message = Messages[hash];
            message.WritePart(packetId, dataBytes);

            if (!message.IsComplete)
                return null;

            Messages.Remove(hash);
            return message.Data;
        }

        private class PartialMessage
        {
            public byte[] Data;
            private readonly HashSet<int> _receivedPackets = new HashSet<int>();
            private readonly int _maxId;
            public bool IsComplete => _receivedPackets.Count == _maxId + 1;

            public PartialMessage(int startId)
            {
                _maxId = startId;
                Data = new byte[DATA_LENGTH * startId];
            }

            public void WritePart(int id, byte[] data)
            {
                var index = _maxId - id;
                var requiredLength = index * DATA_LENGTH + data.Length;

                if (Data.Length < requiredLength)
                    Array.Resize(ref Data, requiredLength);

                Array.Copy(data, 0, Data, index * DATA_LENGTH, data.Length);
                _receivedPackets.Add(id);
            }
        }
    }
}

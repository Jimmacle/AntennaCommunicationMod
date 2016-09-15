using Sandbox.ModAPI;

namespace Jimmacle.Antennas
{
    public static class Debug
    {
        public static bool Enabled { get; set; }

        public static void Write(string msg)
        {
            if (!Enabled)
                return;

            MyAPIGateway.Utilities.ShowMessage("antennas", msg);
        }
    }
}

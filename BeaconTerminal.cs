using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces.Terminal;
using System.Collections.Generic;
using System.Text;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using Sandbox.ModAPI;

namespace Jimmacle.Antennas
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Beacon))]
    public class BeaconTerminal : MyGameLogicComponent
    {
        private static bool init = false;
        private MyObjectBuilder_EntityBase builder;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            builder = objectBuilder;
            Entity.NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return copy ? builder.Clone() as MyObjectBuilder_EntityBase : builder;
        }

        public List<IMyTerminalControl> Controls = new List<IMyTerminalControl>();

        public override void UpdateOnceBeforeFrame()
        {
            if (MyAPIGateway.TerminalControls == null)
            {
                Entity.NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                return;
            }

            if (!init)
            {
                init = true;

                //Actions.
                var sendAction = CustomControls.SendAction<IMyBeacon>();
                MyAPIGateway.TerminalControls.AddAction<IMyBeacon>(sendAction);

                //Properties.
                var channelProp = CustomControls.ChannelProp<IMyBeacon>();
                MyAPIGateway.TerminalControls.AddControl<IMyBeacon>(channelProp);

                var messageProp = CustomControls.MessageProp<IMyBeacon>();
                MyAPIGateway.TerminalControls.AddControl<IMyBeacon>(messageProp);

                //Controls.
                var separator = CustomControls.Separator<IMyBeacon>();
                Controls.Add(separator);

                var channelSlider = CustomControls.ChannelSlider<IMyBeacon>();
                Controls.Add(channelSlider);

                var messageTextbox = CustomControls.MessageTextbox<IMyBeacon>();
                Controls.Add(messageTextbox);

                var sendBtn = CustomControls.SendBtn<IMyBeacon>();
                Controls.Add(sendBtn);

                foreach (var control in Controls)
                {
                    MyAPIGateway.TerminalControls.AddControl<IMyBeacon>(control);
                }
            }
        }
    }
}

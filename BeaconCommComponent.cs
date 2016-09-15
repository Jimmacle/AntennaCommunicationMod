using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Jimmacle.Antennas
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Beacon))]
    public class BeaconCommComponent : MyGameLogicComponent
    {
        public static HashSet<IMyBeacon> Beacons = new HashSet<IMyBeacon>();

        private static bool _init;
        private MyObjectBuilder_EntityBase _builder;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            _builder = objectBuilder;
            Entity.NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            Beacons.Add(Entity as IMyBeacon);
        }

        public override void MarkForClose()
        {
            Beacons.Remove(Entity as IMyBeacon);
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return copy ? _builder.Clone() as MyObjectBuilder_EntityBase : _builder;
        }

        public List<IMyTerminalControl> Controls = new List<IMyTerminalControl>();

        public override void UpdateOnceBeforeFrame()
        {
            if (MyAPIGateway.TerminalControls == null)
            {
                Entity.NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
                return;
            }

            if (!_init)
            {
                _init = true;

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
                    MyAPIGateway.TerminalControls.AddControl<IMyBeacon>(control);
            }
        }
    }
}

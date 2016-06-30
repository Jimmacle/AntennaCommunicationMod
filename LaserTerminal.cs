using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Jimmacle.Antennas
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_LaserAntenna))]
    public class LaserTerminal : MyGameLogicComponent
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
                var sendAction = CustomControls.SendAction<IMyLaserAntenna>();
                MyAPIGateway.TerminalControls.AddAction<IMyLaserAntenna>(sendAction);

                var clearAction = CustomControls.ClearQueue<IMyLaserAntenna>();
                MyAPIGateway.TerminalControls.AddAction<IMyLaserAntenna>(clearAction);

                //Properties.
                var channelProp = CustomControls.ChannelProp<IMyLaserAntenna>();
                MyAPIGateway.TerminalControls.AddControl<IMyLaserAntenna>(channelProp);

                var messageProp = CustomControls.MessageProp<IMyLaserAntenna>();
                MyAPIGateway.TerminalControls.AddControl<IMyLaserAntenna>(messageProp);

                var incomingCountProp = CustomControls.IncomingCount<IMyLaserAntenna>();
                MyAPIGateway.TerminalControls.AddControl<IMyLaserAntenna>(incomingCountProp);

                var readNextIncoming = CustomControls.ReadNextIncoming<IMyLaserAntenna>();
                MyAPIGateway.TerminalControls.AddControl<IMyLaserAntenna>(readNextIncoming);

                var callback = CustomControls.Callback<IMyLaserAntenna>();
                MyAPIGateway.TerminalControls.AddControl<IMyLaserAntenna>(callback);

                //Controls.
                var separator = CustomControls.Separator<IMyLaserAntenna>();
                Controls.Add(separator);

                var messageTexbox = CustomControls.MessageTextbox<IMyLaserAntenna>();
                Controls.Add(messageTexbox);

                var sendBtn = CustomControls.SendBtn<IMyLaserAntenna>();
                Controls.Add(sendBtn);

                var callbackList = CustomControls.CallbackList<IMyLaserAntenna>();
                Controls.Add(callbackList);

                var clearCallbackBtn = CustomControls.ClearCallbackBtn<IMyLaserAntenna>();
                ((IMyTerminalControlButton)clearCallbackBtn).Action += b => Controls.ForEach(x => x.UpdateVisual());
                Controls.Add(clearCallbackBtn);

                foreach (var control in Controls)
                {
                    MyAPIGateway.TerminalControls.AddControl<IMyLaserAntenna>(control);
                }
            }
        }
    }
}

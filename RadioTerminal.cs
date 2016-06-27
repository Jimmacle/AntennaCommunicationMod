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
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_RadioAntenna))]
    public class RadioTerminal : MyGameLogicComponent
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
                var sendAction = CustomControls.SendAction<IMyRadioAntenna>();
                MyAPIGateway.TerminalControls.AddAction<IMyRadioAntenna>(sendAction);

                var clearAction = CustomControls.ClearQueue<IMyRadioAntenna>();
                MyAPIGateway.TerminalControls.AddAction<IMyRadioAntenna>(clearAction);

                //Properties.
                var channelProp = CustomControls.ChannelProp<IMyRadioAntenna>();
                MyAPIGateway.TerminalControls.AddControl<IMyRadioAntenna>(channelProp);

                var messageProp = CustomControls.MessageProp<IMyRadioAntenna>();
                MyAPIGateway.TerminalControls.AddControl<IMyRadioAntenna>(messageProp);

                var incomingCountProp = CustomControls.IncomingCount<IMyRadioAntenna>();
                MyAPIGateway.TerminalControls.AddControl<IMyRadioAntenna>(incomingCountProp);

                var readNextIncoming = CustomControls.ReadNextIncoming<IMyRadioAntenna>();
                MyAPIGateway.TerminalControls.AddControl<IMyRadioAntenna>(readNextIncoming);

                var callback = CustomControls.Callback<IMyRadioAntenna>();
                MyAPIGateway.TerminalControls.AddControl<IMyRadioAntenna>(callback);

                //Controls.
                var separator = CustomControls.Separator<IMyRadioAntenna>();
                Controls.Add(separator);

                var channelSlider = CustomControls.ChannelSlider<IMyRadioAntenna>();
                Controls.Add(channelSlider);

                var messageTextbox = CustomControls.MessageTextbox<IMyRadioAntenna>();
                Controls.Add(messageTextbox);

                var sendBtn = CustomControls.SendBtn<IMyRadioAntenna>();
                Controls.Add(sendBtn);

                var callbackList = CustomControls.CallbackList<IMyRadioAntenna>();
                Controls.Add(callbackList);

                var clearCallbackBtn = CustomControls.ClearCallbackBtn<IMyRadioAntenna>();
                ((IMyTerminalControlButton)clearCallbackBtn).Action += b => Controls.ForEach(x => x.UpdateVisual());
                Controls.Add(clearCallbackBtn);

                foreach (var control in Controls)
                {
                    MyAPIGateway.TerminalControls.AddControl<IMyRadioAntenna>(control);
                }
            }
        }
    }
}

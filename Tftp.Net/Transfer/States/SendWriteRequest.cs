using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Tftp.Net.Transfer;
using Tftp.Net.Trace;

namespace Tftp.Net.Transfer.States
{
    class SendWriteRequest : StateWithNetworkTimeout
    {
        public override void OnStateEnter()
        {
            base.OnStateEnter();
            SendRequest();
        }

        private void SendRequest()
        {
            // Don't propose options if we are not negotiating
            var optionsToPropose = Context.NegotiateOptions ? Context.ProposedOptions.ToOptionList() : null;
            WriteRequest request = new WriteRequest(Context.Filename, Context.TransferMode, optionsToPropose);
            SendAndRepeat(request);
        }

        public override void OnCommand(ITftpCommand command, System.Net.EndPoint endpoint)
        {
            if (command is OptionAcknowledgement)
            {
                if (Context.NegotiateOptions)
                {
                    TransferOptionSet acknowledged = new TransferOptionSet((command as OptionAcknowledgement).Options);
                    Context.FinishOptionNegotiation(acknowledged);
                    BeginSendingTo(endpoint);
                }
                else
                {
                    throw new Exception("Option negotion disabled, but remote acknowledged options");
                }
            }
            else
            if (command is Acknowledgement && (command as Acknowledgement).BlockNumber == 0)
            {
                if (Context.NegotiateOptions)
                {
                    Context.FinishOptionNegotiation(TransferOptionSet.NewEmptySet());
                }
                BeginSendingTo(endpoint);
            }
            else
            if (command is Error error) //The server denied our request
            {
                Context.SetState(new ReceivedError(error));
            }
            else
                base.OnCommand(command, endpoint);
        }

        private void BeginSendingTo(System.Net.EndPoint endpoint)
        {
            //Switch to the endpoint that we received from the server
            Context.GetConnection().RemoteEndpoint = endpoint;

            //Start sending packets
            Context.SetState(new Sending());
        }

        public override void OnCancel(TftpErrorPacket reason)
        {
            Context.SetState(new CancelledByUser(reason));
        }
    }
}

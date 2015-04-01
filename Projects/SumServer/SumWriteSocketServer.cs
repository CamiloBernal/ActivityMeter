using System;
using System.Net.WebSockets;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;

namespace SumServer
{
    public class SumWriteSocketServer : ISumSocketServer
    {
        private System.Timers.Timer serviceTimer;
        private ISumSocketServerCallBack serviceCallback = null;

        public SumWriteSocketServer()
        {
            this.serviceTimer = new System.Timers.Timer(1000);
            this.serviceTimer.Elapsed += serviceTimer_Elapsed;
        }

        private void serviceTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Process();
        }

        public void SendSumData(System.ServiceModel.Channels.Message msg)
        {
            serviceCallback = OperationContext.Current.GetCallbackChannel<ISumSocketServerCallBack>();
            this.serviceTimer.Start();
        }

        private void Process()
        {
            if (serviceCallback == null)
                return;

            if (((IChannel)serviceCallback).State == CommunicationState.Opened)
            {
                while (SumServerProcess.CurrentDateInterval < 1000)
                {
                    System.Threading.Thread.Sleep(1);
                }

                try
                {
                    int currentSum = SumServerProcess.CurrentSum;
                    if (((IChannel)serviceCallback).State == CommunicationState.Opened)
                        serviceCallback.ReportSumDataToClients(BuildClientMessage(currentSum.ToString()));
                }
                catch (Exception)
                {
                    //TODO: Pending
                }
                SumServerProcess.ClearSum();
            }
        }

        private Message BuildClientMessage(string msgText)
        {
            Message msg = ByteStreamMessage.CreateMessage(
                new ArraySegment<byte>(Encoding.UTF8.GetBytes(msgText)));
            msg.Properties["WebSocketMessageProperty"] =
                new WebSocketMessageProperty
                {
                    MessageType = WebSocketMessageType.Text
                };

            return msg;
        }
    }
}
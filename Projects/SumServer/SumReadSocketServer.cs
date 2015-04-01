using System.ServiceModel;
using System.Text;

namespace SumServer
{
    public sealed class SumReadSocketServer : ISumSocketServer
    {
        public void SendSumData(System.ServiceModel.Channels.Message message)
        {
            var serviceCallback = OperationContext.Current.GetCallbackChannel<ISumSocketServerCallBack>();
            if (message.IsEmpty)
            {
                return;
            }
            byte[] body = message.GetBody<byte[]>();
            string clientMessage = Encoding.UTF8.GetString(body);
            int entrie = -100;
            if (int.TryParse(clientMessage, out entrie))
            {
                entrie = int.Parse(clientMessage);
                if (entrie >= 0 && entrie <= 20)
                {
                    SumServerProcess.SumEntries.Add(entrie);
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }
    }
}
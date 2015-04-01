using System.ServiceModel;
using System.ServiceModel.Channels;

namespace SumServer
{
    [ServiceContract(CallbackContract = typeof(ISumSocketServerCallBack))]
    public interface ISumSocketServer
    {
        [OperationContract(IsOneWay = true, Action = "*")]
        void SendSumData(Message msg);
    }
}
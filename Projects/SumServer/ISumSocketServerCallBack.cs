using System.ServiceModel;
using System.ServiceModel.Channels;

namespace SumServer
{
    [ServiceContract]
    public interface ISumSocketServerCallBack
    {
        [OperationContract(IsOneWay = true, Action = "*")]
        void ReportSumDataToClients(Message msg);
    }
}
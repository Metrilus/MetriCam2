using CookComputing.XmlRpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetriCam2.Cameras.IFM
{

    public interface ISession : IXmlRpcProxy
    {
        [XmlRpcMethod("editApplication")]
        string EditApplication(string[] param);

        [XmlRpcMethod("stopEditingApplication")]
        string StopEditingApplication(string[] param);

        [XmlRpcMethod("setOperatingMode")]
        string SetOperatingMode(string param);

        [XmlRpcMethod("cancelSession")]
        string CancelSession();
    }


    public interface IDevice : IXmlRpcProxy
    {
        [XmlRpcMethod("getParameter")]
        string GetParameter(string parameter);

        [XmlRpcMethod("setParameter")]
        string SetParameter(string parameter, string value);

        [XmlRpcMethod("save")]
        string Save();
    }


    public interface IAppImager : IXmlRpcProxy
    {
        [XmlRpcMethod("getParameter")]
        string GetParameter(string parameter);

        [XmlRpcMethod("setParameter")]
        string SetParameter(string parameter, string value);
    }


    public interface IApp : IXmlRpcProxy
    {
        [XmlRpcMethod("getParameter")]
        string GetParameter(string parameter);

        [XmlRpcMethod("setParameter")]
        string SetParameter(string parameter, string value);

        [XmlRpcMethod("save")]
        string Save();
    }


    public interface IEdit : IXmlRpcProxy
    {
        [XmlRpcMethod("editApplication")]
        string EditApplication(int param);

        [XmlRpcMethod("stopEditingApplication")]
        string StopEditingApplication();

        [XmlRpcMethod("createApplication")]
        int CreateApplication();

        [XmlRpcMethod("deleteApplication")]
        string DeleteApplication(int param);
    }


    public interface IEditDevice : IXmlRpcProxy
    {
        [XmlRpcMethod("setParameter")]
        string SetParameter(string[] param);
    }

    public interface IServer : IXmlRpcProxy
    {
        [XmlRpcMethod("requestSession")]
        string RequestSession(string param0, string param1);
    }
}
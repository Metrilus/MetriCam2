// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using CookComputing.XmlRpc;

namespace MetriCam2.Cameras.IFM
{
    public struct Application
    {
        public int Index;
        public string Name;
        public string Description;
    }

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
        [XmlRpcMethod("setParameter")]
        string SetParameter(string parameter, string value);

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

        [XmlRpcMethod("reboot")]
        string Reboot(int mode);
    }

    public interface IServer : IXmlRpcProxy
    {
        [XmlRpcMethod("requestSession")]
        string RequestSession(string param0, string param1);

        [XmlRpcMethod("getApplicationList")]
        Application[] GetApplicationList();
    }
}
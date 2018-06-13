namespace MetriCam2.Cameras.Internal.Sick
{
    /// <summary>
    /// Access modes on the camera.
    /// </summary>
    internal enum AccessModes
    {
        Always_Run = 0,
        Operator = 1,
        Maintenance = 2,
        AuthorizedClient = 3,
        Service = 4,
    }
}

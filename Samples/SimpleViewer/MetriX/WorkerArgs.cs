using MetriX.Debug;
using MetriX.Models;
using System;
using System.Threading;

namespace MetriX.Freight.Views.Debug
{
    public sealed class WorkerArgs
    {
        public MockEngine Engine;
        public Func<CancellationToken, bool> Callback;

        public SnapshotType FailureSnapshotType = SnapshotType.Debug;
        public string FailureSnapshotPrefix = "Exception";

        public Func<Exception, bool> ExceptionHandler;
    }
}

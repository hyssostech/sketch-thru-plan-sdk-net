using System;
using DynamicData;

namespace StpSDK;

public abstract class StpService : IDisposable
{
    protected IObservableCache<IStpObject, string> Items { get; }
    protected StpRecognizer _stpRecognizer;
    protected bool disposedValue;

    private SourceCache<IStpObject, string> _stpItemCache;

    internal StpService(StpRecognizer stpRecognizer)
    {
        if (stpRecognizer is null)
            throw new ArgumentNullException(nameof(stpRecognizer));

        _stpRecognizer = stpRecognizer;

        if (_stpRecognizer.IsConnected)
            throw new InvalidOperationException("Service creation needs to take place before connection to STP");

        _stpItemCache = new SourceCache<IStpObject, string>(t => t?.Poid ?? string.Empty);
        SubscribetoEvents();
        Items = _stpItemCache.AsObservableCache();
    }

    protected abstract void SubscribetoEvents();

    protected void AddOrUpdate(IStpObject item)
    {
        _stpItemCache.AddOrUpdate(item);
    }

    protected void RemoveKey(string poid)
    {
        _stpItemCache?.Remove(poid);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _stpItemCache.Dispose();
            }
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

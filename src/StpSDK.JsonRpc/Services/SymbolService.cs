using System;
using DynamicData;

namespace StpSDK;

public class SymbolService : StpService
{
    public IObservableCache<StpSymbol, string> All => Items.Cast(o => o as StpSymbol).Filter(s => s is not null).AsObservableCache();

    public IObservableCache<StpSymbol, string> Units { get; }
    public IObservableCache<StpSymbol, string> TacticalGraphics { get; }
    public IObservableCache<StpSymbol, string> Mootw { get; }

    public SymbolService(StpRecognizer stpRecognizer) : base(stpRecognizer)
    {
        Units = All.Connect()
            .Filter(o => o.Type.Equals("unit", StringComparison.InvariantCultureIgnoreCase))
            .Cast(o => (StpSymbol)o)
            .AsObservableCache();
        TacticalGraphics = All.Connect()
            .Filter(s => s.Type.Equals("tg", StringComparison.InvariantCultureIgnoreCase))
            .Cast(o => (StpSymbol)o)
            .AsObservableCache();
        Mootw = All.Connect()
            .Filter(s => s.Type.Equals("mootw", StringComparison.InvariantCultureIgnoreCase))
            .Cast(o => (StpSymbol)o)
            .AsObservableCache();
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                if (_stpRecognizer is not null)
                {
                    _stpRecognizer.OnSymbolAdded -= StpSdk_OnSymbolAddedOrUpdated;
                    _stpRecognizer.OnSymbolModified -= StpSdk_OnSymbolAddedOrUpdated;
                    _stpRecognizer.OnSymbolDeleted -= StpSdk_OnSymbolDeleted;
                }
                base.Dispose();
            }
        }
    }

    protected override void SubscribetoEvents()
    {
        _stpRecognizer.OnSymbolAdded += StpSdk_OnSymbolAddedOrUpdated;
        _stpRecognizer.OnSymbolModified += StpSdk_OnSymbolAddedOrUpdated;
        _stpRecognizer.OnSymbolDeleted += StpSdk_OnSymbolDeleted;
    }

    private void StpSdk_OnSymbolAddedOrUpdated(string poid, StpItem stpSymbol, bool isUndo)
    {
        AddOrUpdate(stpSymbol);
    }

    private void StpSdk_OnSymbolDeleted(string poid, bool isUndo)
    {
        RemoveKey(poid);
    }
}

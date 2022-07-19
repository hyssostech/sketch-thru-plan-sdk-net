using StpSDK;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StpSDKSample;

/// <summary>
/// ViewModel factory class
/// </summary>
internal static class ViewModel
{
    /// <summary>
    /// Create a formatted object for display on a propertygrid
    /// </summary>
    /// <param name="stpItem"></param>
    /// <returns></returns>
    public static RootVM Create(StpItem stpItem)
    {
        if (stpItem is StpSymbol stpSymbol)
        {
            if (stpSymbol.Type == "unit")
            {
                return new UnitVM(stpSymbol);
            }
            else if (stpSymbol.Type == "mootw")
            {
                return new MootwVM(stpSymbol);
            }
            else if (stpSymbol.Type == "tg")
            {
                return new TgVM(stpSymbol);
            }
        }
        return null;
    }
}

/// <summary>
/// Common (readonly) properties
/// </summary>
internal abstract class RootVM
{
    [ReadOnly(true), Display(Order = 1)]
    public string Type { get; set; }
    [ReadOnly(true), Display(Order = 2)]
    public string Id { get; set; }
    [ReadOnly(true), Display(Order = 3)]
    public string Description { get; set; }
    public RootVM() { }
    public RootVM(string type, string id, string description)
    {
        Type = type;
        Id = id;
        Description = description;
    }
    public abstract StpItem AsStpItem();
}

/// <summary>
/// Common symbol properties
/// </summary>
internal class SymbolVM : RootVM
{
    protected StpSymbol _stpSymbol;

    public string SIDC { get; set; }
    public string Designator1 { get; set; }
    public string Designator2 { get; set; }
    public Affiliation Affiliation { get; set; }

    public SymbolVM() { }
    public SymbolVM(StpSymbol stpSymbol)
    {
        _stpSymbol = stpSymbol;
        Type = stpSymbol.Type;
        Id = stpSymbol.Poid;
        Description = stpSymbol.Description;
        SIDC = stpSymbol.SymbolId;
        Designator1 = stpSymbol.Designator1;
        Designator2 = stpSymbol.Designator2;
        Affiliation = stpSymbol.Affiliation;
    }

    public override StpItem AsStpItem()
    {
        // Update item with current properties and return
        _stpSymbol.SymbolId = SIDC;
        _stpSymbol.Designator1 = Designator1;
        _stpSymbol.Designator2 = Designator2;
        _stpSymbol.Affiliation = Affiliation;
        return _stpSymbol;
    }
}

/// <summary>
/// Unit properties
/// </summary>
internal class UnitVM : SymbolVM
{
    public Echelon Echelon { get; set; }
    public Strength Strength { get; set; }
    public Modifier HQType { get; set; }
    public Status Status { get; set; }

    public UnitVM() { }
    public UnitVM(StpSymbol stpSymbol) : base(stpSymbol)
    {
        Echelon = stpSymbol.Echelon;
        Strength = stpSymbol.Strength;
        HQType = stpSymbol.Modifier;
        Status = stpSymbol.Status;
    }

    /// <summary>
    /// Item with updated properties
    /// </summary>
    /// <returns></returns>
    public override StpItem AsStpItem()
    {
        base.AsStpItem();
        _stpSymbol.Echelon = Echelon;
        _stpSymbol.Strength = Strength;
        _stpSymbol.Modifier = HQType;
        _stpSymbol.Status = Status;
        return _stpSymbol;
    }
}

/// <summary>
/// Task Graphic / TG properties
/// [Category("TG")]
/// </summary>
internal class TgVM : SymbolVM
{
    public Echelon Echelon { get; set; }
    public TgVM() { }

    public TgVM(StpSymbol stpSymbol) : base(stpSymbol)
    {
        Echelon = stpSymbol.Echelon;
    }

    public override StpItem AsStpItem()
    {
        base.AsStpItem();
        _stpSymbol.Echelon = Echelon;
        return _stpSymbol;
    }
}

/// <summary>
/// MOOTW symbol properties
/// </summary>
internal class MootwVM : SymbolVM
{
    public MootwVM() { }
    public MootwVM(StpSymbol stpSymbol) : base(stpSymbol)
    {
    }
    public override StpItem AsStpItem() => base.AsStpItem();
}

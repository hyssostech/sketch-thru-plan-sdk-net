using System;
using System.ComponentModel;
using DynamicData;
using DynamicData.Kernel;

namespace StpSDK;

public class TaskOrgService : StpService
{
    public IObservableCache<StpTaskOrgUnit, string> All => Items.Cast(o => o as StpTaskOrgUnit).Filter(s => s is not null).AsObservableCache();
    public IObservableCache<StpTaskOrgUnit, string> Nodes => _nodes;
    public IObservableCache<Node<StpTaskOrgUnit, string>, string> Tree => _tree;
    public IObservableCache<UnitRelationshipPair, string> Pairs => _pairs;

    protected IObservableCache<StpTaskOrgUnit, string> _nodes;
    protected IObservableCache<Node<StpTaskOrgUnit, string>, string> _tree;
    protected IObservableCache<UnitRelationshipPair, string> _pairs;
    protected SourceCache<StpTaskOrgRelationship, string> _stpRelationshipsCache;

    public TaskOrgService(StpRecognizer stpRecognizer) : base(stpRecognizer)
    {
        _stpRelationshipsCache = new SourceCache<StpTaskOrgRelationship, string>(t => t.Child);

        _pairs = All.Connect()
            .LeftJoin(
                right: _stpRelationshipsCache.Connect(),
                rightKeySelector: r => r.Child,
                resultSelector: (unit, rel) => new UnitRelationshipPair(unit, rel))
            .AsObservableCache();

        _nodes = _pairs.Connect()
            .Transform(n =>
            {
                n.Unit.PoidSuperior = n.Rel.HasValue ? n.Rel.Value.Parent ?? string.Empty : string.Empty;
                return n.Unit;
            })
            .AsObservableCache();

        _tree = _nodes.Connect()
            .TransformToTree(t => t.PoidSuperior)
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
                    _stpRecognizer.OnTaskOrgUnitAdded -= StpSdk_OnTaskOrgUnitAddedOrModified;
                    _stpRecognizer.OnTaskOrgUnitModified -= StpSdk_OnTaskOrgUnitAddedOrModified;
                    _stpRecognizer.OnTaskOrgUnitDeleted -= StpSdk_OnTaskOrgUnitDeleted;
                    _stpRecognizer.OnTaskOrgRelationshipAdded -= StpSdk_OnTaskOrgRelationshipAddedOrModified;
                    _stpRecognizer.OnTaskOrgRelationshipModified -= StpSdk_OnTaskOrgRelationshipAddedOrModified;
                    _stpRecognizer.OnTaskOrgRelationshipDeleted -= StpSdk_OnTaskOrgRelationshipDeleted;
                }
                _stpRelationshipsCache?.Dispose();
                _nodes?.Dispose();
                base.Dispose();
            }
        }
    }

    protected override void SubscribetoEvents()
    {
        _stpRecognizer.OnTaskOrgUnitAdded += StpSdk_OnTaskOrgUnitAddedOrModified;
        _stpRecognizer.OnTaskOrgUnitModified += StpSdk_OnTaskOrgUnitAddedOrModified;
        _stpRecognizer.OnTaskOrgUnitDeleted += StpSdk_OnTaskOrgUnitDeleted;
        _stpRecognizer.OnTaskOrgRelationshipAdded += StpSdk_OnTaskOrgRelationshipAddedOrModified;
        _stpRecognizer.OnTaskOrgRelationshipModified += StpSdk_OnTaskOrgRelationshipAddedOrModified;
        _stpRecognizer.OnTaskOrgRelationshipDeleted += StpSdk_OnTaskOrgRelationshipDeleted;
    }

    private void StpSdk_OnTaskOrgUnitAddedOrModified(string poid, StpTaskOrgUnit stpTaskOrgUnit, bool isUndo)
    {
        AddOrUpdate(stpTaskOrgUnit);
    }

    private void StpSdk_OnTaskOrgUnitDeleted(string poid, bool isUndo)
    {
        RemoveKey(poid);
    }

    private void StpSdk_OnTaskOrgRelationshipAddedOrModified(string poid, StpTaskOrgRelationship stpTaskOrgRelationship, bool isUndo)
    {
        _stpRelationshipsCache.AddOrUpdate(stpTaskOrgRelationship);
    }

    private void StpSdk_OnTaskOrgRelationshipDeleted(string poid, bool isUndo)
    {
        _stpRelationshipsCache.RemoveKey(poid);
    }

    public class UnitRelationshipPair : INotifyPropertyChanged
    {
#pragma warning disable CS0067
        public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore CS0067

        private StpTaskOrgUnit _unit;
        private Optional<StpTaskOrgRelationship> _rel;

        public StpTaskOrgUnit Unit { get => _unit; set => _unit = value; }
        public Optional<StpTaskOrgRelationship> Rel { get => _rel; set => _rel = value; }

        public UnitRelationshipPair(StpTaskOrgUnit unit, Optional<StpTaskOrgRelationship> rel)
        {
            Unit = unit;
            Rel = rel;
        }
    }
}

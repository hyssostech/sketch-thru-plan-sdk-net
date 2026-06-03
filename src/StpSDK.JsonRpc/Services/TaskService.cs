using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DynamicData;
using DynamicData.Kernel;

namespace StpSDK;

public class TaskService : StpService
{
    public IObservableCache<StpTask, string> All => Items.Cast(o => o as StpTask).Filter(s => s is not null).AsObservableCache();
    public IObservableCache<StpNode<StpTask>, string> Nodes => _nodes;
    public IObservableCache<Node<StpNode<StpTask>, string>, string> Tree => _tree;

    private SymbolService _symbolService;
    private IObservableCache<StpNode<StpTask>, string> _nodes;
    private IObservableCache<Node<StpNode<StpTask>, string>, string> _tree;

    public TaskService(StpRecognizer stpRecognizer, SymbolService symbolService) : base(stpRecognizer)
    {
        if (symbolService is null)
            throw new ArgumentNullException(nameof(symbolService));

        _symbolService = symbolService;

        _nodes = _symbolService.Units.Connect()
            .LeftJoinMany(
                right: All.Connect(),
                rightKeySelector: t => t.Who,
                resultSelector: (symbol, tasks) => new TasksPerUnit((StpSymbol)symbol, tasks))
            .TransformMany(p => p.Tasks.Select(t => new UnitTaskPair(p.Symbol, t)), p => p.Symbol.Poid + p.Task?.Poid ?? string.Empty)
            .GroupWithImmutableState(uwt => uwt.Symbol.DesigPlusDescription.ToUpperInvariant())
            .Transform(group =>
            {
                var unitTaskPairs = group.Items;
                var symbol = unitTaskPairs.FirstOrDefault().Symbol;
                var rootTask = new StpTask()
                {
                    Poid = symbol.Poid,
                    Description = symbol.DesigPlusDescription,
                    StartTime = 0,
                    EndTime = 0,
                };
                string concatKey = string.Join("|", unitTaskPairs.Select(ut => ut.Symbol.Poid));
                var root = new StpNode<StpTask>()
                {
                    Item = rootTask,
                    Key = concatKey,
                    ParentKey = string.Empty,
                    Description = group.Key,
                    Depth = 0,
                    ChildrenCount = unitTaskPairs.Where(ut => ut.Task is not null).Count()
                };
                var unitTasks = unitTaskPairs
                    .Select(ut => ut.Task).Where(t => t is not null)
                    .Select(t =>
                        new StpNode<StpTask>()
                        {
                            Item = t,
                            Key = t.Poid,
                            ParentKey = root.Key,
                            Description = t.Description,
                            Depth = 1,
                            ChildrenCount = t.Alternates?.Count - 1 ?? 0
                        })
                    .OrderBy(t => t.Item.StartTime)
                    .ThenBy(t => t.Item.EndTime)
                    .SelectMany(t =>
                    {
                        List<StpNode<StpTask>> utNodes = new() { t };
                        if (t.Item.Alternates != null && t.Item.Alternates.Count > 1)
                        {
                            utNodes.AddRange(t.Item.Alternates
                                .GetRange(1, t.Item.Alternates.Count - 1)
                                .Select(a =>
                                    new StpNode<StpTask>()
                                    {
                                        Item = (StpTask)a,
                                        Key = t.Item.Poid + a.Order,
                                        ParentKey = t.Item.Poid,
                                        Description = a.Description,
                                        Depth = 2,
                                        ChildrenCount = 0
                                    }));
                        }
                        return utNodes;
                    });
                if (unitTasks != null && unitTasks.Any())
                {
                    rootTask.StartTime = unitTasks.Where(t => t.Depth == 1).Min(t => t.Item.StartTime);
                    rootTask.EndTime = unitTasks.Where(t => t.Depth == 1).Max(t => t.Item.EndTime);
                }
                List<StpNode<StpTask>> res = new() { root };
                res.AddRange(unitTasks);
                return res;
            }, transformOnRefresh: true)
            .TransformMany(tl => tl, t => t.Key)
            .AsObservableCache();

        _tree = _nodes.Connect()
            .TransformToTree(t => t.ParentKey)
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
                    _stpRecognizer.OnTaskAdded -= StpSdk_OnTaskAddedOrUpdated;
                    _stpRecognizer.OnTaskModified -= StpSdk_OnTaskAddedOrUpdated;
                    _stpRecognizer.OnTaskDeleted -= StpSdk_OnTaskDeleted;
                }
                _tree?.Dispose();
                base.Dispose();
            }
        }
    }

    protected override void SubscribetoEvents()
    {
        _stpRecognizer.OnTaskAdded += StpSdk_OnTaskAddedOrUpdated;
        _stpRecognizer.OnTaskModified += StpSdk_OnTaskAddedOrUpdated;
        _stpRecognizer.OnTaskDeleted += StpSdk_OnTaskDeleted;
    }

    private void StpSdk_OnTaskAddedOrUpdated(string poid, StpTask stpTask, List<string> taskPoids, bool isUndo)
    {
        AddOrUpdate(stpTask);
    }

    private void StpSdk_OnTaskDeleted(string poid, bool isUndo)
    {
        RemoveKey(poid);
    }
}

public class TasksPerUnit : INotifyPropertyChanged
{
#pragma warning disable CS0067
    public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore CS0067

    public StpSymbol Symbol { get; set; }
    public List<StpTask> Tasks;

    public TasksPerUnit(StpSymbol symbol, IGrouping<StpTask, string, string> tasks)
    {
        Symbol = symbol;
        Tasks = tasks.Items.ToList();
    }
}

public class UnitTaskPair : INotifyPropertyChanged
{
#pragma warning disable CS0067
    public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore CS0067

    public StpSymbol Symbol { get; set; }
    public StpTask Task;

    public UnitTaskPair(StpSymbol symbol, Optional<StpTask> task)
    {
        Symbol = symbol;
        Task = task.ValueOrDefault();
    }
}

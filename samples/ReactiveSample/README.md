# Reactive Sample Overview


This sample modifies the [Tasking Sample](../TaskingSample), using [Reactive Extensions (Rx)](https://reactivex.io/) 
instead of .NET events. More specifically, it takes advantage of SDK provided services that make use of the
[Dynamic Data](https://dynamic-data.org/) package. 

Symbols, Tasks, and Task Orgs (ORBAT) operations are automatically handled, and the data exposed as `Observable 
Caches`, which can be transformed using the rich set o Dynamic Data operators, which  "are fully composable and 
can be chained together to perform powerful and very complicated operations while maintaining simple, fluent code".

The main advantage of this functionality is its ability to transparently generate Observable Collections that
can be bound directly to UI components that support such binding, like many XAML components, and some Winforms ones.
That simplifies in many cases the code required to drive the Views in MVVM projects, which is reduced to a few
lines required to subscribe to one of the services and apply transformations to match the requirements of the view
(for example converting STP objects into some target that the View is able to consume).

Here we overview the SDK items that support a reactive style, and briefly describe related Rx capabilities
for context only.

## Services

The SDK exposes observable caches via three Services:

. SymbolService - military symbols  
. TaskService - task definitions
. TaskOrgService - Task Org (TO) units and relationships

Each service provides properties exposing `IObservableCache`s providing all nodes/elements, as well
as specific subsets as described below.

### SymbolService

`All` provides all symbols, independently of their type. `Units`, `TacticalGraphics` and `Mootw` 
pre-filter based on the related types.

```csharp
/// <summary>
/// All items
/// </summary>
public IObservableCache<StpSymbol, string> All => base.Items.Cast(o => o as StpSymbol).Filter(s => s is not null).AsObservableCache();
/// <summary>
/// Observable cache for unit symbols
/// </summary>
public IObservableCache<StpSymbol, string> Units { get; }

/// <summary>
/// Observable cache for tactical graphic symbols
/// </summary>
public IObservableCache<StpSymbol, string> TacticalGraphics { get; }

/// <summary>
/// Observable cache for mootw symbols
/// </summary>
public IObservableCache<StpSymbol, string> Mootw { get; }
```

### TaskService

`Nodes` offers a flat/linear view of the tasks, while `Tree` is hierarchical, fit therefore as a source
for consumers that are able to handle a tree-like structure.

```csharp
/// <summary>
/// All items
/// </summary>
public IObservableCache<StpTask, string> All => base.Items.Cast(o => o as StpTask).Filter(s => s is not null).AsObservableCache();
/// <summary>
/// Observable cache of (self-referenced) task nodes
/// </summary>
public IObservableCache<StpNode<StpItem>, string> Nodes => _nodes;
/// <summary>
/// Observable tree of task
/// </summary>
/// <remarks>
/// Tasks trees have three tiers:
/// 1. Root referencing units with a common name - generally present and anticipated
/// 2. Tasks related to units with that name as children
/// 3. Each task has its Alternates as children (excluding the reference to the task itself)
/// </remarks>
public IObservableCache<Node<StpNode<StpItem>, string>, string> Tree => _tree;
```

### TaskOrgService

`Nodes` offers a flat/linear view of the tasks, while `Tree` is hierarchical, fit therefore as a source
for consumers that are able to handle a tree-like structure.

```csharp
/// <summary>
/// All items
/// </summary>
public IObservableCache<StpTaskOrgUnit, string> All => base.Items.Cast(o => o as StpTaskOrgUnit).Filter(s => s is not null).AsObservableCache();
/// <summary>
/// Observable cache of (self-referenced) unit nodes representing an ORBAT/TO hierarchy
/// </summary>
public IObservableCache<StpTaskOrgUnit, string> Nodes => _nodes;
/// <summary>
/// Observable tree of unit nodes 
/// </summary>
public IObservableCache<Node<StpTaskOrgUnit, string>, string> Tree => _tree;



## Subscribing and filtering

To make use of a service, create an instance by invoking an SDK factory method (`CreateSymbolService`, 
`CreateTaskService`, `CreateTaskOrgDervice).

The first step is to `Connect()` to one of the caches provided by the service, and then applying whatever additional 
filtering, sorting or transformation is desired. 

In the example below, symbols are filtered by affiliation (via a property associated with Friendly and Hostile 
UI checkboxes). A common operation (not used here) would be to transform the STP provided objects into
a different native one that matches the consumers' expectations. For additional information on supported operators, see the
[Dynamic Data](https://dynamic-data.org/) documentation, samples and support. 


```csharp
// Subscribe to services  _before_ connecting to STP, so that the correct message subscriptions can be identified
_symbolService = _stpRecognizer.CreateSymbolService();
_symbolService.All.Connect()
    // Log the updates
    //.ForEachChange(change => ShowStpMessage(
    //    $"SymbolService.Items {change.Reason}: {((StpSymbol)change.Current).FullDescription}"))
    // Convert to StpSymbol and bind to list that feeds the UI controls
    .Filter(_affiliationFilter.Observable)
    .ObserveOn(SynchronizationContext.Current)
    .Bind<StpSymbol, string>(_currentSymbols)
    // Dispose items that are removed as symbols are deleted and subscribe to the feed
    .DisposeMany()
    .Subscribe();
```


After potential transformations, if needed, the cache can be bound to an observable 
such as a `BindingList` or a `ReadOnlyObservableCollection`, which can in turn be 
used as  UI elements' data sources to drive automatic refreshes as the underlying data
changes (more details in the next subsection).

`ObserveOn()` makes the bind occur in the appropriate UI thread, avoiding the usual extra coding required to
switch context via `Invoke`s. `DisposeMany()` disposes objects that may have been produced as part of the
transformations, and are no longer needed. 

As symbols are added, modified, or removed by STP as a reaction to local or remote  user speech and sketch actions, 
the associated `BindingList` that subscribes to the service is automatically updated,
with the prescribed processing automatically applied in an efficient way as update events are propagated. 

**No further handling of any STP events are required to update the bound observables or UI elements, which
are automatically updated without any extra code, if associated with the subscribing observable collections 
(as described in the next sub-section).**


NOTE: The commented out `ForEachChange()` simply displays update events as they are received, for
debugging purposes. The same approach is used to make updates to Tasks and TaskOrgs visible on the
log pane.
WinForms lacks a native data bound hierarchical UI component, and the sample therefore does not
take advantage more directly of a stream of hierarchical nodes, other than to log the node
updates.
In more capable UI frameworks, these could be used to automatically   


## Associating with a DatsSource

In this sample the service is associated with a `Datagrid` UI element. 
That is one of the few WinForms components that can be populated via a `DataSource`.
Other frameworks (e.g. `WPF`) offer a considerable number of other elements that can be bound to a data source
and therefore take advantage of the Rx capabilities.


```csharp
BindingList<StpSymbol> _currentSymbols;
// -- snip --
// Load initial/blank Symbols datagridview and associate the data property names with StpSymbol fields
_currentSymbols = new BindingList<StpSymbol>();
dataGridViewSymbolItems.AutoGenerateColumns = false;
dataGridViewSymbolItems.DataSource = _currentSymbols;
Type.DataPropertyName = "Type";
Description.DataPropertyName = "Description";
Designator.DataPropertyName = "DesignatorDescription";
SymbolID.DataPropertyName = "SymbolID";
Affiliation.DataPropertyName = "Affiliation";
```

The [source code](./Form1.cs) has additional examples of how `DataGrid` selection events
can be used to automatically refresh the element that is displayed in a `PropertyGrid`.
Once again, that code reflects the limited capabilities of WinForms when it comes to 
binding, which can be more naturally expressed in other frameworks.

## Events as Observables

Even though direct events in most cases do not need to be handled in separate,
since their effect is already exposed via the changes to the observable caches as discussed,
for the sake of completeness, the SDK also includes Observables that wrap each of the
events that were discussed in previous samples.

As an example, the event `OnSymbolEdit` is matched by a `WhenSymbolEdit` Observable 
that triggers whenever the corresponding event fires.
Similar `When*` Observables exist for the remaining `On*` events.

One small advantage of the Observables is that `Invoke`s are not required.
`ObserveOn()` can be used instead to handle dispatching the handling action in 
the rith thread context.

```csharp
// Subscribe to the observables _before_ connecting, so that the correct message 
// subscriptions can be identified
// Edit operations, including map commands
_stpRecognizer.WhenSymbolEdit
    .ObserveOn(SynchronizationContext.Current)
    .Subscribe(args =>
    {
        ShowStpMessage("---------------------------------\n" +
            $"EDIT OPERATION:\t{args.Operation}\n");
    });
```

# Editing Sample Overview


This sample extends the [quickstart](../quickstart), adding:

* Sketch and audio processing feedback
* Selection of alternate interpretations
* Speech and sketch Edits and Map operations
* Manual editing- UI-based deletion, updates, and symbol creation  


Here just the particular aspects illustrated by the sample are described.
Details shared by all samples are described in the [main samples page](../README.md). 
Common rendering and other map operations are described in the [Mapping class documentation](../../plugins/Mapping/SimpleMapPlugin/README.md)


## Sketch and audio processing feedback

Most of the input to STP is in the form of sketches and speech. Since processing happens behind the scenes
within the STP Engine, it is recommended that users be provided incremental processing feedback so they 
can have an indication that their input is being considered and, most of all, are able to tell when
input has not been successful so they can retry a command.

STP advertises interpretation progress primarily through the follawing events:

* OnSpeechRecognized - provides feedback on the speech to text interpretation. This even is optional, but provides meaningful feedback to users. 
* OnListeningStateChanged - provides an indication of the state of the audio capture device - listening to user input, or off.
* OnSketchRecognized - invoked when strokes have been processed by STP. This event is useful for removing ink that either resulted in a successful symbol interpretation, or was dropped from consideration, to keep the interface clean
* OnSketchIntegrated - invoked when strokes have been processed by STP. This event is useful for removing ink that either resulted in a successful symbol interpretation, or was dropped from consideration, to keep the interface clean

```cs
// Speech recognition and ink feedback
_stpRecognizer.OnSpeechRecognized += StpRecognizer_OnSpeechRecognized;
_stpRecognizer.OnListeningStateChanged += StpRecognizer_OnListeningStateChanged;
_stpRecognizer.OnSketchRecognized += StpRecognizer_OnSketchRecognized;
_stpRecognizer.OnSketchIntegrated += StpRecognizer_OnSketchIntegrated;
```

### Speech and audio feedback

In the [quickstart](../quickstart), users were already shown the results of the speech transcription, as soon as that
was completed, displayed in a textbox as lowercase sentences separated by the "|" sybol. Also displayed was the specific 
transcription that was successfully fused with a sketch, shown in the same testbox in capital case letters.

```cs
private void StpRecognizer_OnSpeechRecognized(List<string> speechList)
{
        // Display to provide users feedback on the input
        if (speechList != null && speechList.Count > 0)
        {
            // Show just top alternates to avoid best being hidden by scroll
            int max = speechList.Count > 5 ? 5 : speechList.Count;
            string concat = string.Join(" | ", speechList.GetRange(0, max));
            if (max < speechList.Count)
            {
                concat += " | ...";
            }
            ShowSpeechReco(concat);
        }
}
```

In this sample, additional audio-related feedback is provided. When the audio collection is activated (the microphone is on),
a green border is displayed around the speech transcription text box.
When the audio capture is deactivated (the microphone is off), the highlight is removed.
This provides an hint to users regarding the time slot during which they can speak, after starting to sketch.
This indication can take different forms, for example via a microphone icon state or some other visual means.

```cs
private void StpRecognizer_OnListeningStateChanged(bool isListening)
{
    if (this.InvokeRequired)
    {   // recurse on GUI thread if necessary
        this.Invoke(new MethodInvoker(
            () => StpRecognizer_OnListeningStateChanged(isListening)));
        return;
    }
    // Change the color of the speech text box to green while on
    panelAudioCapture.BackColor = isListening ? Color.Green : SystemColors.Control;
}
```

### Sketch processing feedback

The main sketch-related feedback, also illustrated in the [quickstart](../quickstart), is the freehand ink rendering, drawn
as the user drags a mouse or stylus.
Also illustrated by the [quickstart](../quickstart) was the removal of this electronic ink once a symbol was recognized
and rendered on the display,
providing a user an indication that the sketch has been successfully integrated, and removed from consideration.
Alternative designs may leave this ink in place, for example on a layer, offering users the ability to turn the
visibility on/off as they prefer.

```cs
private void StpRecognizer_OnSketchIntegrated()
{
    if (this.InvokeRequired)
    {   // recurse on GUI thread if necessary
        this.Invoke(new MethodInvoker(
            () => StpRecognizer_OnSketchIntegrated()));
        return;
    }
    // Remove ink
    _mapHandler.ClearInk();
}
```

In this sample, additional feedback is provided, changing the color of the ink rendering (from red to orange),
as an indication that STP has processed the ink already, and is therefore considering it in combination with
speech.

```cs
private void StpRecognizer_OnSketchRecognized(List<SketchRecoResult> sketchList)
{
    if (this.InvokeRequired)
    {   // recurse on GUI thread if necessary
        this.Invoke(new MethodInvoker(() => StpRecognizer_OnSketchRecognized(sketchList)));
        return;
    }
    // Change color
    _mapHandler.MarkInkAsProcessed();
}
```

Changing the color of the ink helps users determine when input failed to result in a successful recognition.
When in `drawing mode`, i.e. when sketching a full 2525/APP6 symbol, rather than using Point, Line or Area gestures,
users can tell when the system took over processing. If they paused for too long before adding a required next stroke,
they will be able to tell what it is that STP has performed analysis based on an incomplete drawing, which likely needs
to be re-entered.
If in `Point, Line, Area (PLA) mode`, orange ink that remains on the screen after the speech transcription has been presented
is an indication that no symbol was successfully detected, and that the user should try again.


## Selection of alternate interpretations

Natural language is in most cases ambiguous, admitting different possible interpretations. A short Line, for example, can be interpreted as a
Point, depending on the context; Areas are normally not fully closed, so they could in many cases represent Lines; similarly, other types
of gestures that STP recognizes, such as "harpoons", "zig zag lines", "vees", and "Us" can be confused with each other, as they share
basic linear characteristics.
Speech interpretation is similarly prone to multiple interpretations, as different words may sound alike, and noise or other audio
device artifacts, or user accent and intonation may lead to different possible interpretations of the same underlying signal.

As a result of the above, both sketch and speech transcriptions most often than not result in a multiple possible interpretations,
some more likely than others.

STP leverages the two different modalities - speech and sketch - to identify the best possible combinations via `mutual disambiguation`.
As a simple example, if there is high confidence that a sketch is a Point, then the speech interpretations that are related to 
single point symbols are going to be promoted, even if they may not have been identified as the most likely.
As a result of this process, STP is able to have a high level of successful multimodal interpretations, in cases where the 
sketch and the speech were not the most likely individually.

Multiple speech transcription and sketch  interpretations are paired up, according to the doctrinal language that is configured.
The successful matches are then ranked according to their likelihood, and this list of alternates is what is returned (asynchronously) by STP to
the client apps.

It is convenient to offer users a way to examine the alternates and pick a different one in case the best interpretation is
not the one they had in mind, or if they find that another related symbol may actually fit better what they intend to add.
This sample displays the the list of alternates of the most recently recognized symbol in a data grid, offering users the opportunity to pick a different interpretation
with a single click.

Selection of an alternate is communicated to STP via the `ChooseAlternate` SDK method. It requires the unique identifier of the symbol
for which a lower ranked interpretation is being selected, as well as the `Order` property for that item. The Order/rank is in general
zero for the best/most likely interpretation, 1 for the second best and so on. This property is defined for each of the symbols
in the list of Alternates that is returned by STP. Selection should therefore identify the Order of the element that the user
selected, and send that over to STP for processing.

```cs
private void DataGridViewAlternates_RowStateChanged(object sender, DataGridViewRowStateChangedEventArgs e)
{
    // If a row other than the first (best) is selected, update the symbol with the corresponding interpretation
    if (e.StateChanged == DataGridViewElementStates.Selected && e.Row.Index > 0)
    {
        var item = e.Row.DataBoundItem as StpItem;
        if (item != null)
        {
            // Inform STP that the item with this rank order was chosen by the user
            // STP will issue a symbol update notification (OnSymbolModefied) with this element first in the list
            // of alternates
            _stpRecognizer.ChooseAlternate(item.Poid, item.Order);
        }
    }
}
```

STP updates its state and broadcasts the updated symbols so that all client apps that are connected to the Engine have the opportunity to update their displays. This illustrates
the asynchronous style employed by STP. The local app is not required (neither should) perform any updates directly, but only
within STP event handlers. Here the same handler that deals with speech and sketch edits - `OnSymbolModified` - also handles alternate selections. 

```cs
private void StpRecognizer_OnSymbolModified(string poid, StpItem stpItem, bool isUndo)
{
    StpRecognizer_OnStpMessage(
        StpRecognizer.StpMessageLevel.Info, "---------------------------------");
    string msg = $"SYMBOL MODIFIED:\t{stpItem.Poid}\t{stpItem.FullDescription}";
    ShowStpMessage(msg);
    // Display the modified  item as a military symbol
    // Not interested in other types of objects 
    if (stpItem is StpSymbol stpSymbol)
    {
        _currentSymbol = stpSymbol;
        DisplaySymbol(stpSymbol);
    }
}
```

## Speech and sketch Edits and Map operations

Besides handling the input of symbols via speech and sketch, STP supports a (configurable) set of edit and map operations.
Most of these operations are handled by STP itself, which triggers events to notify client apps of the updates or deletions 
that may result from the edits.

Editing is premised on the identification of symbols the user may be trying to edit. In STP this is normally done
via a sketch on top of an existing symbol, followed by a spoken command, as described in more detail below. 
From a technical standpoint, for editing of this kind to work, it is necessary for client apps to indicate -
whenever a stroke is sent to STP for processing - which symbols that stroke intersects. 
Since such intersection depends on the visual representation presented to the user at a moment, the responsibility
for detecting and reporting stroke/symbol intersection rests with the client apps.

As strokes are sent to STP via `SendInk`, the unique identifiers of symbols that are intersected
by the particular stroke are provided:

```cs
    private void MapHandler_OnStrokeCompleted(object sender, Mapping.PenStroke penStroke)
    {
    // To support multimodal symbol editing, it is necessary for the app to
    // identify the existing elements that a stroke intersects, for example, 
    // a point or line over a unit that one wants to move, delete, or change 
    // attributes
        List<string> intersectedPoids =
            _currentSymbol is null
                ? null
                : _mapHandler.IntesectedSymbols(new List<StpSymbol>() { _currentSymbol });

        // Send sketch to STP for processing and potential fusion with speech
        _stpRecognizer.SendInk(penStroke.PixelBounds,
                               penStroke.TopLeftGeo,
                               penStroke.BotRightGeo,
                               penStroke.Stroke,
                               penStroke.TimeStart,
                               penStroke.TimeEnd,
                               intersectedPoids);
    }
```

The samples use a simple detection mechanism that is described in more detail in the [Mapping class  documentation](../../Plugins/SimpleMapPlugin/README.md).

### Adding or modifying attributes of already placed symbols

STP supports incremental/piecemeal definition of symbol properties by allowing users to "poke" at a symbol
(mark it with a sketched point or short point) and then speaking the value of a property, for example "echelon platoon", 
or "reinforced".

Supported edits include the following, provided that a particular symbol supports the corresponding property:

* Echelon
* Strength
* Designators
* Status - "anticipated", "present"
* Altitude, or max and min altitude 
* Start and end time (a measure is in effect)

This type of edit is handled automatically by STP. Upon detecting a stroke that intersects one or more symbols, STP considers speech transcriptions that may 
be interpreted as property values for those symbols.
The different types of properties that each symbol may take is also taken into consideration, so an echelon or strength transcription for example will only be consider for symbols that have these particular properties.

If there is enough confidence in the interpretation, then STP performs the editing and communicates the change via the already mentioned `OnSymbolModified` event, providing the updated properties as the content.

### Deleting and moving symbols

Symbols can be deleted by  "poking" at a symbol (marking it with an intersecting stroke) and speaking "delete this".

To change the location of a symbol, sketch a line starting within a symbol (intersecting it), and ending at a new desired position 
for the symbol, speaking "move this"

Deletion is communicated by STP via the `OnSymbolDeleted` event, so that the app is able to refresh caches and the display. Movement is handled as yet another type of update, via the same `OnSymbolModified` event used to support other types of edits. In this case, what is changed in the location of the symbol.
Apps should therefore be prepared to re-render a symbol on a new location as part of possible updates.

### Other UI operations

While the edits described thus far are performed by STP itself, and communicated 
via standard `OnSymbolModifeid` and `OnSymbolDeleted`, some operations depend purely on the client apps.
That includes map operations, such as zoomming and panning, and visual symbol operations, like selection.
Upon interpreting input as a client UI command, STP sends a notification requesting that the client app perform the
operation.

* OnSymbolEdited - invoked whenever a n operation (e.g. selection) is triggered
* OnMapOperation - invoked whenever a map operation such as zoom or pan is triggered

```cs
// Edit operations, including map commands
_stpRecognizer.OnSymbolEdited += StpRecognizer_OnSymbolEdited;
_stpRecognizer.OnMapOperation += StpRecognizer_OnMapOperation;
```

Editing is extensible, and operations that are meaningful to a particular application can be added (for example "show imagery 
in this area"). Here we limit the discussion to the common commands:

* Zooming is commanded by sketching a Point and speaking "zoom here", or "zoom out", with the expectation
that a fixed zoom is applied, centered at the location

* If a Line or an Area are sketched and the user speaks "zoom here", the expectation is that the map is zoomed
so that as much of the area indicated by the sketch as possible is presented in the display

* Panning is activated by a Line and "pan map" speech, with the expectation that the map pans in the direction
and distance given by the vector corresponding to the sketched line

* "Poking" at a symbol and speaking "select this" is expected to cause the symbol to be selected, with 
whatever semantics selection has in the client app

Operations that affect the UI, not the symbols directly, are relayed to client apps via the `OnSymbolEdited` and 
`OnMapOperation` events.

This sample responds to these events by just listing the requests, with no actual map/UI effect.

```cs
private void StpRecognizer_OnSymbolEdited(string operation, Location location)
{
    ShowStpMessage("---------------------------------");
    string msg = $"EDIT OPERATION:\t{operation}";
    ShowStpMessage(msg);
}
```

```cs
private void StpRecognizer_OnMapOperation(string operation, Location location)
{
    ShowStpMessage("---------------------------------");
    string msg = $"MAP OPERATION:\t{operation}";
    ShowStpMessage(msg);
}
```

## Manual editing- UI-based deletion, updates, and symbol creation

Typically, applications that integrate STP already have conventional UI means to add, update and remove symbols. 
To support the continued use of these existing capabilities in a seamless way, the SDK provides methods that can be used 
to notify STP of changes performed through these conventional means.

When users add, modify or remove elements using conventional UI controls, these operations need to be conveyed to STP, so that the
state remains consistent within the Engine itself and across all client apps, making
it possible for example for STP to apply speech and sketch edits on these symbols just as it does on symbols that are created via speech and sketch, avoiding the 
existence of different types of symbols supporting different capabilities.


The client app is expected to just notify STP of the user intention expressed via the UI, and then await the 
regular asynchronous response issued by STP whenever symbols are added, modified or deleted (`OnSymbolAdded`, `OnSymbolModified`, `OnSymbolDeleted`). 
If that is a problem give a particular app design, then the client app is required to detect
when to ignore STP notifications related to operations that have already been performed as part of the
UI element activations.
This is dispreferred, as it has a potential for introducing inconsistencies, as one client app may show users a state
that has not been propagated yet to other clients, and may fail to ever be for some reason.

In this sample a Delete button causes the latest symbol or task to be removed. 
The app calls `DeleteSymbol` to notify STP and then performs the actual deletion 
within the usual  asynchronous `OnSymbolRemoved` event handler.



```cs
private void ButtonDelete_Click(object sender, EventArgs e)
{
    if (_currentSymbol != null)
    {
        // Inform STP that this symbol should be deleted
        // STP will issue a symbol deletion notification 
        // (OnSymbolRemoved), which will cause the actual 
        // eventual removal
        _stpRecognizer.DeleteSymbol(_currentSymbol.Poid);
    }
}
```

Manual updates are illustrated in this sample via the `Update` button, which loads any edits performed to the current 
item displayed in the property grid and submits the changes to STP. The app then handles the corresponding asynchronous 
`OnSymbolUpdated` event that is broadcast by STP as a response.

```cs
private void ButtonUpdate_Click(object sender, EventArgs e)
{
    // Bail if no symbol 
    if (propertyGridResult.SelectedObject is null)
    {
        return;
    }

    var symbolVM = propertyGridResult.SelectedObject as SymbolVM;
    // Inform STP that this symbol should be updated
    // STP will issue a symbol update notification 
    // (OnSymbolModified), which will cause the actual eventual 
    // update
    _stpRecognizer.UpdateSymbol(symbolVM.Id, symbolVM.AsStpItem());
}
```

In case of new symbols, `AddSymbol` - not shown in this smaple - takes an object populated with the desired properties, as manually specified by the user. 
The only required property is SymbolId, which in a military domain contains a 2525/APP6 SIDC. 
STP sets remaining properties to their defaults automatically, based on its  language configurations.

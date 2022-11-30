# Reactive Sample Overview


This sample modifies the [Tasking Sample](../TaskingSample), using [Reactive Extensions (Rx)](https://reactivex.io/) 
instead of .NET events. More specifically, it takes advantage of SDK provided services that make use of the
[Dynamic Data](https://dynamic-data.org/) package. 

Symbols, Tasks, and Task Orgs (ORBAT) operations are automatically handled, and the data exposed as `Observable 
Chaches`, which can be transformed using the rich set o Dynamic Data operators, which  "are fully composable and 
can be chained together to perform powerful and very complicated operations while maintaining simple, fluent code".

The main advantage of this functionaility is its ability to transparently generate Observable Collections that
can be bound directly to UI components that support such binding, like many XAML components, and some Winforms ones.
That simplifies in many cases the code required to drive the Views in MVVM projects, whihc is reduced to a few
lines required to subscribe to one of the services and apply transformations to match the requirements of the view
(for example converting STP objects into some target that the View is able to consume).

UNDER CONSTRUCTION



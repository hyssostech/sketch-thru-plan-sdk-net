using DynamicData.Binding;
using System;
using System.Reactive.Linq;

namespace StpSDK;

public class ObservableObj<T> : AbstractNotifyPropertyChanged
{
    private T _object;
    private IObservable<T> _observable;

    public T Value
    {
        get => _object;
        set => SetAndRaise(ref _object, value);
    }

    public IObservable<T> Observable => _observable;

    public ObservableObj(T defaultValue = default(T))
    {
        _observable = this.WhenValueChanged(@this => @this.Value)
            .Select(o => o ?? defaultValue);
    }
}

public class ObservableFilter<ObjT, FilterT> : AbstractNotifyPropertyChanged
{
    private ObjT _object;
    private IObservable<Func<FilterT, bool>> _observable;
    Func<FilterT, bool> _condition;

    public ObjT DynammicValue
    {
        get => _object;
        set => SetAndRaise(ref _object, value);
    }

    public IObservable<Func<FilterT, bool>> Observable => _observable;

    public ObservableFilter(Func<FilterT, bool> condition)
    {
        _condition = condition;
        _observable = this.WhenValueChanged(@this => @this.DynammicValue)
            .Select(o => _condition);
    }
}

public class NotifyingObj<T> : AbstractNotifyPropertyChanged
{
    private T _object;

    public T Value
    {
        get => _object;
        set => SetAndRaise(ref _object, value);
    }
}

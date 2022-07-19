namespace StpSDK.Mapping
{
    public interface IMapping
    {
        Color StrokeProcessedColor { get; set; }
        Color StrokeSketchingColor { get; set; }
        int StrokeWidth { get; set; }

        event EventHandler<LatLon> OnPenDown;
        event EventHandler<Mapping.PenStroke> OnStrokeCompleted;

        void ClearInk();
        void ClearMap();
        void Highlight(StpSymbol stpSymbol);
        List<string> IntesectedSymbols(List<StpSymbol> symbols);
        void MarkInkAsProcessed();
        void Pan(LatLon init, LatLon end);
        void RenderSymbol(StpSymbol stpSymbol, Image overlay = null);
        void Zoom(LatLon center, double factor);
        void Zoom(LatLon topLeft, LatLon botRight);
    }
}
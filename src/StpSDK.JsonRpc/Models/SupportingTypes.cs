using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StpSDK;

public class SpeechRecoItem
{
    [JsonProperty("text")]
    public string Text { get; set; }

    [JsonProperty("confidence")]
    public double Confidence { get; set; }

    public double? StartSec { get; set; }
    public double? EndSec { get; set; }

    public string ExtraRecoInfo { get; set; }

    public SpeechRecoItem() { }

    public SpeechRecoItem(string text, double confidence, double? startSec = null, double? endSec = null)
    {
        Text = text;
        Confidence = confidence;
        StartSec = startSec;
        EndSec = endSec;
    }

    public override string ToString() => $"({Text}, {Confidence})";
}

public class SpeechRecoResult
{
    public string FromReco { get; set; }
    public List<SpeechRecoItem> Results { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public SpeechRecoResult(string fromReco)
    {
        FromReco = fromReco;
        Results = new List<SpeechRecoItem>();
    }

    public SpeechRecoResult(string fromReco, DateTime startTime, DateTime endTime) : this(fromReco)
    {
        StartTime = startTime;
        EndTime = endTime;
    }

    public SpeechRecoResult(string fromRecos, List<SpeechRecoResult> allResults, int maxCombinedRecos = -1) : this(fromRecos)
    {
        if (allResults is null)
            throw new ArgumentNullException(nameof(allResults));
        foreach (var res in allResults)
            Combine(res, maxCombinedRecos);
    }

    public void Combine(SpeechRecoResult res, int maxCombinedRecos = -1)
    {
        if (Results.Count == 0)
        {
            Results.AddRange(res.Results);
            StartTime = res.StartTime;
            EndTime = res.EndTime;
            return;
        }

        foreach (var alternate in res.Results)
        {
            var sameText = Results.Where(cri => cri.Text == alternate.Text).ToList();
            if (sameText is null || sameText.Count == 0)
            {
                Results.Add(alternate);
            }
            else
            {
                if (sameText[0].Confidence < alternate.Confidence)
                {
                    Results.Remove(sameText[0]);
                    alternate.Confidence *= 1.1;
                    Results.Add(alternate);
                }
                else
                {
                    sameText[0].Confidence *= 1.1;
                }
            }
        }
        Results = Results.OrderBy(r => -r.Confidence).ToList();

        if (maxCombinedRecos >= 0 && Results.Count > maxCombinedRecos)
            Results.RemoveRange(maxCombinedRecos, Results.Count - maxCombinedRecos);

        if (Results.Count > 0 && Results[0].Confidence > 0.99)
        {
            double discount = 0.99 / Results[0].Confidence;
            foreach (var cr in Results)
                cr.Confidence *= discount;
        }

        StartTime = StartTime > res.StartTime ? res.StartTime : StartTime;
        EndTime = EndTime < res.EndTime ? res.EndTime : EndTime;
    }

    public void AddAlternate(string alternate, double likelihood, double? startSec = null, double? endSec = null, string extraRecoInfo = null)
    {
        alternate = alternate.Trim();
        SpeechRecoItem ri = Results.Find(r => r.Text.Equals(alternate, StringComparison.InvariantCultureIgnoreCase));
        if (ri is null)
        {
            ri = new SpeechRecoItem(alternate, likelihood, startSec, endSec);
            Results.Add(ri);
        }
        else
        {
            ri.Confidence = Math.Max(ri.Confidence, likelihood) * 1.05;
        }
        ri.ExtraRecoInfo = extraRecoInfo;
    }
}

public enum SketchClass
{
    None, SelectTemp, UnimodalModify, Dot, Line, StraightLine, ZigZagLine,
    BentVee, Vee, Hook, UBend, UBendThreePoint, Area, OpenCircle,
    Circle_Center, Rectangle_Axis, Rectangle_Center, ArrowThin, ArrowFat, Unit, TG
}

public class SketchRecoResult
{
    public SketchClass Type { get; set; }
    public double Confidence { get; set; }

    public SketchRecoResult() { }
    public SketchRecoResult(SketchClass type, double confidence)
    {
        Type = type;
        Confidence = confidence;
    }
}

public enum ListenMode { once, on, off }

// StpMessageLevel is declared nested in StpRecognizer (see StpRecognizerEvents.cs) to
// preserve source compatibility with the original SDK (StpRecognizer.StpMessageLevel).
// The former StpSDK.Size DTO was removed; the public API uses System.Drawing.Size to
// match the original SDK and avoid colliding with System.Drawing.Size under `using StpSDK;`.

public class Interval
{
    [JsonProperty("start")]
    public DateTime Start { get; set; }

    [JsonProperty("end")]
    public DateTime End { get; set; }

    public Interval() { }
    public Interval(DateTime start, DateTime end)
    {
        Start = start;
        End = end;
    }
}

public static class TimingConstants
{
    public const double Timing_Not_Set = -1.0;
    public const double Timing_PLA = 0.0;
    public const double Timing_Drawing = 2.5;
    public const double Timing_Expert = 1.5;
    public const double Timing_Novice = 3.5;
}

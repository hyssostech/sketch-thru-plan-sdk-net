namespace StpSDK;

public interface IStpObject
{
    string Type { get; set; }
    string Poid { get; set; }
    string UpdateTimestamp { get; set; }
    string CreationTimestamp { get; set; }
    string Description { get; set; }
}

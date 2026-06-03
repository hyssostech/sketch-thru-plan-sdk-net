namespace StpSDK;

public partial class StpRecognizer
{
    public SymbolService CreateSymbolService()
    {
        return new SymbolService(this);
    }

    public TaskService CreateTaskService(SymbolService symbolService)
    {
        return new TaskService(this, symbolService);
    }

    public TaskOrgService CreateTaskOrgService()
    {
        return new TaskOrgService(this);
    }
}

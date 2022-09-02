namespace MetaMe.WindowsClient
{
    //needs to be public
    public enum ClientApplicationStatus
    {
        Initializing, //first run discovering state
        Started,
        Stopping,
        Error
    }
}

namespace MetaMe.WindowsClient
{
    //needs to be public to prevent obfuscation
    public enum ClientApplicationAccountStatus
    {
        Unknown,
        LoggedOut,
        LoggedIn,
        PasswordRequired, //for later use
    }
}

namespace Sandaab.AndroidApp.Entities
{
    public enum PermissionRequestCode : int
    {
        AddDevice
    }

    public enum MessageBoxButtons
    {
        None,
        OK = 0x1,
        Cancel = 0x02,
        Yes = 0x04,
        No = 0x08,
        OKCancel = OK | Cancel,
        YesNo = Yes | No,
        YesNoCancel = Yes | No | Cancel
    }

    public enum MessageBoxIcon
    {
        None = 0,
        Hand = 16,
        Stop = 16,
        Error = 16,
        Question = 32,
        Exclamation = 48,
        Warning = 48,
        Asterisk = 64,
        Information = 64
    }
}

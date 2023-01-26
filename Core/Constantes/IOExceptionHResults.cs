namespace Sandaab.Core.Constantes
{
    public enum IOExceptionHResults : int
    {
        // These use an error code from WinError.h
        COR_E_ENDOFSTREAM = unchecked((int)0x80070026),  // OS defined
        COR_E_FILELOAD = unchecked((int)0x80131621),
        COR_E_FILENOTFOUND = unchecked((int)0x80070002),
        COR_E_DIRECTORYNOTFOUND = unchecked((int)0x80070003),
        COR_E_PATHTOOLONG = unchecked((int)0x800700CE),

        COR_E_IO = unchecked((int)0x80131620),
    }
}

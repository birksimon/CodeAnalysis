namespace CodeAnalysis.Output
{
    interface ICSVPrintable
    {
        string GetCSVString();
        string GetCSVHeader();
        bool IsEmpty();
        string GetFileName();
    }
}

namespace XmppBot
{
  // Models/PrintJob.cs
  public class PrintJob
  {
    public string PrinterName { get; set; }
    public string FilePath { get; set; }
    public string Protocol { get; set; } // "IPP", "CUPS", etc.
  }

}

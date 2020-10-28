using Microsoft.VisualStudio.DebuggerVisualizers;

[assembly: System.Diagnostics.DebuggerVisualizer(typeof(DumpVisualizer.Visualizer), typeof(DumpVisualizer.ObjectSource),
    Target = typeof(System.WeakReference), Description = "Dump Visualizer")]

namespace DumpVisualizer
{
    public class Visualizer : DialogDebuggerVisualizer
    {
        protected override void Show(
      IDialogVisualizerService windowService,
      IVisualizerObjectProvider objectProvider)
        {
            string html = objectProvider.GetObject().ToString();
            using (HtmlDlg htmlDlg = new HtmlDlg())
            {
                htmlDlg.Init(html);
                windowService.ShowDialog(htmlDlg);
            }
        }
    }
}

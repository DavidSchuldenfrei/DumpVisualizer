using Microsoft.VisualStudio.DebuggerVisualizers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DumpVisualizer
{
    [Serializable]
    public class ObjectSource : VisualizerObjectSource
    {

        public override void GetData(object target, Stream outgoingData)
        {
            var realTarget = ((WeakReference)target).Target;
            base.GetData(realTarget.Dump(useScripts: true), outgoingData);
        }
    }
}

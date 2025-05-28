using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ETABSv1;

namespace GeometryFromEtabs.Extractors
{
    public interface IElementExtractor<T>
    {
        List<T> ExtractElements(cSapModel model);
        string ElementType { get; }
    }
}

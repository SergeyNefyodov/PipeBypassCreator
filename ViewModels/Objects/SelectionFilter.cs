using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipeBypassCreator.ViewModels.Objects
{
    public class SelectionFilter : ISelectionFilter
    {
        public Element Element { get; set; }
        public bool AllowElement(Element elem)
        {
            if (!(elem is MEPCurve))
                return false;

            if (Element == null)
            {                
                return true;
            }

            return elem.Id == Element.Id;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }
}

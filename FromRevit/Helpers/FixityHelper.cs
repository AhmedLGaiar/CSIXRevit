using Autodesk.Revit.DB;
using FromRevit.Data;

namespace FromRevit.Helpers
{
    public static class FixityHelper
    {
        public static Fixity GetColumnFixity(FamilyInstance column)
        {
            Fixity fixity = new Fixity { Base = "Fixed", Top = "Fixed" }; // Default

            // Get Base and Top Level Fixity Parameters
            Parameter baseReleaseX = column.get_Parameter(BuiltInParameter.STRUCTURAL_BOTTOM_RELEASE_FX);
            Parameter baseReleaseY = column.get_Parameter(BuiltInParameter.STRUCTURAL_BOTTOM_RELEASE_FY);
            Parameter topReleaseX = column.get_Parameter(BuiltInParameter.STRUCTURAL_TOP_RELEASE_FX);
            Parameter topReleaseY = column.get_Parameter(BuiltInParameter.STRUCTURAL_TOP_RELEASE_FY);

            // If base releases exist, assume pinned
            if ((baseReleaseX != null && baseReleaseX.AsInteger() == 1) ||
                (baseReleaseY != null && baseReleaseY.AsInteger() == 1))
            {
                fixity.Base = "Pinned";
            }

            // If top releases exist, assume pinned
            if ((topReleaseX != null && topReleaseX.AsInteger() == 1) ||
                (topReleaseY != null && topReleaseY.AsInteger() == 1))
            {
                fixity.Top = "Pinned";
            }

            return fixity;
        }
    }
}


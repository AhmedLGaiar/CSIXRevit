using Autodesk.Revit.DB;

namespace FromRevit.Data
{
    public class Fixity
    {
        public string Base { get; set; }
        public string Top { get; set; }

        public  Fixity GetColumnFixity(FamilyInstance column)
        {
            Fixity fixity = new Fixity { Base = "Fixed", Top = "Fixed" }; // Default

            // Get Structural Framing End Release Parameters
            Parameter baseReleaseX = column.get_Parameter(BuiltInParameter.STRUCTURAL_END_RELEASE_FX);
            Parameter baseReleaseY = column.get_Parameter(BuiltInParameter.STRUCTURAL_END_RELEASE_FY);
            Parameter topReleaseX = column.get_Parameter(BuiltInParameter.STRUCTURAL_START_RELEASE_FX);
            Parameter topReleaseY = column.get_Parameter(BuiltInParameter.STRUCTURAL_START_RELEASE_FY);

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

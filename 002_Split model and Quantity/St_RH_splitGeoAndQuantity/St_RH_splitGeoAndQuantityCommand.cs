using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;

namespace St_RH_splitGeoAndQuantity
{
    public class St_RH_splitGeoAndQuantityCommand : Command
    {
        public St_RH_splitGeoAndQuantityCommand()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static St_RH_splitGeoAndQuantityCommand Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "SplitTesting";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // TODO: start here modifying the behaviour of your command.
            // ---
            Rhino.DocObjects.ObjRef[] objrefs;
            Result rc = Rhino.Input.RhinoGet.GetMultipleObjects("Select first set of polysurfaces",
                                                                false, Rhino.DocObjects.ObjectType.PolysrfFilter, out objrefs);
            if (rc != Rhino.Commands.Result.Success)
                return rc;
            if (objrefs == null || objrefs.Length < 1)
                return Rhino.Commands.Result.Failure;

            List<Rhino.Geometry.Brep> in_breps0 = new List<Rhino.Geometry.Brep>();
            for (int i = 0; i < objrefs.Length; i++)
            {
                Rhino.Geometry.Brep brep = objrefs[i].Brep();
                if (brep != null)
                    in_breps0.Add(brep);
            }

            doc.Objects.UnselectAll();
            rc = Rhino.Input.RhinoGet.GetMultipleObjects("Select second set of polysurfaces",
              false, Rhino.DocObjects.ObjectType.PolysrfFilter, out objrefs);
            if (rc != Rhino.Commands.Result.Success)
                return rc;
            if (objrefs == null || objrefs.Length < 1)
                return Rhino.Commands.Result.Failure;

            List<Rhino.Geometry.Brep> in_breps1 = new List<Rhino.Geometry.Brep>();
            for (int i = 0; i < objrefs.Length; i++)
            {
                Rhino.Geometry.Brep brep = objrefs[i].Brep();
                if (brep != null)
                    in_breps1.Add(brep);
            }

            double tolerance = doc.ModelAbsoluteTolerance;
            Rhino.Geometry.Brep[] breps = Rhino.Geometry.Brep.CreateBooleanDifference(in_breps0, in_breps1, tolerance);
            if (breps.Length < 1)
                return Rhino.Commands.Result.Nothing;
            for (int i = 0; i < breps.Length; i++)
                doc.Objects.AddBrep(breps[i]);
            doc.Views.Redraw();

            // ---
            return Result.Success;
        }
    }
}
